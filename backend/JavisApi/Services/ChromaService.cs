using System.Net.Http.Json;
using System.Text.Json;

namespace JavisApi.Services;

/// <summary>
/// Client for ChromaDB HTTP API (v1).
/// ChromaDB runs separately: docker run -p 8001:8000 chromadb/chroma
/// </summary>
public class ChromaService
{
    private readonly HttpClient _http;
    private const string ApiBase = "/api/v1";

    public ChromaService(IHttpClientFactory httpClientFactory)
    {
        _http = httpClientFactory.CreateClient("ChromaDB");
    }

    public async Task EnsureCollectionAsync(string collection)
    {
        var payload = new { name = collection, metadata = new { } };
        var res = await _http.PostAsJsonAsync($"{ApiBase}/collections", payload);
        // 200 = created, 409 = already exists — both are fine
    }

    public async Task UpsertAsync(
        string collection,
        string documentId,
        float[] embedding,
        Dictionary<string, string>? metadata = null)
    {
        await EnsureCollectionAsync(collection);

        var collectionId = await GetCollectionIdAsync(collection);
        if (collectionId is null) return;

        var payload = new
        {
            ids = new[] { documentId },
            embeddings = new[] { embedding },
            metadatas = new[] { metadata ?? new Dictionary<string, string>() }
        };

        await _http.PostAsJsonAsync($"{ApiBase}/collections/{collectionId}/upsert", payload);
    }

    public async Task<List<string>> QueryAsync(
        string collection,
        float[] queryEmbedding,
        int topK = 10,
        Dictionary<string, string>? whereFilter = null)
    {
        var collectionId = await GetCollectionIdAsync(collection);
        if (collectionId is null) return [];

        var payload = new
        {
            query_embeddings = new[] { queryEmbedding },
            n_results = topK,
            where = whereFilter
        };

        var res = await _http.PostAsJsonAsync($"{ApiBase}/collections/{collectionId}/query", payload);
        if (!res.IsSuccessStatusCode) return [];

        var json = await res.Content.ReadFromJsonAsync<JsonElement>();
        var ids = json.GetProperty("ids")[0].EnumerateArray()
            .Select(x => x.GetString() ?? "")
            .Where(x => !string.IsNullOrEmpty(x))
            .ToList();

        return ids;
    }

    public async Task DeleteAsync(string collection, string documentId)
    {
        var collectionId = await GetCollectionIdAsync(collection);
        if (collectionId is null) return;

        var payload = new { ids = new[] { documentId } };
        await _http.PostAsJsonAsync($"{ApiBase}/collections/{collectionId}/delete", payload);
    }

    private async Task<string?> GetCollectionIdAsync(string collection)
    {
        var res = await _http.GetAsync($"{ApiBase}/collections/{collection}");
        if (!res.IsSuccessStatusCode) return null;

        var json = await res.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("id").GetString();
    }
}
