using SmartWorkflow.Api.DTOs;
using SmartWorkflow.Api.Helpers;
using SmartWorkflow.Api.Models;
using SmartWorkflow.Api.Repositories;

namespace SmartWorkflow.Api.Services;

public interface IRequestService
{
    Task<RequestDto> CreateRequestAsync(CreateRequestDto dto, string userId);
    Task<RequestDto> UpdateRequestAsync(string id, UpdateRequestDto dto, string userId);
    Task<(List<RequestDto> Items, long TotalCount)> GetRequestsAsync(
        string? status = null,
        string? workflowId = null,
        string? requestedBy = null,
        string? search = null,
        int page = 1,
        int pageSize = 10);
    Task<RequestDetailDto> GetRequestDetailAsync(string id, string userId);
    Task<RequestDto> CancelRequestAsync(string id, string userId);
}

public class RequestService : IRequestService
{
    private readonly IWorkflowEngine _workflowEngine;
    private readonly IRequestRepository _requestRepository;
    private readonly IApprovalRepository _approvalRepository;
    private readonly ICommentRepository _commentRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IUserRepository _userRepository;

    public RequestService(
        IWorkflowEngine workflowEngine,
        IRequestRepository requestRepository,
        IApprovalRepository approvalRepository,
        ICommentRepository commentRepository,
        IAuditLogRepository auditLogRepository,
        IUserRepository userRepository)
    {
        _workflowEngine = workflowEngine;
        _requestRepository = requestRepository;
        _approvalRepository = approvalRepository;
        _commentRepository = commentRepository;
        _auditLogRepository = auditLogRepository;
        _userRepository = userRepository;
    }

    public async Task<RequestDto> CreateRequestAsync(CreateRequestDto dto, string userId)
    {
        var normalizedData = DataNormalizer.Normalize(dto.Data);
        var request = await _workflowEngine.StartWorkflowAsync(
            dto.WorkflowId,
            userId,
            dto.Title,
            dto.Description,
            normalizedData);

        return MapToRequestDto(request);
    }

