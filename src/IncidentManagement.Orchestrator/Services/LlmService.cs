using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using IncidentManagement.Shared.Models;

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
        var tools = await GetMcpToolDefinitionsAsync();
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
                _logger.LogInformation("[Orchestrator] Tool çağrısı: {Tool}", toolCall.Function?.Name);
                var toolResult = await CallMcpToolAsync(toolCall);
                sourcesUsed.Add(toolCall.Function?.Name ?? "unknown");
                messages.Add(new OllamaMessage { Role = "tool", Content = toolResult });
            }
        }

        return new IncidentAnalysis("Analiz tamamlanamadı.", sourcesUsed);
    }

    private async Task<List<OllamaTool>> GetMcpToolDefinitionsAsync()
    {
        try
        {
            using var mcpHttp = new HttpClient();
            var payload = new { method = "tools/list", @params = new { } };
            var response = await mcpHttp.PostAsJsonAsync($"{_mcpServerUrl}/mcp", payload);
            var result = await response.Content.ReadFromJsonAsync<McpToolsListResponse>();
            return result?.Result?.Tools?.Select(t => new OllamaTool
            {
                Type = "function",
                Function = new OllamaToolFunction
                {
                    Name = t.Name,
                    Description = t.Description,
                    Parameters = t.InputSchema
                }
            }).ToList() ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Orchestrator] MCP tool tanımları alınamadı");
            return new();
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

    private async Task<string> CallMcpToolAsync(OllamaToolCall toolCall)
    {
        try
        {
            using var mcpHttp = new HttpClient();
            var payload = new
            {
                method = "tools/call",
                @params = new
                {
                    name = toolCall.Function?.Name,
                    arguments = toolCall.Function?.Arguments
                }
            };

            var response = await mcpHttp.PostAsJsonAsync($"{_mcpServerUrl}/mcp", payload);
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("result", out var result) &&
                result.TryGetProperty("content", out var content) &&
                content.ValueKind == JsonValueKind.Array &&
                content.GetArrayLength() > 0)
            {
                return content[0].TryGetProperty("text", out var text) ? text.GetString() ?? "" : "";
            }

            return "Tool sonucu alınamadı.";
        }
        catch (Exception ex)
        {
            return $"Tool çağrısı başarısız: {ex.Message}";
        }
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
        **Job ID:** {job.JobId}
        **Hata Kodu:** {job.ErrorCode}
        **Hata Mesajı:** {job.ErrorMessage}
        **Tetiklenen Endpoint:** {job.EndpointUrl}
        **Başlangıç:** {job.StartTime:yyyy-MM-dd HH:mm:ss}
        **Bitiş:** {job.EndTime:yyyy-MM-dd HH:mm:ss}

        Lütfen araçları kullanarak bu hatayı analiz et ve çözüm önerisi sun.
        """;
}

public record IncidentAnalysis(string Analysis, List<string> SourcesUsed);

public class OllamaMessage
{
    [JsonPropertyName("role")]    public string Role { get; set; } = string.Empty;
    [JsonPropertyName("content")] public string? Content { get; set; }
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

public class McpToolsListResponse
{
    [JsonPropertyName("result")] public McpToolsResult? Result { get; set; }
}

public class McpToolsResult
{
    [JsonPropertyName("tools")] public List<McpToolDef>? Tools { get; set; }
}

public class McpToolDef
{
    [JsonPropertyName("name")]        public string Name { get; set; } = string.Empty;
    [JsonPropertyName("description")] public string Description { get; set; } = string.Empty;
    [JsonPropertyName("inputSchema")] public object? InputSchema { get; set; }
}
