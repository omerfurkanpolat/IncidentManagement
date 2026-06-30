using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace IncidentManagement.Shared.Models;

public class IncidentReport
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string JobResultId { get; set; } = string.Empty;
    public string JobName { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;

    public string LlmAnalysis { get; set; } = string.Empty;
    public List<string> SourcesUsed { get; set; } = new();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public IncidentStatus Status { get; set; } = IncidentStatus.Open;
}

public enum IncidentStatus
{
    Open,
    InProgress,
    Resolved
}
