using JavisApi.Data;
using JavisApi.Models;
using JavisApi.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace JavisApi.AI;

/// <summary>
/// Tool-calling agent loop that compiles wiki pages from source documents.
/// Equivalent to app/ai/wiki_agent.py
/// </summary>
public class WikiAgent
{
    private const int MaxSteps = 50;
    private const int MinPageWords = 80;

    private readonly ProviderRegistry _registry;
    private readonly WikiService _wikiService;
    private readonly AppDbContext _db;
    private readonly ILogger<WikiAgent> _logger;

    public WikiAgent(
        ProviderRegistry registry,
        WikiService wikiService,
        AppDbContext db,
        ILogger<WikiAgent> logger)
    {
        _registry = registry;
        _wikiService = wikiService;
        _db = db;
        _logger = logger;
    }

    public async Task RunAsync(Guid sourceId, CancellationToken ct = default)
    {
        var source = await _db.Sources.FindAsync([sourceId], ct);
        if (source is null) return;

        var llm = await _registry.GetLlmAsync();
        var embeddingProvider = await _registry.GetEmbeddingAsync();

        var systemPrompt = BuildSystemPrompt();
        var messages = new List<ChatMessage>
        {
            new("user", await BuildInitialMessageAsync(source))
        };

        var tools = BuildTools();
        bool finished = false;

        for (int step = 0; step < MaxSteps && !finished; step++)
        {
            UpdateProgress(source, (int)(20 + step * 1.5), $"Agent step {step + 1}/{MaxSteps}...");
            await _db.SaveChangesAsync(ct);

            LlmToolCallResponse response;
            try
            {
                response = await llm.CompleteWithToolsAsync(systemPrompt, messages, tools, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LLM call failed at step {Step}", step);
                break;
            }

            if (response.IsFinish)
            {
                finished = true;
                break;
            }

            if (response.ToolName == "finish")
            {
                finished = true;
                break;
            }

            // Add assistant message with tool call
            messages.Add(new ChatMessage("assistant",
                $"[Tool call: {response.ToolName}]", response.ToolUseId));

            // Execute tool
            var toolResult = await DispatchToolAsync(
                response.ToolName!, response.ToolInput ?? [],
                source, embeddingProvider, ct);

            // Add tool result
            messages.Add(new ChatMessage("tool", toolResult, response.ToolUseId));
        }

        source.Status = "ready";
        source.Progress = 100;
        source.ProgressMessage = "Compilation complete";
        await _db.SaveChangesAsync(ct);
    }

    // -----------------------------------------------------------------------
    // Tool dispatcher
    // -----------------------------------------------------------------------

    private async Task<string> DispatchToolAsync(
        string toolName,
        Dictionary<string, object?> input,
        Source source,
        IEmbeddingProvider embeddingProvider,
        CancellationToken ct)
    {
        try
        {
            return toolName switch
            {
                "read_wiki_index" => await ReadWikiIndexAsync(source),
                "read_wiki_page" => await ReadWikiPageAsync(GetStr(input, "slug"), source),
                "search_wiki" => await SearchWikiAsync(GetStr(input, "query"), source),
                "read_source_excerpt" => ReadSourceExcerpt(source,
                    GetInt(input, "char_start"), GetInt(input, "char_end")),
                "create_page" => await CreatePageAsync(input, source, embeddingProvider, ct),
                "update_page" => await UpdatePageAsync(input, source, embeddingProvider, ct),
                "append_log" => await AppendLogAsync(GetStr(input, "entry"), source),
                "finish" => "Agent finished.",
                _ => $"Unknown tool: {toolName}"
            };
        }
        catch (Exception ex)
        {
            return $"Tool error: {ex.Message}";
        }
    }

    private async Task<string> ReadWikiIndexAsync(Source source)
    {
        var index = await _wikiService.BuildWikiIndexAsync(source.ScopeType, source.ScopeId);
        return string.IsNullOrWhiteSpace(index) ? "(no wiki pages yet)" : index;
    }

    private async Task<string> ReadWikiPageAsync(string slug, Source source)
    {
        var page = await _wikiService.GetBySlugAsync(slug, source.ScopeType, source.ScopeId);
        if (page is null) return $"Page not found: {slug}";
        return $"# {page.Title}\n\n{page.ContentMd}";
    }

    private async Task<string> SearchWikiAsync(string query, Source source)
    {
        var results = await _wikiService.FullTextSearchAsync(
            query, source.ScopeType, source.ScopeId, limit: 5);
        if (results.Count == 0) return "No matching pages found.";
        return string.Join("\n", results.Select(p => $"[[{p.Slug}]] — {p.Title}: {p.Summary}"));
    }

    private static string ReadSourceExcerpt(Source source, int charStart, int charEnd)
    {
        var text = source.FullText ?? "";
        charStart = Math.Clamp(charStart, 0, text.Length);
        charEnd = Math.Clamp(charEnd, charStart, Math.Min(charStart + 4000, text.Length));
        return text[charStart..charEnd];
    }

    private async Task<string> CreatePageAsync(
        Dictionary<string, object?> input,
        Source source,
        IEmbeddingProvider emb,
        CancellationToken ct)
    {
        var slug = GetStr(input, "slug").ToLower().Replace(" ", "-");
        var title = GetStr(input, "title");
        var contentMd = GetStr(input, "content_md");
        var summary = GetStr(input, "summary");

        if (contentMd.Split(' ').Length < MinPageWords)
            return $"Error: content too short (min {MinPageWords} words)";

        var existing = await _wikiService.GetBySlugAsync(slug, source.ScopeType, source.ScopeId);
        if (existing is not null)
            return $"Page already exists: {slug}. Use update_page instead.";

        float[]? embedding = null;
        try { embedding = await emb.EmbedAsync($"{title}\n{summary}", ct); }
        catch { /* embedding optional */ }

        await _wikiService.CreatePageAsync(
            slug, title, "article", contentMd, summary,
            source.ScopeType, source.ScopeId,
            null, [source.Id], null, "agent_compile", embedding);

        return $"Created page: [[{slug}]]";
    }

    private async Task<string> UpdatePageAsync(
        Dictionary<string, object?> input,
        Source source,
        IEmbeddingProvider emb,
        CancellationToken ct)
    {
        var slug = GetStr(input, "slug");
        var contentMd = GetStr(input, "content_md");
        var summary = GetStr(input, "summary");

        var page = await _wikiService.GetBySlugAsync(slug, source.ScopeType, source.ScopeId);
        if (page is null) return $"Page not found: {slug}. Use create_page.";

        float[]? embedding = null;
        try { embedding = await emb.EmbedAsync($"{page.Title}\n{summary}", ct); }
        catch { /* embedding optional */ }

        await _wikiService.UpdatePageAsync(
            page, contentMd, summary, page.Title,
            null, "agent_compile", null, embedding);

        return $"Updated page: [[{slug}]]";
    }

    private async Task<string> AppendLogAsync(string entry, Source source)
    {
        const string logSlug = "_log";
        var log = await _wikiService.GetBySlugAsync(logSlug, source.ScopeType, source.ScopeId);
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm");
        var newEntry = $"\n- [{timestamp}] {entry}";

        if (log is null)
        {
            await _wikiService.CreatePageAsync(
                logSlug, "Activity Log", "log",
                $"# Activity Log\n{newEntry}", "Chronological activity log.",
                source.ScopeType, source.ScopeId, null, null, null, "agent_compile");
        }
        else
        {
            await _wikiService.UpdatePageAsync(
                log, log.ContentMd + newEntry, log.Summary, log.Title,
                null, "agent_compile");
        }

        return "Log entry added.";
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static string BuildSystemPrompt() => """
        You are a Knowledge Wiki Compiler. Your job is to read source documents and maintain a structured wiki.

        RULES:
        - Write encyclopedic pages, not bullet summaries. Minimum 80 words per page.
        - Preserve exact numbers, dates, names, regulations, procedures.
        - Drop marketing language, boilerplate, and source-specific framing.
        - Write in the same language as the source document.
        - Use [[slug]] wikilinks for first mentions of entities and concepts.
        - Create focused pages (one concept per page).
        - Update existing pages rather than duplicating content.

        WORKFLOW:
        1. Call read_wiki_index to see existing pages.
        2. Call read_source_excerpt to read sections of the source.
        3. Create or update wiki pages as needed.
        4. Call finish when done.
        """;

    private async Task<string> BuildInitialMessageAsync(Source source)
    {
        var index = await _wikiService.BuildWikiIndexAsync(source.ScopeType, source.ScopeId);
        var preview = (source.FullText ?? "")[..Math.Min(2000, source.FullText?.Length ?? 0)];

        return $"""
            Source document: "{source.Title ?? source.FileName ?? "Untitled"}"
            Total length: {source.FullText?.Length ?? 0} characters

            Current wiki index:
            {(string.IsNullOrWhiteSpace(index) ? "(empty)" : index)}

            Document preview:
            {preview}

            Please compile this document into the wiki.
            """;
    }

    private static List<ToolDefinition> BuildTools() =>
    [
        new() { Name = "read_wiki_index", Description = "List all existing wiki pages (slug, title, summary)" },
        new() { Name = "read_wiki_page", Description = "Read full content of a wiki page by slug",
            InputSchema = new { type = "object", properties = new { slug = new { type = "string" } }, required = new[] { "slug" } } },
        new() { Name = "search_wiki", Description = "Full-text search wiki pages",
            InputSchema = new { type = "object", properties = new { query = new { type = "string" } }, required = new[] { "query" } } },
        new() { Name = "read_source_excerpt", Description = "Read a section of the source document",
            InputSchema = new { type = "object", properties = new {
                char_start = new { type = "integer" }, char_end = new { type = "integer" } },
                required = new[] { "char_start", "char_end" } } },
        new() { Name = "create_page", Description = "Create a new wiki page",
            InputSchema = new { type = "object", properties = new {
                slug = new { type = "string" }, title = new { type = "string" },
                content_md = new { type = "string" }, summary = new { type = "string" } },
                required = new[] { "slug", "title", "content_md", "summary" } } },
        new() { Name = "update_page", Description = "Update an existing wiki page",
            InputSchema = new { type = "object", properties = new {
                slug = new { type = "string" },
                content_md = new { type = "string" }, summary = new { type = "string" } },
                required = new[] { "slug", "content_md", "summary" } } },
        new() { Name = "append_log", Description = "Add entry to the activity log",
            InputSchema = new { type = "object", properties = new { entry = new { type = "string" } },
                required = new[] { "entry" } } },
        new() { Name = "finish", Description = "Signal that compilation is complete" }
    ];

    private static string GetStr(Dictionary<string, object?> input, string key) =>
        input.TryGetValue(key, out var val) ? val?.ToString() ?? "" : "";

    private static int GetInt(Dictionary<string, object?> input, string key) =>
        input.TryGetValue(key, out var val) && int.TryParse(val?.ToString(), out var i) ? i : 0;

    private void UpdateProgress(Source source, int progress, string message)
    {
        source.Progress = progress;
        source.ProgressMessage = message;
    }
}
