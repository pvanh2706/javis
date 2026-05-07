using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JavisApi.Models;

[Table("knowledge_types")]
public class KnowledgeType
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("slug")]
    [MaxLength(50)]
    public string Slug { get; set; } = "";

    [Column("name")]
    [MaxLength(100)]
    public string Name { get; set; } = "";

    [Column("color")]
    [MaxLength(20)]
    public string Color { get; set; } = "#6366f1";

    [Column("description")]
    public string? Description { get; set; }

    [Column("sort_order")]
    public int SortOrder { get; set; } = 0;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
