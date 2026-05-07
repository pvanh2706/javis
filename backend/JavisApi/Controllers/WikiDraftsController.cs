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
[Route("api/wiki/drafts")]
[Authorize]
public class WikiDraftsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly WikiService _wiki;
    private readonly PermissionEngine _permissions;

    public WikiDraftsController(AppDbContext db, WikiService wiki, PermissionEngine permissions)
    {
        _db = db;
        _wiki = wiki;
        _permissions = permissions;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? status = "pending")
    {
        var employee = await GetEmployeeAsync();
        if (employee is null) return Unauthorized();

        var query = _db.WikiPageDrafts
            .Include(d => d.Page)
            .Include(d => d.Author)
            .AsQueryable();

        if (!_permissions.IsAdmin(employee) && !_permissions.CanEditWiki(employee))
            query = query.Where(d => d.AuthorId == employee.Id);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(d => d.Status == status);

        var drafts = await query
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new WikiPageDraftDto(
                d.Id, d.PageId,
                d.Page != null ? d.Page.Slug : "",
                d.Page != null ? d.Page.Title : "",
                d.AuthorId,
                d.Author != null ? d.Author.Name : null,
                d.ContentMd, d.Note, d.Status, d.ReviewerNote, d.CreatedAt))
            .ToListAsync();

        return Ok(drafts);
    }

    [HttpPost("pages/{slug}")]
    public async Task<IActionResult> Propose(string slug,
        [FromQuery] string scopeType = "global",
        [FromQuery] Guid? scopeId = null,
        [FromBody] CreateDraftRequest? req = null)
    {
        if (req is null) return BadRequest();
        var employee = await GetEmployeeAsync();
        if (employee is null) return Unauthorized();

        var page = await _wiki.GetBySlugAsync(slug, scopeType, scopeId);
        if (page is null) return NotFound();

        var draft = await _wiki.CreateDraftAsync(
            page.Id, employee.Id, req.ContentMd, req.Note);

        return Ok(new WikiPageDraftDto(
            draft.Id, draft.PageId, slug, page.Title,
            draft.AuthorId, employee.Name,
            draft.ContentMd, draft.Note, draft.Status, null, draft.CreatedAt));
    }

    [HttpPut("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ReviewDraftRequest? req = null)
    {
        var employee = await GetEmployeeAsync();
        if (employee is null) return Unauthorized();
        if (!_permissions.CanEditWiki(employee)) return Forbid();

        var draft = await _wiki.ApproveDraftAsync(id, employee.Id, req?.ReviewerNote);
        return draft is null ? NotFound() : Ok(new { message = "Draft approved" });
    }

    [HttpPut("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] ReviewDraftRequest? req = null)
    {
        var employee = await GetEmployeeAsync();
        if (employee is null) return Unauthorized();
        if (!_permissions.CanEditWiki(employee)) return Forbid();

        var draft = await _wiki.RejectDraftAsync(id, employee.Id, req?.ReviewerNote);
        return draft is null ? NotFound() : Ok(new { message = "Draft rejected" });
    }

    private async Task<Employee?> GetEmployeeAsync()
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(idStr, out var id)) return null;
        return await _db.Employees
            .Include(e => e.CustomRole)
            .FirstOrDefaultAsync(e => e.Id == id && e.IsActive);
    }
}
