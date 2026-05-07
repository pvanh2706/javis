using JavisApi.Services;
using System.Text.Json;

namespace JavisApi.AI;

/// <summary>
/// Cheap first-pass analysis of a source document.
/// Returns a structural map: what pages to create/update.
/// </summary>
public class WikiAnalyzer
{
    private readonly ProviderRegistry _registry;

    public WikiAnalyzer(ProviderRegistry registry)
    {
        _registry = registry;
    }

    public async Task<WikiAnalysisResult> AnalyzeAsync(
        string sourceText, string wikiIndex, CancellationToken ct = default)
    {
        var llm = await _registry.GetLlmAsync();

        var systemPrompt = """
            You are a knowledge management analyst. 
            Your job is to analyze a source document and determine what wiki pages should be created or updated.
            Respond ONLY with a valid JSON object — no markdown, no explanation.
            """;

        var jsonTemplate = """
            {
              "document_type": "SOP|Policy|Technical|Product|HR|Other",
              "primary_topics": ["topic1", "topic2"],
              "pages_to_create": [{"slug": "slug-name", "title": "Page Title", "reason": "why create"}],
              "pages_to_update": [{"slug": "existing-slug", "reason": "why update"}],
              "entities": ["entity1", "entity2"]
            }
            """;

        var wikiIndexSection = string.IsNullOrWhiteSpace(wikiIndex)
            ? "(empty — this is the first document)"
            : wikiIndex;

        var preview = sourceText[..Math.Min(3000, sourceText.Length)];

        var userPrompt = $"""
            EXISTING WIKI INDEX (slug → title: summary):
            {wikiIndexSection}

            SOURCE DOCUMENT (first 3000 chars):
            {preview}

            Analyze and return JSON in this exact format:
            {jsonTemplate}
            """;

        var rawJson = await llm.CompleteAsync(systemPrompt, userPrompt, ct);

        // Strip markdown code fences if present
        rawJson = rawJson.Trim();
        if (rawJson.StartsWith("```")) rawJson = rawJson.Split('\n', 2)[1];
        if (rawJson.EndsWith("```")) rawJson = rawJson[..rawJson.LastIndexOf("```")];

        try
        {
            return JsonSerializer.Deserialize<WikiAnalysisResult>(rawJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? new WikiAnalysisResult();
        }
        catch
        {
            return new WikiAnalysisResult { DocumentType = "Other" };
        }
    }
}

public class WikiAnalysisResult
{
    public string DocumentType { get; set; } = "Other";
    public List<string> PrimaryTopics { get; set; } = [];
    public List<PageToCreate> PagesToCreate { get; set; } = [];
    public List<PageToUpdate> PagesToUpdate { get; set; } = [];
    public List<string> Entities { get; set; } = [];
}

public class PageToCreate
{
    public string Slug { get; set; } = "";
    public string Title { get; set; } = "";
    public string Reason { get; set; } = "";
}

public class PageToUpdate
{
    public string Slug { get; set; } = "";
    public string Reason { get; set; } = "";
}
