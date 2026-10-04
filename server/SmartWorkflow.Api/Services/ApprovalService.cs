using SmartWorkflow.Api.DTOs;
using SmartWorkflow.Api.Helpers;
using SmartWorkflow.Api.Models;
using SmartWorkflow.Api.Repositories;

namespace SmartWorkflow.Api.Services;

public interface IApprovalService
{
    Task<List<ApprovalDto>> GetPendingApprovalsForUserAsync(string userId);
    Task<RequestDto> ApproveRequestAsync(string requestId, string userId, string? comments);
    Task<RequestDto> RejectRequestAsync(string requestId, string userId, string? comments);
}

public class ApprovalService : IApprovalService
{
    private readonly IWorkflowEngine _workflowEngine;
    private readonly IApprovalRepository _approvalRepository;
    private readonly IRequestRepository _requestRepository;
    private readonly IUserRepository _userRepository;

    public ApprovalService(
        IWorkflowEngine workflowEngine,
        IApprovalRepository approvalRepository,
        IRequestRepository requestRepository,
        IUserRepository userRepository)
    {
        _workflowEngine = workflowEngine;
        _approvalRepository = approvalRepository;
        _requestRepository = requestRepository;
        _userRepository = userRepository;
    }

    public async Task<List<ApprovalDto>> GetPendingApprovalsForUserAsync(string userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) throw new NotFoundException("User not found.");

        var allPendingApprovals = await _approvalRepository.FindAsync(a => a.Status == ApprovalStatus.Pending);

        // Filter: user is explicitly assigned OR role matches any of user's roles OR user is Admin
        bool isAdmin = user.Roles.Contains(SystemRoles.Admin);
        var filtered = allPendingApprovals.Where(a =>
            isAdmin ||
            a.ApproverId == userId ||
            (!string.IsNullOrEmpty(a.ApproverRole) && user.Roles.Contains(a.ApproverRole))
        ).ToList();

        // Join with request titles and requester names
        var requestIds = filtered.Select(a => a.RequestId).Distinct().ToList();
        var requests = await _requestRepository.FindAsync(r => requestIds.Contains(r.Id));
        var requestMap = requests.ToDictionary(r => r.Id, r => r);

        return filtered.Select(a =>
        {
            var req = requestMap.TryGetValue(a.RequestId, out var r) ? r : null;
            return new ApprovalDto
            {
                Id = a.Id,
                RequestId = a.RequestId,
                RequestTitle = req?.Title ?? "Unknown Request",
                RequesterName = req?.RequesterName ?? "Unknown Requester",
                WorkflowStepId = a.WorkflowStepId,
                StepName = a.StepName,
                StepOrder = a.StepOrder,
                ApproverId = a.ApproverId,
                ApproverName = a.ApproverName,
                ApproverRole = a.ApproverRole,
                Status = a.Status,
                Comments = a.Comments,
                ActionDate = a.ActionDate,
                CreatedAt = a.CreatedAt
            };
        }).OrderByDescending(a => a.CreatedAt).ToList();
    }

    public async Task<RequestDto> ApproveRequestAsync(string requestId, string userId, string? comments)
    {
        var updatedRequest = await _workflowEngine.ProcessApprovalAsync(requestId, userId, comments);
        return RequestService.MapToRequestDto(updatedRequest);
    }

    public async Task<RequestDto> RejectRequestAsync(string requestId, string userId, string? comments)
    {
        var updatedRequest = await _workflowEngine.ProcessRejectionAsync(requestId, userId, comments);
        return RequestService.MapToRequestDto(updatedRequest);
    }
}
