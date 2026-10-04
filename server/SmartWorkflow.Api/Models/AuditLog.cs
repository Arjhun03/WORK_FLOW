using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SmartWorkflow.Api.Models;

public static class AuditActions
{
    public const string UserCreated = "USER_CREATED";
    public const string WorkflowCreated = "WORKFLOW_CREATED";
    public const string WorkflowUpdated = "WORKFLOW_UPDATED";
    public const string RequestCreated = "REQUEST_CREATED";
    public const string RequestApproved = "REQUEST_APPROVED";
    public const string RequestRejected = "REQUEST_REJECTED";
    public const string RequestCompleted = "REQUEST_COMPLETED";
    public const string RequestCancelled = "REQUEST_CANCELLED";
    public const string CommentAdded = "COMMENT_ADDED";
}

public class AuditLog : BaseEntity
{
    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.ObjectId)]
    public string? RequestId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public Dictionary<string, object> Metadata { get; set; } = new();

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
