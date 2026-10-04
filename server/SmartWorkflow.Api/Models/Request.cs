using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SmartWorkflow.Api.Models;

public static class RequestStatus
{
    public const string Draft = "Draft";
    public const string Pending = "Pending";
    public const string InProgress = "InProgress";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string Cancelled = "Cancelled";
    public const string Completed = "Completed";

    public static readonly string[] All = 
    { 
        Draft, Pending, InProgress, Approved, Rejected, Cancelled, Completed 
    };

    public static bool IsValidTransition(string current, string target)
    {
        return (current, target) switch
        {
            (Draft, Pending) => true,
            (Draft, Cancelled) => true,
            (Pending, InProgress) => true,
            (Pending, Cancelled) => true,
            (Pending, Rejected) => true,
            (Pending, Completed) => true,
            (InProgress, InProgress) => true,
            (InProgress, Approved) => true,
            (InProgress, Completed) => true,
            (InProgress, Rejected) => true,
            (InProgress, Cancelled) => true,
            (Approved, Completed) => true,
            _ => false
        };
    }
}

public class Request : BaseEntity
{
    [BsonRepresentation(BsonType.ObjectId)]
    public string WorkflowId { get; set; } = string.Empty;

    public int WorkflowVersion { get; set; } = 1;

    // Snapshot of the workflow definition at time of request creation
    public Workflow? WorkflowSnapshot { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string RequestedBy { get; set; } = string.Empty;

    public string RequesterName { get; set; } = string.Empty;

    public string RequesterDepartment { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string RequestType { get; set; } = string.Empty;

    public Dictionary<string, object> Data { get; set; } = new();

    public string Status { get; set; } = RequestStatus.Pending;

    public int CurrentStepOrder { get; set; } = 1;

    private string? _currentApproverId;

    [BsonRepresentation(BsonType.ObjectId)]
    [BsonIgnoreIfNull]
    public string? CurrentApproverId
    {
        get => string.IsNullOrWhiteSpace(_currentApproverId) ? null : _currentApproverId;
        set => _currentApproverId = string.IsNullOrWhiteSpace(value) ? null : value;
    }

    public string? CurrentApproverRole { get; set; }

    public DateTime? CompletedAt { get; set; }
}
