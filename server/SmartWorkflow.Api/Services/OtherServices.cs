using SmartWorkflow.Api.DTOs;
using SmartWorkflow.Api.Helpers;
using SmartWorkflow.Api.Models;
using SmartWorkflow.Api.Repositories;

namespace SmartWorkflow.Api.Services;

#region Notification Service
public interface INotificationService
{
    Task<List<NotificationDto>> GetUserNotificationsAsync(string userId, bool unreadOnly = false);
    Task<bool> MarkAsReadAsync(string notificationId, string userId);
    Task<bool> MarkAllAsReadAsync(string userId);
}

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;

    public NotificationService(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task<List<NotificationDto>> GetUserNotificationsAsync(string userId, bool unreadOnly = false)
    {
        var notifications = await _notificationRepository.GetByUserIdAsync(userId, unreadOnly);
        return notifications.Select(n => new NotificationDto
        {
            Id = n.Id,
            UserId = n.UserId,
            RequestId = n.RequestId,
            Type = n.Type,
            Message = n.Message,
            IsRead = n.IsRead,
            CreatedAt = n.CreatedAt
        }).ToList();
    }

    public async Task<bool> MarkAsReadAsync(string notificationId, string userId)
    {
        var notification = await _notificationRepository.GetByIdAsync(notificationId);
        if (notification == null || notification.UserId != userId) return false;

        notification.IsRead = true;
        return await _notificationRepository.UpdateAsync(notificationId, notification);
    }

    public async Task<bool> MarkAllAsReadAsync(string userId)
    {
        await _notificationRepository.MarkAllAsReadAsync(userId);
        return true;
    }
}
#endregion

#region Comment Service
public interface ICommentService
{
    Task<List<CommentDto>> GetCommentsAsync(string requestId);
    Task<CommentDto> AddCommentAsync(string requestId, string userId, string message);
}

public class CommentService : ICommentService
{
    private readonly ICommentRepository _commentRepository;
    private readonly IRequestRepository _requestRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IAuditLogRepository _auditLogRepository;

    public CommentService(
        ICommentRepository commentRepository,
        IRequestRepository requestRepository,
        IUserRepository userRepository,
        INotificationRepository notificationRepository,
        IAuditLogRepository auditLogRepository)
    {
        _commentRepository = commentRepository;
        _requestRepository = requestRepository;
        _userRepository = userRepository;
        _notificationRepository = notificationRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<List<CommentDto>> GetCommentsAsync(string requestId)
    {
        var comments = await _commentRepository.GetByRequestIdAsync(requestId);
        return comments.Select(c => new CommentDto
        {
            Id = c.Id,
            RequestId = c.RequestId,
            UserId = c.UserId,
            UserName = c.UserName,
            Message = c.Message,
            CreatedAt = c.CreatedAt
        }).ToList();
    }

    public async Task<CommentDto> AddCommentAsync(string requestId, string userId, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new BadRequestException("Comment message cannot be empty.");
        }

        var request = await _requestRepository.GetByIdAsync(requestId);
        if (request == null) throw new NotFoundException($"Request with ID '{requestId}' was not found.");

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) throw new UnauthorizedException("User not authenticated.");

        var comment = new Comment
        {
            RequestId = requestId,
            UserId = userId,
            UserName = user.Name,
            Message = message.Trim()
        };

        var created = await _commentRepository.CreateAsync(comment);

        // Notify request owner if the commenter is an approver, or notify approver if commenter is owner
        if (request.RequestedBy != userId)
        {
            await _notificationRepository.CreateAsync(new Notification
            {
                UserId = request.RequestedBy,
                RequestId = requestId,
                Type = NotificationType.CommentAdded,
                Message = $"{user.Name} commented on your request '{request.Title}': \"{message.Trim()}\""
            });
        }
        else if (!string.IsNullOrEmpty(request.CurrentApproverId) && request.CurrentApproverId != userId)
        {
            await _notificationRepository.CreateAsync(new Notification
            {
                UserId = request.CurrentApproverId,
                RequestId = requestId,
                Type = NotificationType.CommentAdded,
                Message = $"{user.Name} commented on request '{request.Title}': \"{message.Trim()}\""
            });
        }

        // Audit Log
        await _auditLogRepository.CreateAsync(new AuditLog
        {
            UserId = userId,
            UserName = user.Name,
            RequestId = requestId,
            Action = AuditActions.CommentAdded,
            NewValue = message.Length > 50 ? message.Substring(0, 50) + "..." : message
        });

        return new CommentDto
        {
            Id = created.Id,
            RequestId = created.RequestId,
            UserId = created.UserId,
            UserName = created.UserName,
            Message = created.Message,
            CreatedAt = created.CreatedAt
        };
    }
}
#endregion

