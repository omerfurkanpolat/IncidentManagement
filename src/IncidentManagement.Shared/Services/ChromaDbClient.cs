using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace IncidentManagement.Shared.Services;

public class ChromaDbClient
{
    private readonly HttpClient _http;

    public ChromaDbClient(HttpClient http, IConfiguration config)
    {
        _http = http;
        _http.BaseAddress = new Uri(config.GetConnectionString("ChromaDB")!);
    }

    public async Task EnsureCollectionAsync(string collectionName)
    {
        var payload = new { name = collectionName };
        await _http.PostAsJsonAsync("/api/v1/collections", payload);
        // 409 = already exists, ignore
    }

    public async Task<string> GetCollectionIdAsync(string collectionName)
    {
        var response = await _http.GetFromJsonAsync<ChromaCollection>($"/api/v1/collections/{collectionName}");
        return response!.Id;
    }

    public async Task AddEmbeddingsAsync(string collectionId, List<ChromaDocument> documents)
    {
        var payload = new
        {
            ids = documents.Select(d => d.Id).ToList(),
            embeddings = documents.Select(d => d.Embedding).ToList(),
            documents = documents.Select(d => d.Text).ToList(),
            metadatas = documents.Select(d => d.Metadata).ToList()
        };
        await _http.PostAsJsonAsync($"/api/v1/collections/{collectionId}/add", payload);
    }

    public async Task<List<ChromaSearchResult>> QueryAsync(string collectionId, float[] queryEmbedding, int nResults = 3)
    {
        var payload = new
        {
            query_embeddings = new[] { queryEmbedding },
            n_results = nResults,
            include = new[] { "documents", "metadatas", "distances" }
        };
        var response = await _http.PostAsJsonAsync($"/api/v1/collections/{collectionId}/query", payload);
        var result = await response.Content.ReadFromJsonAsync<ChromaQueryResponse>();
        return result?.ToSearchResults() ?? new();
    }
}

public record ChromaDocument(string Id, float[] Embedding, string Text, Dictionary<string, string> Metadata);

public class ChromaCollection
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class ChromaQueryResponse
{
    [JsonPropertyName("documents")]
    public List<List<string>>? Documents { get; set; }
    [JsonPropertyName("metadatas")]
    public List<List<Dictionary<string, string>>>? Metadatas { get; set; }
    [JsonPropertyName("distances")]
    public List<List<float>>? Distances { get; set; }

    public List<ChromaSearchResult> ToSearchResults()
    {
        var results = new List<ChromaSearchResult>();
        if (Documents?.Count > 0)
        {
            for (int i = 0; i < Documents[0].Count; i++)
            {
                results.Add(new ChromaSearchResult(
                    Documents[0][i],
                    Metadatas?[0][i] ?? new(),
                    Distances?[0][i] ?? 0f
                ));
            }
        }
        return results;
    }
}

public record ChromaSearchResult(string Text, Dictionary<string, string> Metadata, float Distance);
