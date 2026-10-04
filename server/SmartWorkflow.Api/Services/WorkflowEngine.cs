using SmartWorkflow.Api.Helpers;
using SmartWorkflow.Api.Models;
using SmartWorkflow.Api.Repositories;

namespace SmartWorkflow.Api.Services;

public interface IWorkflowEngine
{
    Task<Request> StartWorkflowAsync(string workflowId, string requesterId, string title, string description, Dictionary<string, object>? data);
    Task<WorkflowStep?> GetCurrentStepAsync(Request request);
    Task<(string? ApproverId, string? ApproverName, string? ApproverRole)> ResolveApproverAsync(WorkflowStep step, User requester);
    Task<Request> ProcessApprovalAsync(string requestId, string currentUserId, string? comments);
    Task<Request> ProcessRejectionAsync(string requestId, string currentUserId, string? comments);
    Task<Request> MoveToNextStepAsync(Request request, WorkflowStep currentStep, string approverId, string? comments);
    Task<Request> CompleteWorkflowAsync(Request request, string approverId, string? comments);
}

public class WorkflowEngine : IWorkflowEngine
{
    private readonly IWorkflowRepository _workflowRepository;
    private readonly IRequestRepository _requestRepository;
    private readonly IApprovalRepository _approvalRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ILogger<WorkflowEngine> _logger;

    public WorkflowEngine(
        IWorkflowRepository workflowRepository,
        IRequestRepository requestRepository,
        IApprovalRepository approvalRepository,
        IUserRepository userRepository,
        INotificationRepository notificationRepository,
        IAuditLogRepository auditLogRepository,
        ILogger<WorkflowEngine> logger)
    {
        _workflowRepository = workflowRepository;
        _requestRepository = requestRepository;
        _approvalRepository = approvalRepository;
        _userRepository = userRepository;
        _notificationRepository = notificationRepository;
        _auditLogRepository = auditLogRepository;
        _logger = logger;
    }

    public async Task<Request> StartWorkflowAsync(
        string workflowId, 
        string requesterId, 
        string title, 
        string description, 
        Dictionary<string, object>? data)
    {
        var workflow = await _workflowRepository.GetByIdAsync(workflowId);
        if (workflow == null)
        {
            throw new NotFoundException($"Workflow with ID '{workflowId}' was not found.");
        }

        if (!workflow.IsActive)
        {
            throw new BadRequestException("Cannot create request: the selected workflow is deactivated.");
        }

        if (workflow.Steps == null || !workflow.Steps.Any())
        {
            throw new BadRequestException("Cannot create request: the workflow has no configured approval steps.");
        }

        var requester = await _userRepository.GetByIdAsync(requesterId);
        if (requester == null)
        {
            throw new NotFoundException($"Requester with ID '{requesterId}' was not found.");
        }

        // 1. Snapshot workflow definition at start time
        var firstStep = workflow.Steps.OrderBy(s => s.Order).First();
        var (approverId, approverName, approverRole) = await ResolveApproverAsync(firstStep, requester);

        var request = new Request
        {
            WorkflowId = workflow.Id,
            WorkflowVersion = workflow.Version,
            WorkflowSnapshot = workflow,
            RequestedBy = requester.Id,
            RequesterName = requester.Name,
            RequesterDepartment = requester.Department,
            Title = title,
            Description = description,
            RequestType = workflow.RequestType,
            Data = DataNormalizer.Normalize(data),
            Status = RequestStatus.Pending,
            CurrentStepOrder = firstStep.Order,
            CurrentApproverId = approverId,
            CurrentApproverRole = approverRole
        };

        var createdRequest = await _requestRepository.CreateAsync(request);

        // 2. Create the first pending approval document
        var approval = new Approval
        {
            RequestId = createdRequest.Id,
            WorkflowStepId = firstStep.StepId,
            StepName = firstStep.Name,
            StepOrder = firstStep.Order,
            ApproverId = approverId,
            ApproverName = approverName,
            ApproverRole = approverRole,
            Status = ApprovalStatus.Pending
        };
        await _approvalRepository.CreateAsync(approval);

        // 3. Emit notification for the approver (if assigned directly or by role)
        if (!string.IsNullOrEmpty(approverId))
        {
            await _notificationRepository.CreateAsync(new Notification
            {
                UserId = approverId,
                RequestId = createdRequest.Id,
                Type = NotificationType.ApprovalRequired,
                Message = $"New request '{title}' submitted by {requester.Name} requires your approval."
            });
        }
        else if (!string.IsNullOrEmpty(approverRole))
        {
            // Notify all active users in that role
            var roleUsers = await _userRepository.FindAsync(u => u.Roles.Contains(approverRole) && u.IsActive);
            foreach (var rUser in roleUsers)
            {
                await _notificationRepository.CreateAsync(new Notification
                {
                    UserId = rUser.Id,
                    RequestId = createdRequest.Id,
                    Type = NotificationType.ApprovalRequired,
                    Message = $"New request '{title}' for role '{approverRole}' requires review."
                });
            }
        }

        // 4. Emit Audit Log
        await _auditLogRepository.CreateAsync(new AuditLog
        {
            UserId = requester.Id,
            UserName = requester.Name,
            RequestId = createdRequest.Id,
            Action = AuditActions.RequestCreated,
            NewValue = RequestStatus.Pending,
            Metadata = new Dictionary<string, object>
            {
                { "workflowName", workflow.Name },
                { "stepOrder", firstStep.Order },
                { "stepName", firstStep.Name }
            }
        });

        return createdRequest;
    }

