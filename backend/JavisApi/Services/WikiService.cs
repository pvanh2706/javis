using JavisApi.Data;
using JavisApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace JavisApi.Services;

public partial class WikiService
{
    private readonly AppDbContext _db;
    private readonly ChromaService _chroma;
    private readonly AuditService _audit;

    public WikiService(AppDbContext db, ChromaService chroma, AuditService audit)
    {
        _db = db;
        _chroma = chroma;
        _audit = audit;
    }

    // -----------------------------------------------------------------------
    // CRUD
    // -----------------------------------------------------------------------

    public async Task<WikiPage?> GetBySlugAsync(string slug, string scopeType = "global", Guid? scopeId = null)
    {
        return await _db.WikiPages.FirstOrDefaultAsync(p =>
            p.Slug == slug && p.ScopeType == scopeType && p.ScopeId == scopeId);
    }

    public async Task<WikiPage> CreatePageAsync(
        string slug, string title, string pageType,
        string contentMd, string summary,
        string scopeType, Guid? scopeId,
        List<string>? knowledgeTypeSlugs,
        List<Guid>? sourceIds,
        Guid? changedById, string changeType = "agent_compile",
        float[]? embedding = null)
    {
        var page = new WikiPage
        {
            Slug = slug,
            Title = title,
            PageType = pageType,
            ContentMd = contentMd,
            Summary = summary,
            ScopeType = scopeType,
            ScopeId = scopeId,
            Version = 1
        };

        if (knowledgeTypeSlugs is not null) page.KnowledgeTypeSlugs = knowledgeTypeSlugs;
        if (sourceIds is not null) page.SourceIds = sourceIds;

        _db.WikiPages.Add(page);
        await _db.SaveChangesAsync();

        // Save revision snapshot
        await SaveRevisionAsync(page, changeType, null, changedById, null);

        // Refresh wiki links
        await RefreshLinksAsync(page.Slug, contentMd);

        // Store embedding in ChromaDB
        if (embedding is not null)
            await _chroma.UpsertAsync(ChromaCollection(scopeType, scopeId), page.Slug, embedding,
                new() { ["title"] = title, ["scope_type"] = scopeType });

        return page;
    }

    public async Task<WikiPage> UpdatePageAsync(
        WikiPage page, string contentMd, string summary, string title,
        Guid? changedById, string changeType = "editor_edit", string? changeNote = null,
        float[]? embedding = null)
    {
        page.ContentMd = contentMd;
        page.Summary = summary;
        page.Title = title;
        page.Version++;
        page.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        await SaveRevisionAsync(page, changeType, null, changedById, changeNote);
        await RefreshLinksAsync(page.Slug, contentMd);

        if (embedding is not null)
            await _chroma.UpsertAsync(ChromaCollection(page.ScopeType, page.ScopeId), page.Slug, embedding,
                new() { ["title"] = title, ["scope_type"] = page.ScopeType });

        return page;
    }

    public async Task DeletePageAsync(WikiPage page)
    {
        await _chroma.DeleteAsync(ChromaCollection(page.ScopeType, page.ScopeId), page.Slug);

        // Remove links
        var outLinks = _db.WikiLinks.Where(l => l.FromSlug == page.Slug);
        _db.WikiLinks.RemoveRange(outLinks);

        _db.WikiPages.Remove(page);
        await _db.SaveChangesAsync();
    }

    // -----------------------------------------------------------------------
    // Search
    // -----------------------------------------------------------------------

