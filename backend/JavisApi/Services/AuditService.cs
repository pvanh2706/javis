using JavisApi.Data;
using JavisApi.Models;

namespace JavisApi.Services;

public class AuditService
{
    private readonly AppDbContext _db;

    public AuditService(AppDbContext db)
    {
        _db = db;
    }

    public async Task LogAsync(
        Guid principalId,
        string action,
        string resourceType,
        string resourceId,
        string decision = "allow",
        string? reason = null,
        string principalType = "human")
    {
        var log = new AuditLog
        {
            PrincipalId = principalId,
            PrincipalType = principalType,
            Action = action,
            ResourceType = resourceType,
            ResourceId = resourceId,
            Decision = decision,
            Reason = reason
        };

        _db.AuditLogs.Add(log);
        await _db.SaveChangesAsync();
    }
}
