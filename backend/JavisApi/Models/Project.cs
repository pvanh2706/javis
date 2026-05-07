using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JavisApi.Models;

[Table("projects")]
public class Project
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("name")]
    [MaxLength(200)]
    public string Name { get; set; } = "";

    [Column("description")]
    public string? Description { get; set; }

    /// <summary>"project" or "customer"</summary>
    [Column("workspace_type")]
    [MaxLength(20)]
    public string WorkspaceType { get; set; } = "project";

    /// <summary>"active" or "archived"</summary>
    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "active";

    [Column("created_by_id")]
    public Guid? CreatedById { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey(nameof(CreatedById))]
    public Employee? CreatedBy { get; set; }

    public List<ProjectMember> Members { get; set; } = [];
    public List<ProjectSource> ProjectSources { get; set; } = [];
}

[Table("project_members")]
public class ProjectMember
{
    [Column("project_id")]
    public Guid ProjectId { get; set; }

    [Column("employee_id")]
    public Guid EmployeeId { get; set; }

    /// <summary>"viewer"|"contributor"|"editor"|"admin"</summary>
    [Column("role")]
    [MaxLength(20)]
    public string Role { get; set; } = "viewer";

    [Column("added_at")]
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(ProjectId))]
    public Project? Project { get; set; }

    [ForeignKey(nameof(EmployeeId))]
    public Employee? Employee { get; set; }
}

[Table("project_sources")]
public class ProjectSource
{
    [Column("project_id")]
    public Guid ProjectId { get; set; }

    [Column("source_id")]
    public Guid SourceId { get; set; }

    [Column("added_at")]
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(ProjectId))]
    public Project? Project { get; set; }

    [ForeignKey(nameof(SourceId))]
    public Source? Source { get; set; }
}
