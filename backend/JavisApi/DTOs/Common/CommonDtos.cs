namespace JavisApi.DTOs.Common;

public record PagedResult<T>(
    List<T> Items,
    int Total,
    int Page,
    int PageSize
);

public record ApiError(string Message, string? Detail = null);

public record AdminSettingDto(string Key, string? Value);

public record UpdateSettingRequest(string Value);

public record AuditLogDto(
    Guid Id,
    DateTime Timestamp,
    Guid PrincipalId,
    string PrincipalType,
    string Action,
    string ResourceType,
    string ResourceId,
    string Decision,
    string? Reason
);
