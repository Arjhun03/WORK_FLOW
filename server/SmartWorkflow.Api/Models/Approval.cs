using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SmartWorkflow.Api.Models;

public static class ApprovalStatus
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string Skipped = "Skipped";

    public static readonly string[] All = { Pending, Approved, Rejected, Skipped };
}

public class Approval : BaseEntity
{
    [BsonRepresentation(BsonType.ObjectId)]
    public string RequestId { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.ObjectId)]
    public string WorkflowStepId { get; set; } = string.Empty;

    public string StepName { get; set; } = string.Empty;

    public int StepOrder { get; set; } = 1;

    private string? _approverId;

    [BsonRepresentation(BsonType.ObjectId)]
    [BsonIgnoreIfNull]
    public string? ApproverId
    {
        get => string.IsNullOrWhiteSpace(_approverId) ? null : _approverId;
        set => _approverId = string.IsNullOrWhiteSpace(value) ? null : value;
    }

    public string? ApproverName { get; set; }

    public string? ApproverRole { get; set; }

    public string Status { get; set; } = ApprovalStatus.Pending;

    public string? Comments { get; set; }

    public DateTime? ActionDate { get; set; }
}
