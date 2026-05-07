using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JavisApi.Models;

[Table("roles")]
public class Role
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("name")]
    [MaxLength(100)]
    public string Name { get; set; } = "";

    [Column("description")]
    public string? Description { get; set; }

    /// <summary>
    /// JSON array of permission strings: ["doc:read:all", "wiki:edit:own_dept"]
    /// </summary>
    [Column("permissions")]
    public string PermissionsJson { get; set; } = "[]";

    [Column("is_system")]
    public bool IsSystem { get; set; } = false;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public List<Employee> Employees { get; set; } = [];

    [NotMapped]
    public List<string> Permissions
    {
        get => System.Text.Json.JsonSerializer.Deserialize<List<string>>(PermissionsJson) ?? [];
        set => PermissionsJson = System.Text.Json.JsonSerializer.Serialize(value);
    }
}