    public Task<WorkflowStep?> GetCurrentStepAsync(Request request)
    {
        var steps = request.WorkflowSnapshot?.Steps ?? new List<WorkflowStep>();
        var currentStep = steps.FirstOrDefault(s => s.Order == request.CurrentStepOrder);
        return Task.FromResult(currentStep);
    }

    public async Task<(string? ApproverId, string? ApproverName, string? ApproverRole)> ResolveApproverAsync(
        WorkflowStep step, 
        User requester)
    {
        switch (step.ApproverType)
        {
            case ApproverTypes.User:
                if (!string.IsNullOrEmpty(step.ApproverUserId))
                {
                    var user = await _userRepository.GetByIdAsync(step.ApproverUserId);
                    return (user?.Id, user?.Name, null);
                }
                return (null, null, null);

            case ApproverTypes.Role:
                return (null, null, step.ApproverRole);

            case ApproverTypes.Manager:
                if (!string.IsNullOrEmpty(requester.ManagerId))
                {
                    var manager = await _userRepository.GetByIdAsync(requester.ManagerId);
                    if (manager != null)
                    {
                        return (manager.Id, manager.Name, SystemRoles.Manager);
                    }
                }
                // Fallback to manager role if specific manager is not set
                return (null, null, SystemRoles.Manager);

            case ApproverTypes.DepartmentRole:
                var targetRole = step.ApproverRole ?? SystemRoles.Manager;
                var deptUsers = await _userRepository.FindAsync(u => 
                    u.Department == requester.Department && 
                    u.Roles.Contains(targetRole) && 
                    u.IsActive);
                
                var firstDeptUser = deptUsers.FirstOrDefault();
                if (firstDeptUser != null)
                {
                    return (firstDeptUser.Id, firstDeptUser.Name, targetRole);
                }
                return (null, null, targetRole);

            default:
                return (null, null, null);
        }
    }

