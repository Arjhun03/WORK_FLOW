using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartWorkflow.Api.DTOs;
using SmartWorkflow.Api.Models;
using SmartWorkflow.Api.Services;

namespace SmartWorkflow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class RequestsController : ControllerBase
{
    private readonly IRequestService _requestService;
    private readonly IApprovalService _approvalService;

    public RequestsController(IRequestService requestService, IApprovalService approvalService)
    {
        _requestService = requestService;
        _approvalService = approvalService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> GetAll(
        [FromQuery] string? status = null,
        [FromQuery] string? workflowId = null,
        [FromQuery] string? requestedBy = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] bool myRequestsOnly = false)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var effectiveRequestedBy = myRequestsOnly ? currentUserId : requestedBy;

        // If user is neither Admin nor Approver role, default to showing their own requests
        var roles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();
        bool canViewAll = roles.Any(r => r is SystemRoles.Admin or SystemRoles.Manager or SystemRoles.HR or SystemRoles.IT or SystemRoles.Finance);
        if (!canViewAll && string.IsNullOrEmpty(effectiveRequestedBy))
        {
            effectiveRequestedBy = currentUserId;
        }

        var (items, totalCount) = await _requestService.GetRequestsAsync(
            status, workflowId, effectiveRequestedBy, search, page, pageSize);

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

    [HttpPost]
    public async Task<ActionResult<ApiResponse<RequestDto>>> Create([FromBody] CreateRequestDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var created = await _requestService.CreateRequestAsync(dto, userId);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, ApiResponse<RequestDto>.SuccessResult(created, "Request submitted successfully"));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<RequestDetailDto>>> GetById(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var detail = await _requestService.GetRequestDetailAsync(id, userId);
        return Ok(ApiResponse<RequestDetailDto>.SuccessResult(detail));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<RequestDto>>> Update(string id, [FromBody] UpdateRequestDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var updated = await _requestService.UpdateRequestAsync(id, dto, userId);
        return Ok(ApiResponse<RequestDto>.SuccessResult(updated, "Request details updated successfully"));
    }

    [HttpPost("{id}/cancel")]
    public async Task<ActionResult<ApiResponse<RequestDto>>> Cancel(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var cancelled = await _requestService.CancelRequestAsync(id, userId);
        return Ok(ApiResponse<RequestDto>.SuccessResult(cancelled, "Request cancelled successfully"));
    }

    [HttpPost("{id}/approve")]
    public async Task<ActionResult<ApiResponse<RequestDto>>> Approve(string id, [FromBody] ApprovalActionDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _approvalService.ApproveRequestAsync(id, userId, dto.Comments);
        return Ok(ApiResponse<RequestDto>.SuccessResult(result, "Request approved successfully"));
    }

    [HttpPost("{id}/reject")]
    public async Task<ActionResult<ApiResponse<RequestDto>>> Reject(string id, [FromBody] ApprovalActionDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _approvalService.RejectRequestAsync(id, userId, dto.Comments);
        return Ok(ApiResponse<RequestDto>.SuccessResult(result, "Request rejected"));
    }
}
