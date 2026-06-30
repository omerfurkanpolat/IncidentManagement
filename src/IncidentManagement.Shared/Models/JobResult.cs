using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace IncidentManagement.Shared.Models;

public class JobResult
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string JobName { get; set; } = string.Empty;
    public string JobId { get; set; } = string.Empty;
    public JobStatus Status { get; set; }

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorStackTrace { get; set; }
    public string? EndpointUrl { get; set; }

    public Dictionary<string, string> Metadata { get; set; } = new();

    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public bool IsProcessed { get; set; } = false;
}

public enum JobStatus
{
    Success,
    Failed,
    Warning
}

public class JobResultRequest
{
    public string JobName { get; set; } = string.Empty;
    public string JobId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorStackTrace { get; set; }
    public string? EndpointUrl { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}
