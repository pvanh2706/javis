namespace JavisApi.DTOs.Projects;

public record ProjectDto(
    Guid Id,
    string Name,
    string? Description,
    string WorkspaceType,
    string Status,
    Guid? CreatedById,
    string? CreatedByName,
    int MemberCount,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record CreateProjectRequest(
    string Name,
    string? Description = null,
    string WorkspaceType = "project"
);

public record UpdateProjectRequest(
    string? Name,
    string? Description,
    string? Status
);

public record ProjectMemberDto(
    Guid EmployeeId,
    string EmployeeName,
    string EmployeeEmail,
    string? DepartmentName,
    string Role,
    DateTime AddedAt
);

public record AddMemberRequest(
    Guid EmployeeId,
    string Role = "viewer"
);

public record UpdateMemberRoleRequest(string Role);
