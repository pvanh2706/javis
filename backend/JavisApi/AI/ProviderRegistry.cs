using JavisApi.AI.Providers;
using JavisApi.Services;

namespace JavisApi.AI;

/// <summary>
/// Resolves the configured LLM and embedding providers from the DB config.
/// Config keys: "llm_provider", "llm_model", "llm_api_key",
///              "embedding_provider", "embedding_model", "embedding_api_key"
/// </summary>
public class ProviderRegistry
{
    private readonly ConfigService _config;
    private ILlmProvider? _llm;
    private IEmbeddingProvider? _embedding;

    public ProviderRegistry(ConfigService config)
    {
        _config = config;
    }

    public async Task<ILlmProvider> GetLlmAsync()
    {
        if (_llm is not null) return _llm;

        var provider = await _config.GetAsync("llm_provider") ?? "openai";
        var model = await _config.GetAsync("llm_model");
        var apiKey = await _config.GetAsync("llm_api_key") ?? "";

        _llm = provider switch
        {
            "anthropic" => new AnthropicProvider(apiKey, model ?? "claude-3-5-haiku-20241022"),
            "google" => new GoogleProvider(apiKey, model ?? "gemini-2.0-flash"),
            _ => new OpenAiProvider(apiKey, model ?? "gpt-4o-mini")
        };

        return _llm;
    }

    public async Task<IEmbeddingProvider> GetEmbeddingAsync()
    {
        if (_embedding is not null) return _embedding;

        var provider = await _config.GetAsync("embedding_provider") ?? "openai";
        var model = await _config.GetAsync("embedding_model");
        var apiKey = await _config.GetAsync("embedding_api_key")
                     ?? await _config.GetAsync("llm_api_key") ?? "";

        _embedding = provider switch
        {
            "google" => (IEmbeddingProvider)new GoogleProvider(apiKey, embeddingModel: model ?? "text-embedding-004"),
            _ => new OpenAiProvider(apiKey, embeddingModel: model ?? "text-embedding-3-small")
        };

        return _embedding;
    }

    /// <summary>Call when config is updated to force re-initialization.</summary>
    public void Invalidate()
    {
        _llm = null;
        _embedding = null;
    }
}
