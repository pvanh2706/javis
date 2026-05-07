using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JavisApi.Models;

[Table("departments")]
public class Department
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("name")]
    [MaxLength(200)]
    public string Name { get; set; } = "";

    [Column("description")]
    public string? Description { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public List<Employee> Employees { get; set; } = [];
    public List<SourceDepartment> SourceDepartments { get; set; } = [];
    public List<SkillDepartment> SkillDepartments { get; set; } = [];
}
