using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace JavisApi.Models;

[Table("wiki_pages")]
public class WikiPage
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("slug")]
    [MaxLength(300)]
    public string Slug { get; set; } = "";

    [Column("title")]
    [MaxLength(500)]
    public string Title { get; set; } = "";

    [Column("page_type")]
    [MaxLength(30)]
    public string PageType { get; set; } = "article";

    [Column("content_md")]
    public string ContentMd { get; set; } = "";

    [Column("summary")]
    public string Summary { get; set; } = "";

    /// <summary>"global" or "project"</summary>
    [Column("scope_type")]
    [MaxLength(20)]
    public string ScopeType { get; set; } = "global";

    [Column("scope_id")]
    public Guid? ScopeId { get; set; }

    /// <summary>JSON array of knowledge type slugs</summary>
    [Column("knowledge_type_slugs_json")]
    public string KnowledgeTypeSlugsJson { get; set; } = "[]";

    /// <summary>JSON array of source IDs (as strings)</summary>
    [Column("source_ids_json")]
    public string SourceIdsJson { get; set; } = "[]";

    // Embedding stored in ChromaDB — NOT in SQLite
    [Column("version")]
    public int Version { get; set; } = 1;

    [Column("orphaned")]
    public bool Orphaned { get; set; } = false;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [NotMapped]
    public List<string> KnowledgeTypeSlugs
    {
        get => JsonSerializer.Deserialize<List<string>>(KnowledgeTypeSlugsJson) ?? [];
        set => KnowledgeTypeSlugsJson = JsonSerializer.Serialize(value);
    }

    [NotMapped]
    public List<Guid> SourceIds
    {
        get
        {
            var raw = JsonSerializer.Deserialize<List<string>>(SourceIdsJson) ?? [];
            return raw.Select(s => Guid.TryParse(s, out var g) ? g : Guid.Empty)
                      .Where(g => g != Guid.Empty).ToList();
        }
        set => SourceIdsJson = JsonSerializer.Serialize(value.Select(g => g.ToString()));
    }
}

[Table("wiki_links")]
public class WikiLink
{
    [Column("from_slug")]
    [MaxLength(300)]
    public string FromSlug { get; set; } = "";

    [Column("to_slug")]
    [MaxLength(300)]
    public string ToSlug { get; set; } = "";
}

[Table("wiki_page_drafts")]
public class WikiPageDraft
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("page_id")]
    public Guid PageId { get; set; }

    [Column("author_id")]
    public Guid? AuthorId { get; set; }

    [Column("content_md")]
    public string ContentMd { get; set; } = "";

    [Column("note")]
    public string? Note { get; set; }

    /// <summary>"pending" | "approved" | "rejected"</summary>
    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "pending";

    [Column("source")]
    [MaxLength(40)]
    public string Source { get; set; } = "web_ui";

    [Column("source_metadata_json")]
    public string? SourceMetadataJson { get; set; }

    [Column("reviewed_by_id")]
    public Guid? ReviewedById { get; set; }

    [Column("reviewed_at")]
    public DateTime? ReviewedAt { get; set; }

    [Column("reviewer_note")]
    public string? ReviewerNote { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(PageId))]
    public WikiPage? Page { get; set; }

    [ForeignKey(nameof(AuthorId))]
    public Employee? Author { get; set; }

    [ForeignKey(nameof(ReviewedById))]
    public Employee? Reviewer { get; set; }
}

[Table("wiki_page_revisions")]
public class WikiPageRevision
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("page_id")]
    public Guid PageId { get; set; }

    [Column("version")]
    public int Version { get; set; }

    [Column("content_md")]
    public string ContentMd { get; set; } = "";

    /// <summary>"agent_compile"|"editor_edit"|"draft_approved"|"manual_rebuild"|"rollback"</summary>
    [Column("change_type")]
    [MaxLength(30)]
    public string ChangeType { get; set; } = "agent_compile";

    [Column("draft_id")]
    public Guid? DraftId { get; set; }

    [Column("changed_by_id")]
    public Guid? ChangedById { get; set; }

    [Column("change_note")]
    public string? ChangeNote { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(PageId))]
    public WikiPage? Page { get; set; }

    [ForeignKey(nameof(ChangedById))]
    public Employee? ChangedBy { get; set; }
}
