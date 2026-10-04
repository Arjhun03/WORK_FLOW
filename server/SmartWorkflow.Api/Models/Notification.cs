using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SmartWorkflow.Api.Models;

public static class NotificationType
{
    public const string ApprovalRequired = "ApprovalRequired";
    public const string RequestApproved = "RequestApproved";
    public const string RequestRejected = "RequestRejected";
    public const string RequestCompleted = "RequestCompleted";
    public const string CommentAdded = "CommentAdded";
}

public class Notification : BaseEntity
{
    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.ObjectId)]
    public string RequestId { get; set; } = string.Empty;

    public string Type { get; set; } = NotificationType.ApprovalRequired;

    public string Message { get; set; } = string.Empty;

    public bool IsRead { get; set; } = false;
}
