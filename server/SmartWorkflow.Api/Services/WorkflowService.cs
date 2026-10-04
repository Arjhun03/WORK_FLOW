using MongoDB.Bson;
using SmartWorkflow.Api.DTOs;
using SmartWorkflow.Api.Helpers;
using SmartWorkflow.Api.Models;
using SmartWorkflow.Api.Repositories;

namespace SmartWorkflow.Api.Services;

public interface IWorkflowService
{
    Task<List<WorkflowDto>> GetAllWorkflowsAsync(bool? activeOnly = null);
    Task<WorkflowDto> GetWorkflowByIdAsync(string id);
    Task<WorkflowDto> CreateWorkflowAsync(CreateWorkflowDto dto, string userId);
    Task<WorkflowDto> UpdateWorkflowAsync(string id, UpdateWorkflowDto dto, string userId);
    Task<bool> DeleteWorkflowAsync(string id, string userId);
    Task<WorkflowDto> SetActiveStatusAsync(string id, bool isActive, string userId);
}

public class WorkflowService : IWorkflowService
{
    private readonly IWorkflowRepository _workflowRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogRepository _auditLogRepository;

    public WorkflowService(
        IWorkflowRepository workflowRepository,
        IUserRepository userRepository,
        IAuditLogRepository auditLogRepository)
    {
        _workflowRepository = workflowRepository;
        _userRepository = userRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<List<WorkflowDto>> GetAllWorkflowsAsync(bool? activeOnly = null)
    {
        var workflows = activeOnly.HasValue && activeOnly.Value
            ? await _workflowRepository.FindAsync(w => w.IsActive)
            : await _workflowRepository.GetAllAsync();

        return workflows.Select(MapToWorkflowDto).ToList();
    }

    public async Task<WorkflowDto> GetWorkflowByIdAsync(string id)
    {
        var workflow = await _workflowRepository.GetByIdAsync(id);
        if (workflow == null) throw new NotFoundException($"Workflow with ID '{id}' was not found.");
        return MapToWorkflowDto(workflow);
    }

    public async Task<WorkflowDto> CreateWorkflowAsync(CreateWorkflowDto dto, string userId)
    {
        if (dto.Steps == null || !dto.Steps.Any())
        {
            throw new BadRequestException("Every workflow must have at least one approval step.");
        }

        ValidateSteps(dto.Steps);

        var steps = dto.Steps.OrderBy(s => s.Order).Select((s, index) => new WorkflowStep
        {
            StepId = ObjectId.GenerateNewId().ToString(),
            Name = s.Name,
            Order = index + 1, // ensure continuous sequence starting at 1
            ApproverType = s.ApproverType,
            ApproverRole = s.ApproverRole,
            ApproverUserId = string.IsNullOrWhiteSpace(s.ApproverUserId) ? null : s.ApproverUserId,
            ApproverDepartment = s.ApproverDepartment,
            IsRequired = s.IsRequired
        }).ToList();

        var workflow = new Workflow
        {
            Name = dto.Name,
            Description = dto.Description,
            RequestType = dto.RequestType,
            Version = 1,
            IsActive = true,
            CreatedBy = userId,
            Steps = steps
        };

        var created = await _workflowRepository.CreateAsync(workflow);

        // Audit Log
        var user = await _userRepository.GetByIdAsync(userId);
        await _auditLogRepository.CreateAsync(new AuditLog
        {
            UserId = userId,
            UserName = user?.Name ?? "User",
            Action = AuditActions.WorkflowCreated,
            NewValue = created.Name,
            Metadata = new Dictionary<string, object>
            {
                { "workflowId", created.Id },
                { "version", created.Version },
                { "stepCount", created.Steps.Count }
            }
        });

        return MapToWorkflowDto(created);
    }

    public async Task<WorkflowDto> UpdateWorkflowAsync(string id, UpdateWorkflowDto dto, string userId)
    {
        var existing = await _workflowRepository.GetByIdAsync(id);
        if (existing == null) throw new NotFoundException($"Workflow with ID '{id}' was not found.");

        if (dto.Steps == null || !dto.Steps.Any())
        {
            throw new BadRequestException("Every workflow must have at least one approval step.");
        }

        ValidateSteps(dto.Steps);

        var steps = dto.Steps.OrderBy(s => s.Order).Select((s, index) => new WorkflowStep
        {
            StepId = string.IsNullOrWhiteSpace(s.StepId) || !ObjectId.TryParse(s.StepId, out _) ? ObjectId.GenerateNewId().ToString() : s.StepId,
            Name = s.Name,
            Order = index + 1,
            ApproverType = s.ApproverType,
            ApproverRole = s.ApproverRole,
            ApproverUserId = string.IsNullOrWhiteSpace(s.ApproverUserId) ? null : s.ApproverUserId,
            ApproverDepartment = s.ApproverDepartment,
            IsRequired = s.IsRequired
        }).ToList();

        existing.Name = dto.Name;
        existing.Description = dto.Description;
        existing.RequestType = dto.RequestType;
        existing.IsActive = dto.IsActive;
        existing.Steps = steps;
        existing.Version += 1; // Increment version on modification
        existing.UpdatedAt = DateTime.UtcNow;

        await _workflowRepository.UpdateAsync(id, existing);

        var user = await _userRepository.GetByIdAsync(userId);
        await _auditLogRepository.CreateAsync(new AuditLog
        {
            UserId = userId,
            UserName = user?.Name ?? "User",
            Action = AuditActions.WorkflowUpdated,
            OldValue = $"Version {existing.Version - 1}",
            NewValue = $"Version {existing.Version}",
            Metadata = new Dictionary<string, object>
            {
                { "workflowId", existing.Id },
                { "name", existing.Name }
            }
        });

        return MapToWorkflowDto(existing);
    }

    public async Task<bool> DeleteWorkflowAsync(string id, string userId)
    {
        var existing = await _workflowRepository.GetByIdAsync(id);
        if (existing == null) throw new NotFoundException($"Workflow with ID '{id}' was not found.");

        // Soft delete / deactivate
        existing.IsActive = false;
        existing.UpdatedAt = DateTime.UtcNow;
        return await _workflowRepository.UpdateAsync(id, existing);
    }

    public async Task<WorkflowDto> SetActiveStatusAsync(string id, bool isActive, string userId)
    {
        var existing = await _workflowRepository.GetByIdAsync(id);
        if (existing == null) throw new NotFoundException($"Workflow with ID '{id}' was not found.");

        existing.IsActive = isActive;
        existing.UpdatedAt = DateTime.UtcNow;
        await _workflowRepository.UpdateAsync(id, existing);

        return MapToWorkflowDto(existing);
    }

    private static void ValidateSteps(List<WorkflowStepDto> steps)
    {
        foreach (var step in steps)
        {
            if (string.IsNullOrWhiteSpace(step.Name))
            {
                throw new BadRequestException("Every workflow step must have a descriptive name.");
            }

            switch (step.ApproverType)
            {
                case ApproverTypes.Role:
                    if (string.IsNullOrWhiteSpace(step.ApproverRole))
                        throw new BadRequestException($"Step '{step.Name}' specifies ApproverType 'Role' but ApproverRole is missing.");
                    break;

                case ApproverTypes.User:
                    if (string.IsNullOrWhiteSpace(step.ApproverUserId))
                        throw new BadRequestException($"Step '{step.Name}' specifies ApproverType 'User' but ApproverUserId is missing.");
                    break;

                case ApproverTypes.Manager:
                    // Manager dynamically resolves to requester's managerId
                    break;

                case ApproverTypes.DepartmentRole:
                    if (string.IsNullOrWhiteSpace(step.ApproverRole))
                        throw new BadRequestException($"Step '{step.Name}' specifies ApproverType 'DepartmentRole' but ApproverRole is missing.");
                    break;

                default:
                    throw new BadRequestException($"Invalid ApproverType '{step.ApproverType}'. Valid types: User, Role, Manager, DepartmentRole.");
            }
        }
    }

    public static WorkflowDto MapToWorkflowDto(Workflow w)
    {
        return new WorkflowDto
        {
            Id = w.Id,
            Name = w.Name,
            Description = w.Description,
            RequestType = w.RequestType,
            Version = w.Version,
            IsActive = w.IsActive,
            CreatedBy = w.CreatedBy,
            CreatedAt = w.CreatedAt,
            UpdatedAt = w.UpdatedAt,
            Steps = w.Steps.OrderBy(s => s.Order).Select(s => new WorkflowStepDto
            {
                StepId = s.StepId,
                Name = s.Name,
                Order = s.Order,
                ApproverType = s.ApproverType,
                ApproverRole = s.ApproverRole,
                ApproverUserId = s.ApproverUserId,
                ApproverDepartment = s.ApproverDepartment,
                IsRequired = s.IsRequired
            }).ToList()
        };
    }
}
