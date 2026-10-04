using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartWorkflow.Api.DTOs;
using SmartWorkflow.Api.Services;

namespace SmartWorkflow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ApprovalsController : ControllerBase
{
    private readonly IApprovalService _approvalService;

    public ApprovalsController(IApprovalService approvalService)
    {
        _approvalService = approvalService;
    }

    [HttpGet("pending")]
    public async Task<ActionResult<ApiResponse<List<ApprovalDto>>>> GetPending()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var pending = await _approvalService.GetPendingApprovalsForUserAsync(userId);
        return Ok(ApiResponse<List<ApprovalDto>>.SuccessResult(pending));
    }
}

[ApiController]
[Authorize]
[Route("api/requests/{requestId}/[controller]")]
public class CommentsController : ControllerBase
{
    private readonly ICommentService _commentService;

    public CommentsController(ICommentService commentService)
    {
        _commentService = commentService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<CommentDto>>>> GetComments(string requestId)
    {
        var comments = await _commentService.GetCommentsAsync(requestId);
        return Ok(ApiResponse<List<CommentDto>>.SuccessResult(comments));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CommentDto>>> AddComment(string requestId, [FromBody] CreateCommentDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var created = await _commentService.AddCommentAsync(requestId, userId, dto.Message);
        return Ok(ApiResponse<CommentDto>.SuccessResult(created, "Comment added successfully"));
    }
}

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<NotificationDto>>>> GetNotifications([FromQuery] bool unreadOnly = false)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var list = await _notificationService.GetUserNotificationsAsync(userId, unreadOnly);
        return Ok(ApiResponse<List<NotificationDto>>.SuccessResult(list));
    }

    [HttpPut("{id}/read")]
    public async Task<ActionResult<ApiResponse>> MarkAsRead(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        await _notificationService.MarkAsReadAsync(id, userId);
        return Ok(ApiResponse.SuccessResult("Notification marked as read"));
    }

    [HttpPut("read-all")]
    public async Task<ActionResult<ApiResponse>> MarkAllAsRead()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        await _notificationService.MarkAllAsReadAsync(userId);
        return Ok(ApiResponse.SuccessResult("All notifications marked as read"));
    }
}

[ApiController]
[Authorize]
[Route("api/audit-logs")]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogsController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> GetAuditLogs(
        [FromQuery] string? requestId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var (items, totalCount) = await _auditLogService.GetAuditLogsAsync(requestId, page, pageSize);
        var result = new
        {
            items,
            totalCount,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        };
        return Ok(ApiResponse<object>.SuccessResult(result));
    }
}

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<DashboardStatsDto>>> GetDashboard()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var stats = await _dashboardService.GetDashboardStatsAsync(userId);
        return Ok(ApiResponse<DashboardStatsDto>.SuccessResult(stats));
    }
}
