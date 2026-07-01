using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using IncidentManagement.Shared.Models;
using ModelContextProtocol.Client;

namespace IncidentManagement.Orchestrator.Services;

public class LlmService
{
    private readonly HttpClient _http;
    private readonly string _model;
    private readonly string _mcpServerUrl;
    private readonly ILogger<LlmService> _logger;

    public LlmService(HttpClient http, IConfiguration config, ILogger<LlmService> logger)
    {
        _http = http;
        _http.BaseAddress = new Uri(config["Ollama:BaseUrl"]!);
        _http.Timeout = TimeSpan.FromMinutes(10);
        _model = config["Ollama:LlmModel"]!;
        _mcpServerUrl = config["McpServer:Url"]!;
        _logger = logger;
    }

    public async Task<IncidentAnalysis> AnalyzeIncidentAsync(JobResult jobResult)
    {
        var (tools, mcpClient) = await GetMcpToolsAsync();

        await using (mcpClient)
        {
            var messages = new List<OllamaMessage>
            {
                new() { Role = "system", Content = BuildSystemPrompt() },
                new() { Role = "user",   Content = BuildUserPrompt(jobResult) }
            };

            var sourcesUsed = new List<string>();

            for (int iteration = 0; iteration < 10; iteration++)
            {
                var response = await CallOllamaAsync(messages, tools);

                if (response.Message?.ToolCalls == null || !response.Message.ToolCalls.Any())
                    return new IncidentAnalysis(response.Message?.Content ?? string.Empty, sourcesUsed);

                messages.Add(new OllamaMessage
                {
                    Role = "assistant",
                    Content = response.Message.Content ?? string.Empty,
                    ToolCalls = response.Message.ToolCalls
                });

                foreach (var toolCall in response.Message.ToolCalls)
                {
                    var toolName = toolCall.Function?.Name ?? "";
                    _logger.LogInformation("[Orchestrator] Tool çağrısı: {Tool}", toolName);

                    var toolResult = mcpClient != null
                        ? await CallMcpToolAsync(mcpClient, toolCall)
                        : "MCP bağlantısı yok.";

                    sourcesUsed.Add(toolName);
                    messages.Add(new OllamaMessage { Role = "tool", Content = toolResult });
                }
            }

            return new IncidentAnalysis("Analiz tamamlanamadı.", sourcesUsed);
        }
    }

    private async Task<(List<OllamaTool> tools, McpClient? client)> GetMcpToolsAsync()
    {
        try
        {
            var transport = new HttpClientTransport(new HttpClientTransportOptions
            {
                Endpoint = new Uri($"{_mcpServerUrl}/")
            });

            var client = await McpClient.CreateAsync(transport);
            var mcpTools = await client.ListToolsAsync();

            var tools = mcpTools.Select(t => new OllamaTool
            {
                Type = "function",
                Function = new OllamaToolFunction
                {
                    Name = t.Name,
                    Description = t.Description ?? "",
                    Parameters = t.JsonSchema
                }
            }).ToList();

            _logger.LogInformation("[Orchestrator] MCP'den {Count} tool alındı", tools.Count);
            return (tools, client);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Orchestrator] MCP tool tanımları alınamadı, RAG devre dışı");
            return (new List<OllamaTool>(), null);
        }
    }

    private async Task<string> CallMcpToolAsync(McpClient client, OllamaToolCall toolCall)
    {
        try
        {
            var args = toolCall.Function?.Arguments?
                .ToDictionary(k => k.Key, v => (object?)v.Value?.ToString());

            var result = await client.CallToolAsync(
                toolCall.Function?.Name ?? "",
                args ?? new Dictionary<string, object?>()
            );

            var text = result.Content
                .OfType<ModelContextProtocol.Protocol.TextContentBlock>()
                .Select(c => c.Text)
                .FirstOrDefault();

            return text ?? "Sonuç boş.";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Orchestrator] Tool çağrısı başarısız: {Tool}", toolCall.Function?.Name);
            return $"Tool çağrısı başarısız: {ex.Message}";
        }
    }

    private async Task<OllamaResponse> CallOllamaAsync(List<OllamaMessage> messages, List<OllamaTool> tools)
    {
        var payload = new
        {
            model = _model,
            messages,
            tools,
            stream = false
        };

        var response = await _http.PostAsJsonAsync("/api/chat", payload);
        return await response.Content.ReadFromJsonAsync<OllamaResponse>()
            ?? new OllamaResponse();
    }

    private string BuildSystemPrompt() => """
        Sen bir incident yönetim asistanısın. Control-M job hatalarını analiz ederek çözüm önerisi sunuyorsun.
        Elindeki araçları kullanarak:
        1. Önce hata kodunu Control-M hata kataloğunda ara
        2. Benzer geçmiş incidentları incele
        3. İlgili job'ın kod dokümantasyonuna bak
        4. Tüm bu bilgileri sentezleyerek net bir çözüm önerisi sun

        Cevabını şu formatta ver:
        ## Hata Analizi
        ## Olası Nedenler
        ## Önerilen Çözümler
        ## Sorumlu Ekip / Eskalasyon
        """;

    private string BuildUserPrompt(JobResult job) => $"""
        Aşağıdaki Control-M job hatası analiz edilmesi gerekiyor:

        **Job Adı:** {job.JobName}
        **Hata Kodu:** {job.ErrorCode}
        **Hata Mesajı:** {job.ErrorMessage}
        **Endpoint:** {job.EndpointUrl}

        Lütfen araçları kullanarak bu hatayı analiz et ve çözüm önerisi sun.
        """;
}

public record IncidentAnalysis(string Analysis, List<string> SourcesUsed);

public class OllamaMessage
{
    [JsonPropertyName("role")]       public string Role { get; set; } = string.Empty;
    [JsonPropertyName("content")]    public string? Content { get; set; }
    [JsonPropertyName("tool_calls")] public List<OllamaToolCall>? ToolCalls { get; set; }
}

public class OllamaResponse
{
    [JsonPropertyName("message")] public OllamaMessage? Message { get; set; }
}

public class OllamaTool
{
    [JsonPropertyName("type")]     public string Type { get; set; } = "function";
    [JsonPropertyName("function")] public OllamaToolFunction? Function { get; set; }
}

public class OllamaToolFunction
{
    [JsonPropertyName("name")]        public string Name { get; set; } = string.Empty;
    [JsonPropertyName("description")] public string Description { get; set; } = string.Empty;
    [JsonPropertyName("parameters")]  public object? Parameters { get; set; }
}

public class OllamaToolCall
{
    [JsonPropertyName("function")] public OllamaToolCallFunction? Function { get; set; }
}

public class OllamaToolCallFunction
{
    [JsonPropertyName("name")]      public string Name { get; set; } = string.Empty;
    [JsonPropertyName("arguments")] public Dictionary<string, object>? Arguments { get; set; }
}
