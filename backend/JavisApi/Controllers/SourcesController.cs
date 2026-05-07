using Hangfire;
using System.Security.Claims;
using JavisApi.Data;
using JavisApi.DTOs.Sources;
using JavisApi.Jobs;
using JavisApi.Models;
using JavisApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JavisApi.Controllers;

[ApiController]
[Route("api/sources")]
[Authorize]
public class SourcesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IStorageService _storage;
    private readonly PermissionEngine _permissions;
    private readonly AuditService _audit;
    private readonly IBackgroundJobClient _jobs;

    public SourcesController(
        AppDbContext db, IStorageService storage,
        PermissionEngine permissions, AuditService audit,
        IBackgroundJobClient jobs)
    {
        _db = db;
        _storage = storage;
        _permissions = permissions;
        _audit = audit;
        _jobs = jobs;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? scopeType, [FromQuery] Guid? scopeId)
    {
        var employee = await GetEmployeeAsync();
        if (employee is null) return Unauthorized();

        if (!_permissions.CanReadSources(employee))
            return Forbid();

        var query = _db.Sources
            .Include(s => s.SourceDepartments)
            .Include(s => s.KnowledgeType)
            .AsQueryable();

        query = _permissions.FilterSources(query, employee);

        if (scopeType is not null) query = query.Where(s => s.ScopeType == scopeType);
        if (scopeId.HasValue) query = query.Where(s => s.ScopeId == scopeId);

        var sources = await query
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => MapToDto(s))
            .ToListAsync();

        return Ok(sources);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var employee = await GetEmployeeAsync();
        if (employee is null) return Unauthorized();

        var source = await _db.Sources
            .Include(s => s.SourceDepartments)
            .Include(s => s.KnowledgeType)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (source is null) return NotFound();
        return Ok(MapToDto(source));
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(
        IFormFile? file,
        [FromForm] string? title,
        [FromForm] Guid? knowledgeTypeId,
        [FromForm] string? departmentIds,
        [FromForm] string? scopeType,
        [FromForm] Guid? scopeId)
    {
        var employee = await GetEmployeeAsync();
        if (employee is null) return Unauthorized();

        if (!_permissions.CanUploadSource(employee))
            return Forbid();

        if (file is null || file.Length == 0)
            return BadRequest(new { message = "No file provided" });

        // Upload to storage
        await using var stream = file.OpenReadStream();
        var key = await _storage.UploadAsync(stream, file.FileName, file.ContentType);

        var source = new Source
        {
            Title = title ?? Path.GetFileNameWithoutExtension(file.FileName),
            SourceType = "file",
            ScopeType = scopeType ?? "global",
            ScopeId = scopeId,
            KnowledgeTypeId = knowledgeTypeId,
            ContributedByEmployeeId = employee.Id,
            FileName = file.FileName,
            FileSize = file.Length,
            MinioKey = key,
            Status = "pending"
        };

        _db.Sources.Add(source);

        // Parse department IDs
        if (!string.IsNullOrEmpty(departmentIds))
        {
            var deptIds = departmentIds.Split(',')
                .Select(d => Guid.TryParse(d.Trim(), out var g) ? g : Guid.Empty)
                .Where(g => g != Guid.Empty);

            foreach (var deptId in deptIds)
                source.SourceDepartments.Add(new SourceDepartment
                    { SourceId = source.Id, DepartmentId = deptId });
        }

        await _db.SaveChangesAsync();

        // Enqueue ingestion
        var jobId = _jobs.Enqueue<IngestFileJob>(j => j.ExecuteAsync(source.Id));
        source.JobId = jobId;
        await _db.SaveChangesAsync();

        await _audit.LogAsync(employee.Id, "upload", "source", source.Id.ToString());

        return Ok(MapToDto(source));
    }

    [HttpPost("upload-url")]
    public async Task<IActionResult> UploadUrl([FromBody] UploadUrlRequest req)
    {
        var employee = await GetEmployeeAsync();
        if (employee is null) return Unauthorized();

        if (!_permissions.CanUploadSource(employee)) return Forbid();

        var source = new Source
        {
            Title = req.Title ?? req.Url,
            SourceType = "url",
            ScopeType = req.ScopeType ?? "global",
            ScopeId = req.ScopeId,
            KnowledgeTypeId = req.KnowledgeTypeId,
            ContributedByEmployeeId = employee.Id,
            Url = req.Url,
            Status = "pending"
        };

        _db.Sources.Add(source);
        await _db.SaveChangesAsync();

        var jobId = _jobs.Enqueue<IngestFileJob>(j => j.ExecuteAsync(source.Id));
        source.JobId = jobId;
        await _db.SaveChangesAsync();

        return Ok(MapToDto(source));
    }

    [HttpPost("{id:guid}/recompile")]
    public async Task<IActionResult> Recompile(Guid id)
    {
        var employee = await GetEmployeeAsync();
        if (employee is null) return Unauthorized();
        if (!_permissions.CanUploadSource(employee)) return Forbid();

        var source = await _db.Sources.FindAsync(id);
        if (source is null) return NotFound();

        source.Status = "pending";
        source.Progress = 0;
        source.ProgressMessage = "Recompile queued";
        source.ErrorMessage = null;

        var jobId = _jobs.Enqueue<IngestFileJob>(j => j.ExecuteAsync(source.Id));
        source.JobId = jobId;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Recompile queued" });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var employee = await GetEmployeeAsync();
        if (employee is null) return Unauthorized();
        if (!_permissions.CanDeleteSource(employee)) return Forbid();

        var source = await _db.Sources.FindAsync(id);
        if (source is null) return NotFound();

        if (!string.IsNullOrEmpty(source.MinioKey))
            await _storage.DeleteAsync(source.MinioKey);

        _db.Sources.Remove(source);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(employee.Id, "delete", "source", id.ToString());
        return NoContent();
    }

    private async Task<Employee?> GetEmployeeAsync()
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(idStr, out var id)) return null;

        return await _db.Employees
            .Include(e => e.CustomRole)
            .FirstOrDefaultAsync(e => e.Id == id && e.IsActive);
    }

    private static SourceDto MapToDto(Source s) => new(
        s.Id, s.Title, s.SourceType, s.ScopeType, s.ScopeId,
        s.KnowledgeTypeId, s.KnowledgeType?.Name,
        s.FileName, s.FileSize, s.Url,
        s.Status, s.Progress, s.ProgressMessage, s.ErrorMessage,
        s.SourceDepartments.Select(sd => sd.DepartmentId).ToList(),
        s.CreatedAt, s.UpdatedAt);
}

public record UploadUrlRequest(
    string Url,
    string? Title = null,
    Guid? KnowledgeTypeId = null,
    string? ScopeType = null,
    Guid? ScopeId = null);
