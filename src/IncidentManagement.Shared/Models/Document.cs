using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace IncidentManagement.Shared.Models;

public class Document
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DocumentCategory Category { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Pending;

    public string AuthorName { get; set; } = string.Empty;
    public string AuthorTeam { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovedBy { get; set; }

    public bool IsIndexed { get; set; } = false;
    public DateTime? IndexedAt { get; set; }
}

public enum DocumentCategory
{
    ControlMErrors,
    IncidentHistory,
    CodeDocumentation
}

public enum DocumentStatus
{
    Pending,
    Approved,
    Rejected
}
