using Anthropic.SDK;
using Anthropic.SDK.Common;
using Anthropic.SDK.Messaging;
using JavisApi.AI;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace JavisApi.AI.Providers;

public class AnthropicProvider : ILlmProvider
{
    private readonly AnthropicClient _client;
    private readonly string _model;

    public string ProviderName => "anthropic";

    public AnthropicProvider(string apiKey, string model = "claude-3-5-haiku-20241022")
    {
        _client = new AnthropicClient(new APIAuthentication(apiKey));
        _model = model;
    }

    public async Task<string> CompleteAsync(string systemPrompt, string userMessage,
        CancellationToken ct = default)
    {
        var request = new MessageParameters
        {
            Model = _model,
            System = [new SystemMessage(systemPrompt)],
            Messages = [new Message { Role = RoleType.User, Content = [new TextContent { Text = userMessage }] }],
            MaxTokens = 4096
        };

        var response = await _client.Messages.GetClaudeMessageAsync(request, ct);
        return response.Content.OfType<TextContent>().FirstOrDefault()?.Text ?? "";
    }

    public async Task<LlmToolCallResponse> CompleteWithToolsAsync(
        string systemPrompt,
        List<ChatMessage> messages,
        List<ToolDefinition> tools,
        CancellationToken ct = default)
    {
        var anthropicMessages = messages
            .Where(m => m.Role != "tool")
            .Select(m => new Message
            {
                Role = m.Role == "user" ? RoleType.User : RoleType.Assistant,
                Content = [new TextContent { Text = m.Content }]
            }).ToList();

        var anthropicTools = tools.Select(t => new Anthropic.SDK.Common.Tool(
            new Function(t.Name, t.Description,
                JsonNode.Parse("{\"type\":\"object\",\"properties\":{}}")!)
        )).ToList();

        var request = new MessageParameters
        {
            Model = _model,
            System = string.IsNullOrEmpty(systemPrompt) ? null : [new SystemMessage(systemPrompt)],
            Messages = anthropicMessages,
            Tools = anthropicTools,
            MaxTokens = 4096
        };

        var response = await _client.Messages.GetClaudeMessageAsync(request, ct);

        var toolUse = response.Content.OfType<ToolUseContent>().FirstOrDefault();
        if (toolUse is not null)
        {
            var inputDict = JsonSerializer.Deserialize<Dictionary<string, object?>>(
                JsonSerializer.Serialize(toolUse.Input)) ?? [];

            return new LlmToolCallResponse
            {
                IsFinish = false,
                ToolName = toolUse.Name,
                ToolUseId = toolUse.Id,
                ToolInput = inputDict,
                StopReason = response.StopReason
            };
        }

        var text = response.Content.OfType<TextContent>().FirstOrDefault()?.Text ?? "";
        return new LlmToolCallResponse
        {
            IsFinish = true,
            TextContent = text,
            StopReason = response.StopReason
        };
    }
}
