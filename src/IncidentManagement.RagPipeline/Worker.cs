using IncidentManagement.Shared.Models;
using IncidentManagement.Shared.Services;

namespace IncidentManagement.RagPipeline;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly DocumentRepository _docRepo;
    private readonly ChromaDbClient _chroma;
    private readonly OllamaEmbeddingClient _embedder;
    private readonly DocumentChunker _chunker;

    private static readonly Dictionary<DocumentCategory, string> Collections = new()
    {
        { DocumentCategory.ControlMErrors,    "controlm_errors" },
        { DocumentCategory.IncidentHistory,   "incident_history" },
        { DocumentCategory.CodeDocumentation, "code_documentation" }
    };

    public Worker(ILogger<Worker> logger, DocumentRepository docRepo,
        ChromaDbClient chroma, OllamaEmbeddingClient embedder, DocumentChunker chunker)
    {
        _logger = logger;
        _docRepo = docRepo;
        _chroma = chroma;
        _embedder = embedder;
        _chunker = chunker;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        foreach (var col in Collections.Values)
            await _chroma.EnsureCollectionAsync(col);

        _logger.LogInformation("[RAG] ChromaDB koleksiyonları hazır");

        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessPendingDocumentsAsync();
            _logger.LogInformation("[RAG] Sonraki çalışma: 2 dakika sonra");
            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
        }
    }

    private async Task ProcessPendingDocumentsAsync()
    {
        var docs = await _docRepo.GetApprovedNotIndexedAsync();
        if (!docs.Any()) return;

        _logger.LogInformation("[RAG] {Count} doküman işlenecek", docs.Count);

        foreach (var doc in docs)
        {
            try
            {
                var collectionName = Collections[doc.Category];
                var collectionId = await _chroma.GetCollectionIdAsync(collectionName);
                var chunks = _chunker.Chunk(doc.Content);
                var chromaDocs = new List<ChromaDocument>();

                for (int i = 0; i < chunks.Count; i++)
                {
                    var embedding = await _embedder.GetEmbeddingAsync(chunks[i]);
                    chromaDocs.Add(new ChromaDocument(
                        Id: $"{doc.Id}_chunk_{i}",
                        Embedding: embedding,
                        Text: chunks[i],
                        Metadata: new Dictionary<string, string>
                        {
                            ["document_id"] = doc.Id!,
                            ["title"] = doc.Title,
                            ["category"] = doc.Category.ToString(),
                            ["author"] = doc.AuthorName,
                            ["team"] = doc.AuthorTeam
                        }
                    ));
                }

                await _chroma.AddEmbeddingsAsync(collectionId, chromaDocs);
                await _docRepo.MarkAsIndexedAsync(doc.Id!);

                _logger.LogInformation("[RAG] '{Title}' indekslendi ({ChunkCount} chunk)", doc.Title, chunks.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RAG] '{Title}' indekslenemedi", doc.Title);
            }
        }
    }
}
