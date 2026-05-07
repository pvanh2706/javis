using System.Security.Claims;
using JavisApi.Data;
using JavisApi.DTOs.Wiki;
using JavisApi.Models;
using JavisApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JavisApi.Controllers;

[ApiController]
[Route("api/wiki")]
[Authorize]
public class WikiController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly WikiService _wiki;
    private readonly PermissionEngine _permissions;
    private readonly AuditService _audit;

    public WikiController(AppDbContext db, WikiService wiki,
        PermissionEngine permissions, AuditService audit)
    {
        _db = db;
        _wiki = wiki;
        _permissions = permissions;
        _audit = audit;
    }

    [HttpGet("pages")]
    public async Task<IActionResult> List(
        [FromQuery] string scopeType = "global",
        [FromQuery] Guid? scopeId = null,
        [FromQuery] string? knowledgeType = null)
    {
        var employee = await GetEmployeeAsync();
        if (employee is null) return Unauthorized();
        if (!_permissions.CanReadWiki(employee)) return Forbid();

        var query = _db.WikiPages.AsQueryable();
        query = await _permissions.FilterWikiPagesAsync(query, employee);

        if (scopeType is not null) query = query.Where(p => p.ScopeType == scopeType);
        if (scopeId.HasValue) query = query.Where(p => p.ScopeId == scopeId);
        if (!string.IsNullOrEmpty(knowledgeType))
            query = query.Where(p => p.KnowledgeTypeSlugsJson.Contains(knowledgeType));

        var pages = await query
            .OrderBy(p => p.Title)
            .Select(p => MapToSummaryDto(p))
            .ToListAsync();

        return Ok(pages);
    }

    [HttpGet("pages/{slug}")]
    public async Task<IActionResult> Get(string slug,
        [FromQuery] string scopeType = "global",
        [FromQuery] Guid? scopeId = null)
    {
        var employee = await GetEmployeeAsync();
        if (employee is null) return Unauthorized();
        if (!_permissions.CanReadWiki(employee)) return Forbid();

        var page = await _wiki.GetBySlugAsync(slug, scopeType, scopeId);
        if (page is null) return NotFound();

        await _audit.LogAsync(employee.Id, "read", "wiki_page", slug);
        return Ok(MapToDto(page));
    }

    [HttpPost("pages")]
    public async Task<IActionResult> Create([FromBody] CreateWikiPageRequest req)
    {
        var employee = await GetEmployeeAsync();
        if (employee is null) return Unauthorized();
        if (!_permissions.CanEditWiki(employee)) return Forbid();

        var existing = await _wiki.GetBySlugAsync(req.Slug, req.ScopeType, req.ScopeId);
        if (existing is not null)
            return Conflict(new { message = $"Page '{req.Slug}' already exists" });

        var page = await _wiki.CreatePageAsync(
            req.Slug, req.Title, req.PageType,
            req.ContentMd, req.Summary, req.ScopeType, req.ScopeId,
            req.KnowledgeTypeSlugs, null, employee.Id, "editor_edit");

        await _audit.LogAsync(employee.Id, "create", "wiki_page", req.Slug);
        return CreatedAtAction(nameof(Get), new { slug = page.Slug }, MapToDto(page));
    }

    [HttpPut("pages/{slug}")]
    public async Task<IActionResult> Update(string slug,
        [FromQuery] string scopeType = "global",
        [FromQuery] Guid? scopeId = null,
        [FromBody] UpdateWikiPageRequest? req = null)
    {
        if (req is null) return BadRequest();
        var employee = await GetEmployeeAsync();
        if (employee is null) return Unauthorized();
        if (!_permissions.CanEditWiki(employee)) return Forbid();

        var page = await _wiki.GetBySlugAsync(slug, scopeType, scopeId);
        if (page is null) return NotFound();

        await _wiki.UpdatePageAsync(
            page, req.ContentMd, req.Summary, req.Title,
            employee.Id, "editor_edit", req.ChangeNote);

        await _audit.LogAsync(employee.Id, "update", "wiki_page", slug);
        return Ok(MapToDto(page));
    }

    [HttpDelete("pages/{slug}")]
    public async Task<IActionResult> Delete(string slug,
        [FromQuery] string scopeType = "global",
        [FromQuery] Guid? scopeId = null)
    {
        var employee = await GetEmployeeAsync();
        if (employee is null) return Unauthorized();
        if (!_permissions.IsAdmin(employee)) return Forbid();

        var page = await _wiki.GetBySlugAsync(slug, scopeType, scopeId);
        if (page is null) return NotFound();

        await _wiki.DeletePageAsync(page);
        await _audit.LogAsync(employee.Id, "delete", "wiki_page", slug);
        return NoContent();
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string q,
        [FromQuery] string scopeType = "global",
        [FromQuery] Guid? scopeId = null)
    {
        var employee = await GetEmployeeAsync();
        if (employee is null) return Unauthorized();
        if (!_permissions.CanReadWiki(employee)) return Forbid();

        var results = await _wiki.FullTextSearchAsync(q, scopeType, scopeId);
        return Ok(results.Select(MapToSummaryDto));
    }

    [HttpGet("pages/{slug}/links")]
    public async Task<IActionResult> GetLinks(string slug)
    {
        var (outlinks, backlinks) = await _wiki.GetLinksAsync(slug);
        return Ok(new WikiLinksDto(slug, outlinks, backlinks));
    }

    [HttpGet("pages/{slug}/revisions")]
    public async Task<IActionResult> GetRevisions(string slug,
        [FromQuery] string scopeType = "global",
        [FromQuery] Guid? scopeId = null)
    {
        var page = await _wiki.GetBySlugAsync(slug, scopeType, scopeId);
        if (page is null) return NotFound();

        var revisions = await _db.WikiPageRevisions
            .Include(r => r.ChangedBy)
            .Where(r => r.PageId == page.Id)
            .OrderByDescending(r => r.Version)
            .Select(r => new WikiPageRevisionDto(
                r.Id, r.Version, r.ContentMd, r.ChangeType,
                r.ChangedById, r.ChangedBy != null ? r.ChangedBy.Name : null,
                r.ChangeNote, r.CreatedAt))
            .ToListAsync();

        return Ok(revisions);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private async Task<Employee?> GetEmployeeAsync()
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(idStr, out var id)) return null;
        return await _db.Employees
            .Include(e => e.CustomRole)
            .FirstOrDefaultAsync(e => e.Id == id && e.IsActive);
    }

    private static WikiPageDto MapToDto(WikiPage p) => new(
        p.Id, p.Slug, p.Title, p.PageType, p.ContentMd, p.Summary,
        p.ScopeType, p.ScopeId, p.KnowledgeTypeSlugs,
        p.Version, p.Orphaned, p.CreatedAt, p.UpdatedAt);

    private static WikiPageSummaryDto MapToSummaryDto(WikiPage p) => new(
        p.Id, p.Slug, p.Title, p.PageType, p.Summary,
        p.ScopeType, p.ScopeId, p.KnowledgeTypeSlugs,
        p.Version, p.UpdatedAt);
}