    public async Task<Request> ProcessApprovalAsync(string requestId, string currentUserId, string? comments)
    {
        var request = await _requestRepository.GetByIdAsync(requestId);
        if (request == null) throw new NotFoundException($"Request with ID '{requestId}' was not found.");

        if (request.Status is RequestStatus.Completed or RequestStatus.Approved or RequestStatus.Rejected or RequestStatus.Cancelled)
        {
            throw new BadRequestException($"Cannot approve request in '{request.Status}' status.");
        }

        var currentUser = await _userRepository.GetByIdAsync(currentUserId);
        if (currentUser == null) throw new UnauthorizedException("User not authenticated.");

        // Rule: Users cannot approve their own requests unless admin
        if (request.RequestedBy == currentUserId && !currentUser.Roles.Contains(SystemRoles.Admin))
        {
            throw new ForbiddenException("You cannot approve your own request.");
        }

        // Validate current pending approval step
        var pendingApproval = await _approvalRepository.GetCurrentPendingApprovalAsync(requestId, request.CurrentStepOrder);
        if (pendingApproval == null)
        {
            throw new BadRequestException("No pending approval found for the current step.");
        }

        // Validate user authorization for this step
        bool isAuthorized = currentUser.Roles.Contains(SystemRoles.Admin);

        if (!isAuthorized)
        {
            if (!string.IsNullOrEmpty(pendingApproval.ApproverId))
            {
                isAuthorized = pendingApproval.ApproverId == currentUserId;
            }
            else if (!string.IsNullOrEmpty(pendingApproval.ApproverRole))
            {
                isAuthorized = currentUser.Roles.Contains(pendingApproval.ApproverRole);
            }
        }

        if (!isAuthorized)
        {
            throw new ForbiddenException("You are not authorized to approve this request step.");
        }

        // Update current approval record
        pendingApproval.Status = ApprovalStatus.Approved;
        pendingApproval.ApproverId = currentUser.Id;
        pendingApproval.ApproverName = currentUser.Name;
        pendingApproval.Comments = comments;
        pendingApproval.ActionDate = DateTime.UtcNow;
        await _approvalRepository.UpdateAsync(pendingApproval.Id, pendingApproval);

        // Determine next step
        var steps = request.WorkflowSnapshot?.Steps.OrderBy(s => s.Order).ToList() ?? new List<WorkflowStep>();
        var currentStep = steps.FirstOrDefault(s => s.Order == request.CurrentStepOrder);
        var nextStep = steps.FirstOrDefault(s => s.Order > request.CurrentStepOrder);

        if (nextStep != null)
        {
            return await MoveToNextStepAsync(request, nextStep, currentUser.Id, comments);
        }
        else
        {
            return await CompleteWorkflowAsync(request, currentUser.Id, comments);
        }
    }

    public async Task<Request> MoveToNextStepAsync(
        Request request, 
        WorkflowStep nextStep, 
        string approverId, 
        string? comments)
    {
        var requester = await _userRepository.GetByIdAsync(request.RequestedBy);
        var (nextApproverId, nextApproverName, nextApproverRole) = 
            await ResolveApproverAsync(nextStep, requester ?? new User());

        var oldState = request.Status;
        request.Status = RequestStatus.InProgress;
        request.CurrentStepOrder = nextStep.Order;
        request.CurrentApproverId = nextApproverId;
        request.CurrentApproverRole = nextApproverRole;
        request.UpdatedAt = DateTime.UtcNow;
        await _requestRepository.UpdateAsync(request.Id, request);

        // Create next pending approval
        var nextApproval = new Approval
        {
            RequestId = request.Id,
            WorkflowStepId = nextStep.StepId,
            StepName = nextStep.Name,
            StepOrder = nextStep.Order,
            ApproverId = nextApproverId,
            ApproverName = nextApproverName,
            ApproverRole = nextApproverRole,
            Status = ApprovalStatus.Pending
        };
        await _approvalRepository.CreateAsync(nextApproval);

        // Notify next approver
        if (!string.IsNullOrEmpty(nextApproverId))
        {
            await _notificationRepository.CreateAsync(new Notification
            {
                UserId = nextApproverId,
                RequestId = request.Id,
                Type = NotificationType.ApprovalRequired,
                Message = $"Request '{request.Title}' advanced to step '{nextStep.Name}' and requires your approval."
            });
        }

        // Notify requester of progress
        await _notificationRepository.CreateAsync(new Notification
        {
            UserId = request.RequestedBy,
            RequestId = request.Id,
            Type = NotificationType.RequestApproved,
            Message = $"Step was approved. Request '{request.Title}' is now at step '{nextStep.Name}'."
        });

        // Audit Log
        var approver = await _userRepository.GetByIdAsync(approverId);
        await _auditLogRepository.CreateAsync(new AuditLog
        {
            UserId = approverId,
            UserName = approver?.Name ?? "Approver",
            RequestId = request.Id,
            Action = AuditActions.RequestApproved,
            OldValue = oldState,
            NewValue = RequestStatus.InProgress,
            Metadata = new Dictionary<string, object>
            {
                { "advancedToStep", nextStep.Order },
                { "stepName", nextStep.Name },
                { "comments", comments ?? "" }
            }
        });

        return request;
    }

