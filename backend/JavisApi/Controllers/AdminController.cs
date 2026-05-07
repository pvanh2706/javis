using System.Security.Claims;
using JavisApi.Data;
using JavisApi.DTOs.Common;
using JavisApi.Models;
using JavisApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JavisApi.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ConfigService _config;
    private readonly PermissionEngine _permissions;

    public AdminController(AppDbContext db, ConfigService config, PermissionEngine permissions)
    {
        _db = db;
        _config = config;
        _permissions = permissions;
    }

    [HttpGet("admin/settings")]
    public async Task<IActionResult> GetSettings()
    {
        var employee = await GetEmployeeAsync();
        if (employee is null) return Unauthorized();
        if (!_permissions.IsAdmin(employee)) return Forbid();

        var configs = await _config.GetAllAsync();
        // Mask sensitive keys
        var masked = configs.ToDictionary(
            kv => kv.Key,
            kv => kv.Key.Contains("api_key") && kv.Value is not null
                ? "***" + kv.Value[Math.Max(0, kv.Value.Length - 4)..]
                : kv.Value);

        return Ok(masked);
    }

    [HttpPut("admin/settings/{key}")]
    public async Task<IActionResult> UpdateSetting(string key, [FromBody] UpdateSettingRequest req)
    {
        var employee = await GetEmployeeAsync();
        if (employee is null) return Unauthorized();
        if (!_permissions.IsAdmin(employee)) return Forbid();

        await _config.SetAsync(key, req.Value);
        return Ok(new { message = "Setting updated" });
    }

    [HttpGet("audit")]
    public async Task<IActionResult> GetAuditLog(
        [FromQuery] Guid? principalId = null,
        [FromQuery] string? resourceType = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var employee = await GetEmployeeAsync();
        if (employee is null) return Unauthorized();
        if (!_permissions.IsAdmin(employee)) return Forbid();

        var query = _db.AuditLogs.AsQueryable();

        if (principalId.HasValue) query = query.Where(l => l.PrincipalId == principalId);
        if (!string.IsNullOrEmpty(resourceType)) query = query.Where(l => l.ResourceType == resourceType);
        if (from.HasValue) query = query.Where(l => l.Timestamp >= from);
        if (to.HasValue) query = query.Where(l => l.Timestamp <= to);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(l => l.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new AuditLogDto(
                l.Id, l.Timestamp, l.PrincipalId, l.PrincipalType,
                l.Action, l.ResourceType, l.ResourceId, l.Decision, l.Reason))
            .ToListAsync();

        return Ok(new PagedResult<AuditLogDto>(items, total, page, pageSize));
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
