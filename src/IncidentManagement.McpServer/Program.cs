using IncidentManagement.McpServer.Tools;
using IncidentManagement.Shared.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient<ChromaDbClient>();
builder.Services.AddHttpClient<OllamaEmbeddingClient>();

builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithTools<KnowledgeSearchTools>();

var app = builder.Build();
app.MapMcp();
app.Run();