    public async Task<Request> CompleteWorkflowAsync(Request request, string approverId, string? comments)
    {
        var oldState = request.Status;
        request.Status = RequestStatus.Completed;
        request.CompletedAt = DateTime.UtcNow;
        request.UpdatedAt = DateTime.UtcNow;
        request.CurrentApproverId = null;
        request.CurrentApproverRole = null;
        await _requestRepository.UpdateAsync(request.Id, request);

        // Notify requester of completion
        await _notificationRepository.CreateAsync(new Notification
        {
            UserId = request.RequestedBy,
            RequestId = request.Id,
            Type = NotificationType.RequestCompleted,
            Message = $"Congratulations! Your request '{request.Title}' has been fully approved and completed."
        });

        // Audit Log
        var approver = await _userRepository.GetByIdAsync(approverId);
        await _auditLogRepository.CreateAsync(new AuditLog
        {
            UserId = approverId,
            UserName = approver?.Name ?? "Approver",
            RequestId = request.Id,
            Action = AuditActions.RequestCompleted,
            OldValue = oldState,
            NewValue = RequestStatus.Completed,
            Metadata = new Dictionary<string, object>
            {
                { "finalApprovalBy", approver?.Name ?? approverId },
                { "comments", comments ?? "" }
            }
        });

        return request;
    }

    public async Task<Request> ProcessRejectionAsync(string requestId, string currentUserId, string? comments)
    {
        var request = await _requestRepository.GetByIdAsync(requestId);
        if (request == null) throw new NotFoundException($"Request with ID '{requestId}' was not found.");

        if (request.Status is RequestStatus.Completed or RequestStatus.Rejected or RequestStatus.Cancelled)
        {
            throw new BadRequestException($"Cannot reject request in '{request.Status}' status.");
        }

        var currentUser = await _userRepository.GetByIdAsync(currentUserId);
        if (currentUser == null) throw new UnauthorizedException("User not authenticated.");

        var pendingApproval = await _approvalRepository.GetCurrentPendingApprovalAsync(requestId, request.CurrentStepOrder);
        if (pendingApproval == null)
        {
            throw new BadRequestException("No pending approval found for current step.");
        }

        // Validate rejection authorization
        bool isAuthorized = currentUser.Roles.Contains(SystemRoles.Admin);
        if (!isAuthorized)
        {
            if (!string.IsNullOrEmpty(pendingApproval.ApproverId))
            {
                isAuthorized = pendingApproval.ApproverId == currentUserId;
            }
            else if (!string.IsNullOrEmpty(pendingApproval.ApproverRole))
            {
                isAuthorized = currentUser.Roles.Contains(pendingApproval.ApproverRole);
            }
        }

        if (!isAuthorized)
        {
            throw new ForbiddenException("You are not authorized to reject this request step.");
        }

        // Mark approval as Rejected
        pendingApproval.Status = ApprovalStatus.Rejected;
        pendingApproval.ApproverId = currentUser.Id;
        pendingApproval.ApproverName = currentUser.Name;
        pendingApproval.Comments = comments;
        pendingApproval.ActionDate = DateTime.UtcNow;
        await _approvalRepository.UpdateAsync(pendingApproval.Id, pendingApproval);

        // Mark request as Rejected
        var oldState = request.Status;
        request.Status = RequestStatus.Rejected;
        request.UpdatedAt = DateTime.UtcNow;
        request.CompletedAt = DateTime.UtcNow;
        await _requestRepository.UpdateAsync(request.Id, request);

        // Notify requester
        await _notificationRepository.CreateAsync(new Notification
        {
            UserId = request.RequestedBy,
            RequestId = request.Id,
            Type = NotificationType.RequestRejected,
            Message = $"Your request '{request.Title}' was rejected by {currentUser.Name}. Reason: {comments ?? "No comment provided."}"
        });

        // Audit Log
        await _auditLogRepository.CreateAsync(new AuditLog
        {
            UserId = currentUser.Id,
            UserName = currentUser.Name,
            RequestId = request.Id,
            Action = AuditActions.RequestRejected,
            OldValue = oldState,
            NewValue = RequestStatus.Rejected,
            Metadata = new Dictionary<string, object>
            {
                { "rejectionStep", request.CurrentStepOrder },
                { "comments", comments ?? "" }
            }
        });

        return request;
    }
}
