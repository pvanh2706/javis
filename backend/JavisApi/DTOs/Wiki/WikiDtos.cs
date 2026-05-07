namespace JavisApi.DTOs.Wiki;

public record WikiPageDto(
    Guid Id,
    string Slug,
    string Title,
    string PageType,
    string ContentMd,
    string Summary,
    string ScopeType,
    Guid? ScopeId,
    List<string> KnowledgeTypeSlugs,
    int Version,
    bool Orphaned,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record WikiPageSummaryDto(
    Guid Id,
    string Slug,
    string Title,
    string PageType,
    string Summary,
    string ScopeType,
    Guid? ScopeId,
    List<string> KnowledgeTypeSlugs,
    int Version,
    DateTime UpdatedAt
);

public record CreateWikiPageRequest(
    string Slug,
    string Title,
    string PageType,
    string ContentMd,
    string Summary,
    string ScopeType = "global",
    Guid? ScopeId = null,
    List<string>? KnowledgeTypeSlugs = null
);

public record UpdateWikiPageRequest(
    string Title,
    string ContentMd,
    string Summary,
    string? ChangeNote = null
);

public record WikiSearchRequest(
    string Query,
    string? ScopeType = null,
    Guid? ScopeId = null,
    int TopK = 10
);

public record WikiLinksDto(
    string Slug,
    List<string> Outlinks,
    List<string> Backlinks
);

public record WikiPageRevisionDto(
    Guid Id,
    int Version,
    string ContentMd,
    string ChangeType,
    Guid? ChangedById,
    string? ChangerName,
    string? ChangeNote,
    DateTime CreatedAt
);

public record WikiPageDraftDto(
    Guid Id,
    Guid PageId,
    string PageSlug,
    string PageTitle,
    Guid? AuthorId,
    string? AuthorName,
    string ContentMd,
    string? Note,
    string Status,
    string? ReviewerNote,
    DateTime CreatedAt
);

public record CreateDraftRequest(
    string ContentMd,
    string? Note = null
);

public record ReviewDraftRequest(
    string Decision, // "approve" or "reject"
    string? ReviewerNote = null
);
