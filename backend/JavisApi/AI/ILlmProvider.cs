namespace JavisApi.AI;

public interface ILlmProvider
{
    string ProviderName { get; }

    Task<string> CompleteAsync(string systemPrompt, string userMessage,
        CancellationToken ct = default);

    Task<LlmToolCallResponse> CompleteWithToolsAsync(
        string systemPrompt,
        List<ChatMessage> messages,
        List<ToolDefinition> tools,
        CancellationToken ct = default);
}

public interface IEmbeddingProvider
{
    string ProviderName { get; }
    int Dimensions { get; }

    Task<float[]> EmbedAsync(string text, CancellationToken ct = default);
    Task<List<float[]>> EmbedBatchAsync(List<string> texts, CancellationToken ct = default);
}

// -----------------------------------------------------------------------
// Shared message/tool types
// -----------------------------------------------------------------------

public record ChatMessage(string Role, string Content, string? ToolUseId = null);

public class ToolDefinition
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public object InputSchema { get; set; } = new { };
}

public class LlmToolCallResponse
{
    public bool IsFinish { get; set; }
    public string? TextContent { get; set; }
    public string? ToolName { get; set; }
    public string? ToolUseId { get; set; }
    public Dictionary<string, object?>? ToolInput { get; set; }
    public string? StopReason { get; set; }
}
