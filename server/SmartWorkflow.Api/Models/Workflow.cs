using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SmartWorkflow.Api.Models;

public static class ApproverTypes
{
    public const string User = "User";
    public const string Role = "Role";
    public const string Manager = "Manager";
    public const string DepartmentRole = "DepartmentRole";
}

public class WorkflowStep
{
    [BsonRepresentation(BsonType.ObjectId)]
    public string StepId { get; set; } = ObjectId.GenerateNewId().ToString();

    public string Name { get; set; } = string.Empty;

    public int Order { get; set; } = 1;

    public string ApproverType { get; set; } = ApproverTypes.Role;

    public string? ApproverRole { get; set; }

    private string? _approverUserId;

    [BsonRepresentation(BsonType.ObjectId)]
    [BsonIgnoreIfNull]
    public string? ApproverUserId
    {
        get => string.IsNullOrWhiteSpace(_approverUserId) ? null : _approverUserId;
        set => _approverUserId = string.IsNullOrWhiteSpace(value) ? null : value;
    }

    public string? ApproverDepartment { get; set; }

    public bool IsRequired { get; set; } = true;
}

public class Workflow : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string RequestType { get; set; } = string.Empty;

    public int Version { get; set; } = 1;

    public bool IsActive { get; set; } = true;

    [BsonRepresentation(BsonType.ObjectId)]
    public string CreatedBy { get; set; } = string.Empty;

    public List<WorkflowStep> Steps { get; set; } = new();
}
