using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JavisApi.Models;

[Table("sources")]
public class Source
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("title")]
    [MaxLength(500)]
    public string? Title { get; set; }

    [Column("full_text")]
    public string? FullText { get; set; }

    /// <summary>"file" or "url"</summary>
    [Column("source_type")]
    [MaxLength(50)]
    public string? SourceType { get; set; }

    /// <summary>"global" or "project"</summary>
    [Column("scope_type")]
    [MaxLength(20)]
    public string ScopeType { get; set; } = "global";

    [Column("scope_id")]
    public Guid? ScopeId { get; set; }

    [Column("knowledge_type_id")]
    public Guid? KnowledgeTypeId { get; set; }

    [Column("contributed_by_employee_id")]
    public Guid? ContributedByEmployeeId { get; set; }

    [Column("file_path")]
    [MaxLength(1000)]
    public string? FilePath { get; set; }

    [Column("url")]
    [MaxLength(2000)]
    public string? Url { get; set; }

    [Column("minio_key")]
    [MaxLength(500)]
    public string? MinioKey { get; set; }

    [Column("file_name")]
    [MaxLength(500)]
    public string? FileName { get; set; }

    [Column("file_size")]
    public long? FileSize { get; set; }

    /// <summary>"pending" | "processing" | "ready" | "error"</summary>
    [Column("status")]
    [MaxLength(50)]
    public string Status { get; set; } = "pending";

    [Column("error_message")]
    public string? ErrorMessage { get; set; }

    [Column("progress")]
    public int Progress { get; set; } = 0;

    [Column("progress_message")]
    [MaxLength(500)]
    public string? ProgressMessage { get; set; }

    [Column("job_id")]
    [MaxLength(200)]
    public string? JobId { get; set; }

    /// <summary>JSON: heading-based TOC tree</summary>
    [Column("outline_json")]
    public string? OutlineJson { get; set; }

    /// <summary>JSON: char offsets per page</summary>
    [Column("page_offsets_json")]
    public string? PageOffsetsJson { get; set; }

    [Column("metadata_json")]
    public string? MetadataJson { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey(nameof(KnowledgeTypeId))]
    public KnowledgeType? KnowledgeType { get; set; }

    [ForeignKey(nameof(ContributedByEmployeeId))]
    public Employee? Contributor { get; set; }

    public List<SourceDepartment> SourceDepartments { get; set; } = [];
}

[Table("source_departments")]
public class SourceDepartment
{
    [Column("source_id")]
    public Guid SourceId { get; set; }

    [Column("department_id")]
    public Guid DepartmentId { get; set; }

    [ForeignKey(nameof(SourceId))]
    public Source? Source { get; set; }

    [ForeignKey(nameof(DepartmentId))]
    public Department? Department { get; set; }
}
