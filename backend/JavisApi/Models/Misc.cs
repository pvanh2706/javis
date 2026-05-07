using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JavisApi.Models;

[Table("audit_log")]
public class AuditLog
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [Column("principal_id")]
    public Guid PrincipalId { get; set; }

    /// <summary>"human" or "agent"</summary>
    [Column("principal_type")]
    [MaxLength(20)]
    public string PrincipalType { get; set; } = "human";

    [Column("action")]
    [MaxLength(50)]
    public string Action { get; set; } = "";

    [Column("resource_type")]
    [MaxLength(50)]
    public string ResourceType { get; set; } = "";

    [Column("resource_id")]
    [MaxLength(100)]
    public string ResourceId { get; set; } = "";

    /// <summary>"allow" or "deny"</summary>
    [Column("decision")]
    [MaxLength(10)]
    public string Decision { get; set; } = "allow";

    [Column("reason")]
    public string? Reason { get; set; }

    [Column("metadata_json")]
    public string? MetadataJson { get; set; }
}

[Table("app_config")]
public class AppConfig
{
    [Key]
    [Column("key")]
    [MaxLength(100)]
    public string Key { get; set; } = "";

    [Column("value")]
    public string? Value { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

[Table("notes")]
public class Note
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("title")]
    [MaxLength(500)]
    public string? Title { get; set; }

    [Column("content")]
    public string? Content { get; set; }

    [Column("note_type")]
    [MaxLength(50)]
    public string? NoteType { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
