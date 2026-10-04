using System.ComponentModel.DataAnnotations;

namespace SmartWorkflow.Api.DTOs;

#region Auth DTOs
public class LoginRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public UserDto User { get; set; } = null!;
}

public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public string Department { get; set; } = string.Empty;
    public string? ManagerId { get; set; }
    public string? ManagerName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateUserDto
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    public List<string> Roles { get; set; } = new() { "Employee" };

    public string Department { get; set; } = "General";

    public string? ManagerId { get; set; }
}

public class UpdateUserDto
{
    public string? Name { get; set; }
    public List<string>? Roles { get; set; }
    public string? Department { get; set; }
    public string? ManagerId { get; set; }
    public bool? IsActive { get; set; }
    public string? Password { get; set; }
}
#endregion

#region Workflow DTOs
public class WorkflowStepDto
{
    public string? StepId { get; set; }
    
    [Required]
    public string Name { get; set; } = string.Empty;

    public int Order { get; set; } = 1;

    [Required]
    public string ApproverType { get; set; } = "Role"; // User, Role, Manager, DepartmentRole

    public string? ApproverRole { get; set; }

    public string? ApproverUserId { get; set; }

    public string? ApproverDepartment { get; set; }

    public bool IsRequired { get; set; } = true;
}

public class CreateWorkflowDto
{
    [Required]
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    [Required]
    public string RequestType { get; set; } = string.Empty;

    public List<WorkflowStepDto> Steps { get; set; } = new();
}

public class UpdateWorkflowDto
{
    [Required]
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string RequestType { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public List<WorkflowStepDto> Steps { get; set; } = new();
}

public class WorkflowDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string RequestType { get; set; } = string.Empty;
    public int Version { get; set; }
    public bool IsActive { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public List<WorkflowStepDto> Steps { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
#endregion

#region Request DTOs
public class CreateRequestDto
{
    [Required]
    public string WorkflowId { get; set; } = string.Empty;

    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    public Dictionary<string, object>? Data { get; set; }
}

public class UpdateRequestDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public Dictionary<string, object>? Data { get; set; }
}

public class RequestDto
{
    public string Id { get; set; } = string.Empty;
    public string WorkflowId { get; set; } = string.Empty;
    public string WorkflowName { get; set; } = string.Empty;
    public int WorkflowVersion { get; set; }
    public string RequestedBy { get; set; } = string.Empty;
    public string RequesterName { get; set; } = string.Empty;
    public string RequesterDepartment { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string RequestType { get; set; } = string.Empty;
    public Dictionary<string, object> Data { get; set; } = new();
    public string Status { get; set; } = string.Empty;
    public int CurrentStepOrder { get; set; }
    public string? CurrentApproverId { get; set; }
    public string? CurrentApproverName { get; set; }
    public string? CurrentApproverRole { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class RequestDetailDto : RequestDto
{
    public List<ApprovalDto> Approvals { get; set; } = new();
    public List<CommentDto> Comments { get; set; } = new();
    public List<AuditLogDto> AuditLogs { get; set; } = new();
    public List<WorkflowStepDto> WorkflowSteps { get; set; } = new();
}
#endregion

#region Approval DTOs
public class ApprovalDto
{
    public string Id { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public string RequestTitle { get; set; } = string.Empty;
    public string RequesterName { get; set; } = string.Empty;
    public string WorkflowStepId { get; set; } = string.Empty;
    public string StepName { get; set; } = string.Empty;
    public int StepOrder { get; set; }
    public string? ApproverId { get; set; }
    public string? ApproverName { get; set; }
    public string? ApproverRole { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Comments { get; set; }
    public DateTime? ActionDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ApprovalActionDto
{
    public string? Comments { get; set; }
}
#endregion

#region Comment DTOs
public class CreateCommentDto
{
    [Required]
    public string Message { get; set; } = string.Empty;
}

public class CommentDto
{
    public string Id { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
#endregion

#region Notification DTOs
public class NotificationDto
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}
#endregion

#region Audit Log DTOs
public class AuditLogDto
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? RequestId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public DateTime Timestamp { get; set; }
}
#endregion

#region Dashboard DTOs
public class DashboardStatsDto
{
    // Admin / system counters
    public long TotalUsers { get; set; }
    public long ActiveWorkflows { get; set; }
    public long TotalRequests { get; set; }
    public long PendingRequests { get; set; }
    public long ApprovedRequests { get; set; }
    public long RejectedRequests { get; set; }
    public long CompletedRequests { get; set; }

    // User-specific counters
    public long MyRequestsCount { get; set; }
    public long PendingApprovalsCount { get; set; }

    // Charts
    public List<ChartItemDto> RequestsByStatus { get; set; } = new();
    public List<ChartItemDto> RequestsByWorkflow { get; set; } = new();
    public List<TimelineTrendDto> RequestsOverTime { get; set; } = new();
}

public class ChartItemDto
{
    public string Name { get; set; } = string.Empty;
    public long Value { get; set; }
    public string? Color { get; set; }
}

public class TimelineTrendDto
{
    public string Date { get; set; } = string.Empty;
    public long Count { get; set; }
}
#endregion
