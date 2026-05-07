namespace JavisApi.DTOs.Auth;

public record LoginRequest(string Email, string Password);

public record LoginResponse(string AccessToken, string TokenType, EmployeeDto Employee);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public record EmployeeDto(
    Guid Id,
    string Name,
    string Email,
    string Role,
    Guid DepartmentId,
    string? DepartmentName,
    Guid? CustomRoleId,
    string? McpToken,
    bool IsActive,
    DateTime CreatedAt
);
