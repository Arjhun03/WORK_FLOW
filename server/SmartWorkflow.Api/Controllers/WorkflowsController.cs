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
public class WorkflowsController : ControllerBase
{
    private readonly IWorkflowService _workflowService;

    public WorkflowsController(IWorkflowService workflowService)
    {
        _workflowService = workflowService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<WorkflowDto>>>> GetAll([FromQuery] bool? activeOnly = null)
    {
        var workflows = await _workflowService.GetAllWorkflowsAsync(activeOnly);
        return Ok(ApiResponse<List<WorkflowDto>>.SuccessResult(workflows));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<WorkflowDto>>> GetById(string id)
    {
        var workflow = await _workflowService.GetWorkflowByIdAsync(id);
        return Ok(ApiResponse<WorkflowDto>.SuccessResult(workflow));
    }

    [Authorize(Roles = SystemRoles.Admin)]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<WorkflowDto>>> Create([FromBody] CreateWorkflowDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var created = await _workflowService.CreateWorkflowAsync(dto, userId);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, ApiResponse<WorkflowDto>.SuccessResult(created, "Workflow created successfully"));
    }

    [Authorize(Roles = SystemRoles.Admin)]
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<WorkflowDto>>> Update(string id, [FromBody] UpdateWorkflowDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var updated = await _workflowService.UpdateWorkflowAsync(id, dto, userId);
        return Ok(ApiResponse<WorkflowDto>.SuccessResult(updated, "Workflow updated successfully"));
    }

    [Authorize(Roles = SystemRoles.Admin)]
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse>> Delete(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        await _workflowService.DeleteWorkflowAsync(id, userId);
        return Ok(ApiResponse.SuccessResult("Workflow deactivated successfully"));
    }

    [Authorize(Roles = SystemRoles.Admin)]
    [HttpPost("{id}/activate")]
    public async Task<ActionResult<ApiResponse<WorkflowDto>>> Activate(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var workflow = await _workflowService.SetActiveStatusAsync(id, true, userId);
        return Ok(ApiResponse<WorkflowDto>.SuccessResult(workflow, "Workflow activated"));
    }

    [Authorize(Roles = SystemRoles.Admin)]
    [HttpPost("{id}/deactivate")]
    public async Task<ActionResult<ApiResponse<WorkflowDto>>> Deactivate(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var workflow = await _workflowService.SetActiveStatusAsync(id, false, userId);
        return Ok(ApiResponse<WorkflowDto>.SuccessResult(workflow, "Workflow deactivated"));
    }
}