    public async Task<(List<RequestDto> Items, long TotalCount)> GetRequestsAsync(
        string? status = null,
        string? workflowId = null,
        string? requestedBy = null,
        string? search = null,
        int page = 1,
        int pageSize = 10)
    {
        var all = await _requestRepository.GetAllAsync();

        if (!string.IsNullOrWhiteSpace(status))
        {
            all = all.Where(r => r.Status.Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (!string.IsNullOrWhiteSpace(workflowId))
        {
            all = all.Where(r => r.WorkflowId == workflowId).ToList();
        }

        if (!string.IsNullOrWhiteSpace(requestedBy))
        {
            all = all.Where(r => r.RequestedBy == requestedBy).ToList();
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            all = all.Where(r => r.Title.ToLower().Contains(s) || 
                                 r.Description.ToLower().Contains(s) ||
                                 r.RequesterName.ToLower().Contains(s)).ToList();
        }

        var total = all.Count;
        var paged = all
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // Resolve names for current approver
        var userIds = paged.Where(p => !string.IsNullOrEmpty(p.CurrentApproverId))
                           .Select(p => p.CurrentApproverId!)
                           .Distinct()
                           .ToList();
        var userMap = new Dictionary<string, string>();
        if (userIds.Any())
        {
            var users = await _userRepository.FindAsync(u => userIds.Contains(u.Id));
            userMap = users.ToDictionary(u => u.Id, u => u.Name);
        }

        var dtos = paged.Select(r =>
        {
            var dto = MapToRequestDto(r);
            if (!string.IsNullOrEmpty(r.CurrentApproverId) && userMap.TryGetValue(r.CurrentApproverId, out var name))
            {
                dto.CurrentApproverName = name;
            }
            return dto;
        }).ToList();

        return (dtos, total);
    }

    public async Task<RequestDetailDto> GetRequestDetailAsync(string id, string userId)
    {
        var request = await _requestRepository.GetByIdAsync(id);
        if (request == null) throw new NotFoundException($"Request with ID '{id}' was not found.");

        var approvals = await _approvalRepository.GetByRequestIdAsync(id);
        var comments = await _commentRepository.GetByRequestIdAsync(id);
        var auditLogs = await _auditLogRepository.GetByRequestIdAsync(id);

        string? approverName = null;
        if (!string.IsNullOrEmpty(request.CurrentApproverId))
        {
            var user = await _userRepository.GetByIdAsync(request.CurrentApproverId);
            approverName = user?.Name;
        }

        var baseDto = MapToRequestDto(request);
        baseDto.CurrentApproverName = approverName;

        var detail = new RequestDetailDto
        {
            Id = baseDto.Id,
            WorkflowId = baseDto.WorkflowId,
            WorkflowName = baseDto.WorkflowName,
            WorkflowVersion = baseDto.WorkflowVersion,
            RequestedBy = baseDto.RequestedBy,
            RequesterName = baseDto.RequesterName,
            RequesterDepartment = baseDto.RequesterDepartment,
            Title = baseDto.Title,
            Description = baseDto.Description,
            RequestType = baseDto.RequestType,
            Data = baseDto.Data,
            Status = baseDto.Status,
            CurrentStepOrder = baseDto.CurrentStepOrder,
            CurrentApproverId = baseDto.CurrentApproverId,
            CurrentApproverName = baseDto.CurrentApproverName,
            CurrentApproverRole = baseDto.CurrentApproverRole,
            CreatedAt = baseDto.CreatedAt,
            UpdatedAt = baseDto.UpdatedAt,
            CompletedAt = baseDto.CompletedAt,
            Approvals = approvals.Select(a => new ApprovalDto
            {
                Id = a.Id,
                RequestId = a.RequestId,
                RequestTitle = request.Title,
                RequesterName = request.RequesterName,
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
            }).ToList(),
            Comments = comments.Select(c => new CommentDto
            {
                Id = c.Id,
                RequestId = c.RequestId,
                UserId = c.UserId,
                UserName = c.UserName,
                Message = c.Message,
                CreatedAt = c.CreatedAt
            }).ToList(),
            AuditLogs = auditLogs.Select(a => new AuditLogDto
            {
                Id = a.Id,
                UserId = a.UserId,
                UserName = a.UserName,
                RequestId = a.RequestId,
                Action = a.Action,
                OldValue = a.OldValue,
                NewValue = a.NewValue,
                Metadata = a.Metadata,
                Timestamp = a.Timestamp
            }).ToList(),
            WorkflowSteps = request.WorkflowSnapshot?.Steps.OrderBy(s => s.Order).Select(s => new WorkflowStepDto
            {
                StepId = s.StepId,
                Name = s.Name,
                Order = s.Order,
                ApproverType = s.ApproverType,
                ApproverRole = s.ApproverRole,
                ApproverUserId = s.ApproverUserId,
                ApproverDepartment = s.ApproverDepartment,
                IsRequired = s.IsRequired
            }).ToList() ?? new List<WorkflowStepDto>()
        };

        return detail;
    }

    public async Task<RequestDto> CancelRequestAsync(string id, string userId)
    {
        var request = await _requestRepository.GetByIdAsync(id);
        if (request == null) throw new NotFoundException($"Request with ID '{id}' was not found.");

        var currentUser = await _userRepository.GetByIdAsync(userId);
        bool isAdmin = currentUser?.Roles.Contains(SystemRoles.Admin) == true;

        if (request.RequestedBy != userId && !isAdmin)
        {
            throw new ForbiddenException("You can only cancel your own requests.");
        }

        if (request.Status is RequestStatus.Completed or RequestStatus.Approved or RequestStatus.Rejected or RequestStatus.Cancelled)
        {
            throw new BadRequestException($"Cannot cancel request in '{request.Status}' status.");
        }

        var oldState = request.Status;
        request.Status = RequestStatus.Cancelled;
        request.CompletedAt = DateTime.UtcNow;
        request.UpdatedAt = DateTime.UtcNow;
        await _requestRepository.UpdateAsync(request.Id, request);

        // Cancel any pending approval
        var pendingApproval = await _approvalRepository.GetCurrentPendingApprovalAsync(id, request.CurrentStepOrder);
        if (pendingApproval != null)
        {
            pendingApproval.Status = ApprovalStatus.Skipped;
            pendingApproval.Comments = "Request was cancelled by user.";
            await _approvalRepository.UpdateAsync(pendingApproval.Id, pendingApproval);
        }

        // Audit Log
        await _auditLogRepository.CreateAsync(new AuditLog
        {
            UserId = userId,
            UserName = currentUser?.Name ?? "User",
            RequestId = request.Id,
            Action = AuditActions.RequestCancelled,
            OldValue = oldState,
            NewValue = RequestStatus.Cancelled
        });

        return MapToRequestDto(request);
    }

    public async Task<RequestDto> UpdateRequestAsync(string id, UpdateRequestDto dto, string userId)
    {
        var request = await _requestRepository.GetByIdAsync(id);
        if (request == null) throw new NotFoundException($"Request with ID '{id}' was not found.");

        var currentUser = await _userRepository.GetByIdAsync(userId);
        bool isAdmin = currentUser?.Roles.Contains(SystemRoles.Admin) == true;

        if (request.RequestedBy != userId && !isAdmin)
        {
            throw new ForbiddenException("You can only edit your own requests.");
        }

        if (request.Status is RequestStatus.Completed or RequestStatus.Approved or RequestStatus.Rejected or RequestStatus.Cancelled)
        {
            throw new BadRequestException($"Cannot update request in '{request.Status}' status.");
        }

        if (!string.IsNullOrWhiteSpace(dto.Title))
        {
            request.Title = dto.Title.Trim();
        }

        if (dto.Description != null)
        {
            request.Description = dto.Description.Trim();
        }

        if (dto.Data != null)
        {
            var normalized = DataNormalizer.Normalize(dto.Data);
            foreach (var kvp in normalized)
            {
                request.Data[kvp.Key] = kvp.Value;
            }
        }

        request.UpdatedAt = DateTime.UtcNow;
        await _requestRepository.UpdateAsync(request.Id, request);

        // Audit Log
        await _auditLogRepository.CreateAsync(new AuditLog
        {
            UserId = userId,
            UserName = currentUser?.Name ?? "User",
            RequestId = request.Id,
            Action = "RequestUpdated",
            NewValue = request.Title,
            Metadata = new Dictionary<string, object>
            {
                { "updatedFields", new[] { dto.Title != null ? "Title" : null, dto.Description != null ? "Description" : null, dto.Data != null ? "Data" : null }.Where(x => x != null).ToList() }
            }
        });

        return MapToRequestDto(request);
    }

    public static RequestDto MapToRequestDto(Request r)
    {
        return new RequestDto
        {
            Id = r.Id,
            WorkflowId = r.WorkflowId,
            WorkflowName = r.WorkflowSnapshot?.Name ?? r.RequestType,
            WorkflowVersion = r.WorkflowVersion,
            RequestedBy = r.RequestedBy,
            RequesterName = r.RequesterName,
            RequesterDepartment = r.RequesterDepartment,
            Title = r.Title,
            Description = r.Description,
            RequestType = r.RequestType,
            Data = r.Data,
            Status = r.Status,
            CurrentStepOrder = r.CurrentStepOrder,
            CurrentApproverId = r.CurrentApproverId,
            CurrentApproverRole = r.CurrentApproverRole,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt,
            CompletedAt = r.CompletedAt
        };
    }
}
