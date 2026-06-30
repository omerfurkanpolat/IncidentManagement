using System.ComponentModel;
using IncidentManagement.Shared.Services;
using ModelContextProtocol.Server;

namespace IncidentManagement.McpServer.Tools;

[McpServerToolType]
public class KnowledgeSearchTools
{
    private readonly ChromaDbClient _chroma;
    private readonly OllamaEmbeddingClient _embedder;

    public KnowledgeSearchTools(ChromaDbClient chroma, OllamaEmbeddingClient embedder)
    {
        _chroma = chroma;
        _embedder = embedder;
    }

    [McpServerTool, Description("Control-M hata kataloğunda arama yapar. Hata kodu veya hata mesajı ile bilinen hatalar, çözümler ve sorumlu ekipler bulunur.")]
    public async Task<string> SearchControlMErrors(
        [Description("Aranacak hata kodu veya hata mesajı")] string query,
        [Description("Döndürülecek maksimum sonuç sayısı (varsayılan: 3)")] int maxResults = 3)
    {
        return await SearchCollection("controlm_errors", query, maxResults);
    }

    [McpServerTool, Description("Geçmiş incident çözüm dokümantasyonunda arama yapar. Benzer geçmiş hatalar ve nasıl çözüldükleri hakkında bilgi verir.")]
    public async Task<string> SearchIncidentHistory(
        [Description("Aranacak hata veya sorun açıklaması")] string query,
        [Description("Döndürülecek maksimum sonuç sayısı (varsayılan: 3)")] int maxResults = 3)
    {
        return await SearchCollection("incident_history", query, maxResults);
    }

    [McpServerTool, Description("Job kod dokümantasyonunda arama yapar. Job'ın tetiklediği endpoint'in nasıl çalıştığını, bağımlılıklarını ve olası hata noktalarını açıklar.")]
    public async Task<string> SearchCodeDocumentation(
        [Description("Aranacak job adı veya endpoint")] string query,
        [Description("Döndürülecek maksimum sonuç sayısı (varsayılan: 3)")] int maxResults = 3)
    {
        return await SearchCollection("code_documentation", query, maxResults);
    }

    private async Task<string> SearchCollection(string collectionName, string query, int maxResults)
    {
        try
        {
            var collectionId = await _chroma.GetCollectionIdAsync(collectionName);
            var queryEmbedding = await _embedder.GetEmbeddingAsync(query);
            var results = await _chroma.QueryAsync(collectionId, queryEmbedding, maxResults);

            if (!results.Any())
                return $"'{collectionName}' koleksiyonunda '{query}' için sonuç bulunamadı.";

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"**{collectionName} — {results.Count} sonuç:**\n");

            for (int i = 0; i < results.Count; i++)
            {
                var r = results[i];
                sb.AppendLine($"--- Sonuç {i + 1} ---");
                sb.AppendLine($"Başlık: {r.Metadata.GetValueOrDefault("title", "?")}");
                sb.AppendLine($"Yazar/Takım: {r.Metadata.GetValueOrDefault("author", "?")} / {r.Metadata.GetValueOrDefault("team", "?")}");
                sb.AppendLine($"İçerik:\n{r.Text}");
                sb.AppendLine($"Alaka düzeyi: {(1 - r.Distance):P0}\n");
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            return $"Arama sırasında hata: {ex.Message}";
        }
    }
}
