namespace JavisApi.DTOs.Employees;

public record DepartmentDto(
    Guid Id,
    string Name,
    string? Description,
    int EmployeeCount,
    DateTime CreatedAt
);

public record CreateDepartmentRequest(string Name, string? Description = null);
public record UpdateDepartmentRequest(string? Name, string? Description);

public record RoleDto(
    Guid Id,
    string Name,
    string? Description,
    List<string> Permissions,
    bool IsSystem,
    int EmployeeCount,
    DateTime CreatedAt
);

public record CreateRoleRequest(
    string Name,
    string? Description = null,
    List<string>? Permissions = null
);

public record UpdateRoleRequest(
    string? Name,
    string? Description,
    List<string>? Permissions
);

public record CreateEmployeeRequest(
    string Name,
    string Email,
    string Password,
    Guid DepartmentId,
    string Role = "employee",
    Guid? CustomRoleId = null
);

public record UpdateEmployeeRequest(
    string? Name,
    string? Email,
    Guid? DepartmentId,
    string? Role,
    Guid? CustomRoleId,
    bool? IsActive
);

public record KnowledgeTypeDto(
    Guid Id,
    string Slug,
    string Name,
    string Color,
    string? Description,
    int SortOrder
);

public record CreateKnowledgeTypeRequest(
    string Slug,
    string Name,
    string Color = "#6366f1",
    string? Description = null,
    int SortOrder = 0
);