    public async Task<List<WikiPage>> FullTextSearchAsync(
        string query, string? scopeType = null, Guid? scopeId = null, int limit = 20)
    {
        var q = _db.WikiPages.AsQueryable();

        if (scopeType is not null) q = q.Where(p => p.ScopeType == scopeType);
        if (scopeId.HasValue) q = q.Where(p => p.ScopeId == scopeId);

        // SQLite LIKE-based search
        var lower = query.ToLower();
        return await q
            .Where(p => EF.Functions.Like(p.Title.ToLower(), $"%{lower}%") ||
                        EF.Functions.Like(p.ContentMd.ToLower(), $"%{lower}%") ||
                        EF.Functions.Like(p.Summary.ToLower(), $"%{lower}%"))
            .OrderByDescending(p => p.UpdatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<WikiPage>> SemanticSearchAsync(
        List<string> slugs, string scopeType = "global", Guid? scopeId = null)
    {
        if (slugs.Count == 0) return [];
        return await _db.WikiPages
            .Where(p => slugs.Contains(p.Slug) && p.ScopeType == scopeType && p.ScopeId == scopeId)
            .ToListAsync();
    }

    // -----------------------------------------------------------------------
    // Links
    // -----------------------------------------------------------------------

    public async Task RefreshLinksAsync(string slug, string contentMd)
    {
        // Remove existing outlinks from this page
        var old = _db.WikiLinks.Where(l => l.FromSlug == slug);
        _db.WikiLinks.RemoveRange(old);

        // Parse [[slug]] patterns
        var matches = WikiLinkRegex().Matches(contentMd);
        var newLinks = matches
            .Select(m => m.Groups[1].Value.Trim().ToLower())
            .Distinct()
            .Where(s => s != slug)
            .Select(toSlug => new WikiLink { FromSlug = slug, ToSlug = toSlug });

        _db.WikiLinks.AddRange(newLinks);
        await _db.SaveChangesAsync();
    }

    public async Task<(List<string> Outlinks, List<string> Backlinks)> GetLinksAsync(string slug)
    {
        var outlinks = await _db.WikiLinks
            .Where(l => l.FromSlug == slug)
            .Select(l => l.ToSlug).ToListAsync();

        var backlinks = await _db.WikiLinks
            .Where(l => l.ToSlug == slug)
            .Select(l => l.FromSlug).ToListAsync();

        return (outlinks, backlinks);
    }

    // -----------------------------------------------------------------------
    // Drafts
    // -----------------------------------------------------------------------

    public async Task<WikiPageDraft> CreateDraftAsync(
        Guid pageId, Guid? authorId, string contentMd, string? note, string source = "web_ui")
    {
        var draft = new WikiPageDraft
        {
            PageId = pageId,
            AuthorId = authorId,
            ContentMd = contentMd,
            Note = note,
            Source = source,
            Status = "pending"
        };
        _db.WikiPageDrafts.Add(draft);
        await _db.SaveChangesAsync();
        return draft;
    }

    public async Task<WikiPageDraft?> ApproveDraftAsync(
        Guid draftId, Guid reviewerId, string? reviewerNote, float[]? embedding = null)
    {
        var draft = await _db.WikiPageDrafts
            .Include(d => d.Page)
            .FirstOrDefaultAsync(d => d.Id == draftId && d.Status == "pending");

        if (draft?.Page is null) return null;

        draft.Status = "approved";
        draft.ReviewedById = reviewerId;
        draft.ReviewedAt = DateTime.UtcNow;
        draft.ReviewerNote = reviewerNote;

        await UpdatePageAsync(
            draft.Page, draft.ContentMd, draft.Page.Summary, draft.Page.Title,
            reviewerId, "draft_approved", reviewerNote, embedding);

        await _db.SaveChangesAsync();
        return draft;
    }

    public async Task<WikiPageDraft?> RejectDraftAsync(
        Guid draftId, Guid reviewerId, string? reviewerNote)
    {
        var draft = await _db.WikiPageDrafts
            .FirstOrDefaultAsync(d => d.Id == draftId && d.Status == "pending");

        if (draft is null) return null;

        draft.Status = "rejected";
        draft.ReviewedById = reviewerId;
        draft.ReviewedAt = DateTime.UtcNow;
        draft.ReviewerNote = reviewerNote;

        await _db.SaveChangesAsync();
        return draft;
    }

    // -----------------------------------------------------------------------
    // Revisions
    // -----------------------------------------------------------------------

    public async Task SaveRevisionAsync(
        WikiPage page, string changeType, Guid? draftId,
        Guid? changedById, string? changeNote)
    {
        var revision = new WikiPageRevision
        {
            PageId = page.Id,
            Version = page.Version,
            ContentMd = page.ContentMd,
            ChangeType = changeType,
            DraftId = draftId,
            ChangedById = changedById,
            ChangeNote = changeNote
        };
        _db.WikiPageRevisions.Add(revision);
        await _db.SaveChangesAsync();
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static string ChromaCollection(string scopeType, Guid? scopeId) =>
        scopeType == "project" && scopeId.HasValue
            ? $"wiki_pages_project_{scopeId}"
            : "wiki_pages_global";

    public async Task<string> BuildWikiIndexAsync(string scopeType = "global", Guid? scopeId = null)
    {
        var pages = await _db.WikiPages
            .Where(p => p.ScopeType == scopeType && p.ScopeId == scopeId)
            .OrderBy(p => p.Slug)
            .Select(p => new { p.Slug, p.Title, p.Summary })
            .ToListAsync();

        return string.Join("\n", pages.Select(p => $"[[{p.Slug}]] — {p.Title}: {p.Summary}"));
    }

    [GeneratedRegex(@"\[\[([^\]]+)\]\]")]
    private static partial Regex WikiLinkRegex();
}
