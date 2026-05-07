using JavisApi.Data;
using JavisApi.Models;
using Microsoft.EntityFrameworkCore;

namespace JavisApi.Services;

/// <summary>
/// Authenticates MCP clients using their "ark_xxx" bearer tokens.
/// </summary>
public class McpAuthService
{
    private readonly AppDbContext _db;

    public McpAuthService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Employee?> VerifyTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || !token.StartsWith("ark_"))
            return null;

        var employee = await _db.Employees
            .Include(e => e.Department)
            .Include(e => e.CustomRole)
            .FirstOrDefaultAsync(e => e.McpToken == token && e.IsActive);

        if (employee is not null)
        {
            employee.LastConnected = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        return employee;
    }
}
