using JavisApi.AI;
using System.Net.Http.Json;
using System.Text.Json;

namespace JavisApi.AI.Providers;

/// <summary>
/// Google Gemini provider.
/// Uses REST API directly (no official .NET SDK with tool calling yet).
/// </summary>
public class GoogleProvider : ILlmProvider, IEmbeddingProvider
{
    private readonly string _apiKey;
    private readonly string _llmModel;
    private readonly string _embeddingModel;
    private readonly HttpClient _http;

    public string ProviderName => "google";
    public int Dimensions => 768;

    public GoogleProvider(string apiKey,
        string llmModel = "gemini-2.0-flash",
        string embeddingModel = "text-embedding-004")
    {
        _apiKey = apiKey;
        _llmModel = llmModel;
        _embeddingModel = embeddingModel;
        _http = new HttpClient { BaseAddress = new Uri("https://generativelanguage.googleapis.com") };
    }

    public async Task<string> CompleteAsync(string systemPrompt, string userMessage,
        CancellationToken ct = default)
    {
        var body = new
        {
            system_instruction = new { parts = new[] { new { text = systemPrompt } } },
            contents = new[] { new { role = "user", parts = new[] { new { text = userMessage } } } }
        };

        var res = await _http.PostAsJsonAsync(
            $"/v1beta/models/{_llmModel}:generateContent?key={_apiKey}", body, ct);
        res.EnsureSuccessStatusCode();

        var json = await res.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        return json
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString() ?? "";
    }

    public async Task<LlmToolCallResponse> CompleteWithToolsAsync(
        string systemPrompt,
        List<ChatMessage> messages,
        List<ToolDefinition> tools,
        CancellationToken ct = default)
    {
        var contents = messages.Select(m => new
        {
            role = m.Role == "assistant" ? "model" : "user",
            parts = new[] { new { text = m.Content } }
        }).ToList();

        var geminiTools = new[]
        {
            new
            {
                function_declarations = tools.Select(t => new
                {
                    name = t.Name,
                    description = t.Description,
                    parameters = t.InputSchema
                }).ToList()
            }
        };

        var body = new
        {
            system_instruction = string.IsNullOrEmpty(systemPrompt) ? null :
                new { parts = new[] { new { text = systemPrompt } } },
            contents,
            tools = geminiTools
        };

        var res = await _http.PostAsJsonAsync(
            $"/v1beta/models/{_llmModel}:generateContent?key={_apiKey}",
            body, ct);
        res.EnsureSuccessStatusCode();

        var json = await res.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        var candidate = json.GetProperty("candidates")[0];
        var parts = candidate.GetProperty("content").GetProperty("parts");

        // Check for function call
        foreach (var part in parts.EnumerateArray())
        {
            if (part.TryGetProperty("functionCall", out var fc))
            {
                var fnName = fc.GetProperty("name").GetString() ?? "";
                var argsJson = fc.GetProperty("args").GetRawText();
                var input = JsonSerializer.Deserialize<Dictionary<string, object?>>(argsJson) ?? [];

                return new LlmToolCallResponse
                {
                    IsFinish = false,
                    ToolName = fnName,
                    ToolUseId = Guid.NewGuid().ToString(),
                    ToolInput = input
                };
            }
        }

        var text = parts[0].GetProperty("text").GetString() ?? "";
        return new LlmToolCallResponse { IsFinish = true, TextContent = text };
    }

    public async Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
    {
        var body = new { content = new { parts = new[] { new { text } } } };
        var res = await _http.PostAsJsonAsync(
            $"/v1beta/models/{_embeddingModel}:embedContent?key={_apiKey}", body, ct);
        res.EnsureSuccessStatusCode();

        var json = await res.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        return json.GetProperty("embedding")
            .GetProperty("values")
            .EnumerateArray()
            .Select(v => v.GetSingle())
            .ToArray();
    }

    public async Task<List<float[]>> EmbedBatchAsync(List<string> texts, CancellationToken ct = default)
    {
        var tasks = texts.Select(t => EmbedAsync(t, ct));
        var results = await Task.WhenAll(tasks);
        return [.. results];
    }
}