#region Audit Log Service
public interface IAuditLogService
{
    Task<(List<AuditLogDto> Items, long TotalCount)> GetAuditLogsAsync(string? requestId = null, int page = 1, int pageSize = 20);
}

public class AuditLogService : IAuditLogService
{
    private readonly IAuditLogRepository _auditLogRepository;

    public AuditLogService(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;
    }

    public async Task<(List<AuditLogDto> Items, long TotalCount)> GetAuditLogsAsync(string? requestId = null, int page = 1, int pageSize = 20)
    {
        var all = await _auditLogRepository.GetAllAsync();
        if (!string.IsNullOrWhiteSpace(requestId))
        {
            all = all.Where(a => a.RequestId == requestId).ToList();
        }

        var total = all.Count;
        var paged = all
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogDto
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
            })
            .ToList();

        return (paged, total);
    }
}
#endregion

#region Dashboard Service
public interface IDashboardService
{
    Task<DashboardStatsDto> GetDashboardStatsAsync(string userId);
}

public class DashboardService : IDashboardService
{
    private readonly IUserRepository _userRepository;
    private readonly IWorkflowRepository _workflowRepository;
    private readonly IRequestRepository _requestRepository;
    private readonly IApprovalRepository _approvalRepository;

    public DashboardService(
        IUserRepository userRepository,
        IWorkflowRepository workflowRepository,
        IRequestRepository requestRepository,
        IApprovalRepository approvalRepository)
    {
        _userRepository = userRepository;
        _workflowRepository = workflowRepository;
        _requestRepository = requestRepository;
        _approvalRepository = approvalRepository;
    }

    public async Task<DashboardStatsDto> GetDashboardStatsAsync(string userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        var requests = await _requestRepository.GetAllAsync();
        var workflows = await _workflowRepository.GetAllAsync();
        var totalUsers = await _userRepository.CountAsync();
        var pendingApprovals = await _approvalRepository.FindAsync(a => a.Status == ApprovalStatus.Pending);

        bool isAdmin = user?.Roles.Contains(SystemRoles.Admin) == true;
        var myPendingApprovalsCount = pendingApprovals.Count(a =>
            isAdmin ||
            a.ApproverId == userId ||
            (!string.IsNullOrEmpty(a.ApproverRole) && user != null && user.Roles.Contains(a.ApproverRole)));

        // Requests by Status
        var statusGroups = requests.GroupBy(r => r.Status)
            .Select(g => new ChartItemDto
            {
                Name = g.Key,
                Value = g.Count(),
                Color = GetStatusColor(g.Key)
            }).ToList();

        // Requests by Workflow
        var workflowMap = workflows.ToDictionary(w => w.Id, w => w.Name);
        var workflowGroups = requests.GroupBy(r => r.WorkflowId)
            .Select(g => new ChartItemDto
            {
                Name = workflowMap.TryGetValue(g.Key, out var wName) ? wName : "Other",
                Value = g.Count()
            }).ToList();

        // Requests Over Time (last 7 days)
        var sevenDaysAgo = DateTime.UtcNow.Date.AddDays(-6);
        var timeline = new List<TimelineTrendDto>();
        for (int i = 0; i < 7; i++)
        {
            var date = sevenDaysAgo.AddDays(i);
            var dateStr = date.ToString("MMM dd");
            var count = requests.Count(r => r.CreatedAt.Date == date);
            timeline.Add(new TimelineTrendDto { Date = dateStr, Count = count });
        }

        return new DashboardStatsDto
        {
            TotalUsers = totalUsers,
            ActiveWorkflows = workflows.Count(w => w.IsActive),
            TotalRequests = requests.Count,
            PendingRequests = requests.Count(r => r.Status == RequestStatus.Pending),
            ApprovedRequests = requests.Count(r => r.Status is RequestStatus.Approved or RequestStatus.InProgress),
            RejectedRequests = requests.Count(r => r.Status == RequestStatus.Rejected),
            CompletedRequests = requests.Count(r => r.Status == RequestStatus.Completed),
            MyRequestsCount = requests.Count(r => r.RequestedBy == userId),
            PendingApprovalsCount = myPendingApprovalsCount,
            RequestsByStatus = statusGroups,
            RequestsByWorkflow = workflowGroups,
            RequestsOverTime = timeline
        };
    }

    private static string GetStatusColor(string status)
    {
        return status switch
        {
            RequestStatus.Completed => "#10b981", // Emerald
            RequestStatus.InProgress => "#3b82f6", // Blue
            RequestStatus.Pending => "#f59e0b", // Amber
            RequestStatus.Rejected => "#ef4444", // Rose
            RequestStatus.Cancelled => "#6b7280", // Gray
            _ => "#8b5cf6"
        };
    }
}
#endregion
