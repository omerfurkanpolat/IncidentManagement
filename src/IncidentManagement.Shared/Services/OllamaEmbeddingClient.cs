using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace IncidentManagement.Shared.Services;

public class OllamaEmbeddingClient
{
    private readonly HttpClient _http;
    private readonly string _model;

    public OllamaEmbeddingClient(HttpClient http, IConfiguration config)
    {
        _http = http;
        _http.BaseAddress = new Uri(config["Ollama:BaseUrl"]!);
        _model = config["Ollama:EmbeddingModel"]!;
    }

    public async Task<float[]> GetEmbeddingAsync(string text)
    {
        var payload = new { model = _model, prompt = text };
        var response = await _http.PostAsJsonAsync("/api/embeddings", payload);
        var result = await response.Content.ReadFromJsonAsync<OllamaEmbeddingResponse>();
        return result!.Embedding;
    }
}

public class OllamaEmbeddingResponse
{
    [JsonPropertyName("embedding")]
    public float[] Embedding { get; set; } = Array.Empty<float>();
}
