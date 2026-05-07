using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JavisApi.Models;

[Table("employees")]
public class Employee
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("name")]
    [MaxLength(200)]
    public string Name { get; set; } = "";

    [Column("email")]
    [MaxLength(200)]
    public string Email { get; set; } = "";

    [Column("password_hash")]
    [MaxLength(500)]
    public string? PasswordHash { get; set; }

    /// <summary>"admin" or "employee"</summary>
    [Column("role")]
    [MaxLength(20)]
    public string Role { get; set; } = "employee";

    [Column("department_id")]
    public Guid DepartmentId { get; set; }

    [Column("custom_role_id")]
    public Guid? CustomRoleId { get; set; }

    [Column("mcp_token")]
    [MaxLength(500)]
    public string? McpToken { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("last_connected")]
    public DateTime? LastConnected { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey(nameof(DepartmentId))]
    public Department? Department { get; set; }

    [ForeignKey(nameof(CustomRoleId))]
    public Role? CustomRole { get; set; }
}
