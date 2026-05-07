using Azure.AI.OpenAI;
using JavisApi.AI;
using OpenAI.Chat;
using OpenAI.Embeddings;
using System.Text.Json;
using System.ClientModel;

namespace JavisApi.AI.Providers;

public class OpenAiProvider : ILlmProvider, IEmbeddingProvider
{
    private readonly AzureOpenAIClient? _azureClient;
    private readonly OpenAI.OpenAIClient? _openAiClient;
    private readonly string _llmModel;
    private readonly string _embeddingModel;
    private readonly bool _isAzure;

    public string ProviderName => "openai";
    public int Dimensions => 1536;

    public OpenAiProvider(string apiKey,
        string llmModel = "gpt-4o-mini",
        string embeddingModel = "text-embedding-3-small",
        string? azureEndpoint = null)
    {
        _llmModel = llmModel;
        _embeddingModel = embeddingModel;

        if (azureEndpoint is not null)
        {
            _azureClient = new AzureOpenAIClient(new Uri(azureEndpoint), new ApiKeyCredential(apiKey));
            _isAzure = true;
        }
        else
        {
            _openAiClient = new OpenAI.OpenAIClient(new ApiKeyCredential(apiKey));
            _isAzure = false;
        }
    }

    private ChatClient GetChatClient() =>
        _isAzure
            ? _azureClient!.GetChatClient(_llmModel)
            : _openAiClient!.GetChatClient(_llmModel);

    private EmbeddingClient GetEmbeddingClient() =>
        _isAzure
            ? _azureClient!.GetEmbeddingClient(_embeddingModel)
            : _openAiClient!.GetEmbeddingClient(_embeddingModel);

    public async Task<string> CompleteAsync(string systemPrompt, string userMessage,
        CancellationToken ct = default)
    {
        var client = GetChatClient();
        var messages = new List<OpenAI.Chat.ChatMessage>
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userMessage)
        };

        var response = await client.CompleteChatAsync(messages, cancellationToken: ct);
        return response.Value.Content[0].Text;
    }

    public async Task<LlmToolCallResponse> CompleteWithToolsAsync(
        string systemPrompt,
        List<ChatMessage> messages,
        List<ToolDefinition> tools,
        CancellationToken ct = default)
    {
        var client = GetChatClient();

        var openAiMessages = new List<OpenAI.Chat.ChatMessage>();
        if (!string.IsNullOrEmpty(systemPrompt))
            openAiMessages.Add(new SystemChatMessage(systemPrompt));

        foreach (var m in messages)
        {
            openAiMessages.Add(m.Role switch
            {
                "user" => (OpenAI.Chat.ChatMessage)new UserChatMessage(m.Content),
                "assistant" => new AssistantChatMessage(m.Content),
                "tool" => new ToolChatMessage(m.ToolUseId ?? "", m.Content),
                _ => new UserChatMessage(m.Content)
            });
        }

        var chatTools = tools.Select(t => ChatTool.CreateFunctionTool(
            functionName: t.Name,
            functionDescription: t.Description,
            functionParameters: BinaryData.FromObjectAsJson(t.InputSchema)
        )).ToList();

        var options = new ChatCompletionOptions();
        foreach (var tool in chatTools)
            options.Tools.Add(tool);

        var response = await client.CompleteChatAsync(openAiMessages, options, cancellationToken: ct);
        var completion = response.Value;

        if (completion.FinishReason == ChatFinishReason.ToolCalls)
        {
            var toolCall = completion.ToolCalls[0];
            var input = JsonSerializer.Deserialize<Dictionary<string, object?>>(
                toolCall.FunctionArguments.ToString()) ?? [];

            return new LlmToolCallResponse
            {
                IsFinish = false,
                ToolName = toolCall.FunctionName,
                ToolUseId = toolCall.Id,
                ToolInput = input,
                StopReason = "tool_use"
            };
        }

        return new LlmToolCallResponse
        {
            IsFinish = true,
            TextContent = completion.Content[0].Text,
            StopReason = completion.FinishReason.ToString()
        };
    }

    public async Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
    {
        var client = GetEmbeddingClient();
        var response = await client.GenerateEmbeddingAsync(text, cancellationToken: ct);
        return response.Value.ToFloats().ToArray();
    }

    public async Task<List<float[]>> EmbedBatchAsync(List<string> texts, CancellationToken ct = default)
    {
        var client = GetEmbeddingClient();
        var response = await client.GenerateEmbeddingsAsync(texts, cancellationToken: ct);
        return response.Value.Select(e => e.ToFloats().ToArray()).ToList();
    }
}
