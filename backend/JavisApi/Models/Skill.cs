using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JavisApi.Models;

[Table("skills")]
public class Skill
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("name")]
    [MaxLength(200)]
    public string Name { get; set; } = "";

    [Column("slug")]
    [MaxLength(200)]
    public string Slug { get; set; } = "";

    [Column("description")]
    public string? Description { get; set; }

    [Column("scope_type")]
    [MaxLength(20)]
    public string ScopeType { get; set; } = "global";

    [Column("scope_id")]
    public Guid? ScopeId { get; set; }

    [Column("current_version")]
    public int CurrentVersion { get; set; } = 1;

    [Column("version_hash")]
    [MaxLength(64)]
    public string? VersionHash { get; set; }

    [Column("storage_path")]
    [MaxLength(1000)]
    public string? StoragePath { get; set; }

    /// <summary>"active"|"processing"|"deleting"|"deprecated"|"archived"</summary>
    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "active";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<SkillDepartment> SkillDepartments { get; set; } = [];
    public List<SkillVersion> Versions { get; set; } = [];
}

[Table("skill_departments")]
public class SkillDepartment
{
    [Column("skill_id")]
    public Guid SkillId { get; set; }

    [Column("department_id")]
    public Guid DepartmentId { get; set; }

    [ForeignKey(nameof(SkillId))]
    public Skill? Skill { get; set; }

    [ForeignKey(nameof(DepartmentId))]
    public Department? Department { get; set; }
}

[Table("skill_versions")]
public class SkillVersion
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("skill_id")]
    public Guid SkillId { get; set; }

    [Column("version_number")]
    public int VersionNumber { get; set; }

    [Column("version_hash")]
    [MaxLength(64)]
    public string? VersionHash { get; set; }

    [Column("storage_path")]
    [MaxLength(1000)]
    public string? StoragePath { get; set; }

    [Column("changelog")]
    public string? Changelog { get; set; }

    [Column("created_by")]
    public Guid? CreatedById { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(SkillId))]
    public Skill? Skill { get; set; }

    [ForeignKey(nameof(CreatedById))]
    public Employee? Author { get; set; }
}
