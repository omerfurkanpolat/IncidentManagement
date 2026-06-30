using IncidentManagement.RagPipeline;
using IncidentManagement.Shared.Services;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton<DocumentRepository>();
builder.Services.AddSingleton<DocumentChunker>();
builder.Services.AddHttpClient<ChromaDbClient>();
builder.Services.AddHttpClient<OllamaEmbeddingClient>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
