namespace JavisApi.DTOs.Sources;

public record SourceDto(
    Guid Id,
    string? Title,
    string? SourceType,
    string ScopeType,
    Guid? ScopeId,
    Guid? KnowledgeTypeId,
    string? KnowledgeTypeName,
    string? FileName,
    long? FileSize,
    string? Url,
    string Status,
    int Progress,
    string? ProgressMessage,
    string? ErrorMessage,
    List<Guid> DepartmentIds,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record UploadSourceRequest
{
    public string? Title { get; init; }
    public Guid? KnowledgeTypeId { get; init; }
    public List<Guid>? DepartmentIds { get; init; }
    public string? Url { get; init; }
    // File comes via IFormFile in controller
}

public record UpdateSourceRequest(
    string? Title,
    Guid? KnowledgeTypeId,
    List<Guid>? DepartmentIds
);
