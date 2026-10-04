using SmartWorkflow.Api.DTOs;
using SmartWorkflow.Api.Helpers;
using SmartWorkflow.Api.Models;
using SmartWorkflow.Api.Repositories;

namespace SmartWorkflow.Api.Services;

public interface IUserService
{
    Task<List<UserDto>> GetAllUsersAsync(string? search = null, string? department = null);
    Task<UserDto> GetUserByIdAsync(string id);
    Task<UserDto> CreateUserAsync(CreateUserDto dto, string adminUserId);
    Task<UserDto> UpdateUserAsync(string id, UpdateUserDto dto, string adminUserId);
    Task<bool> DeleteUserAsync(string id, string adminUserId);
    Task<List<Role>> GetRolesAsync();
}

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IAuditLogRepository _auditLogRepository;

    public UserService(
        IUserRepository userRepository, 
        IRoleRepository roleRepository,
        IAuditLogRepository auditLogRepository)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<List<UserDto>> GetAllUsersAsync(string? search = null, string? department = null)
    {
        var users = await _userRepository.GetAllAsync();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            users = users.Where(u => u.Name.ToLower().Contains(s) || u.Email.ToLower().Contains(s)).ToList();
        }

        if (!string.IsNullOrWhiteSpace(department))
        {
            users = users.Where(u => u.Department.Equals(department, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        var userMap = users.ToDictionary(u => u.Id, u => u.Name);

        return users.Select(u =>
        {
            var dto = AuthService.MapToUserDto(u);
            if (!string.IsNullOrEmpty(u.ManagerId) && userMap.TryGetValue(u.ManagerId, out var mgrName))
            {
                dto.ManagerName = mgrName;
            }
            return dto;
        }).ToList();
    }

    public async Task<UserDto> GetUserByIdAsync(string id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) throw new NotFoundException($"User with ID '{id}' was not found.");

        var dto = AuthService.MapToUserDto(user);
        if (!string.IsNullOrEmpty(user.ManagerId))
        {
            var manager = await _userRepository.GetByIdAsync(user.ManagerId);
            dto.ManagerName = manager?.Name;
        }

        return dto;
    }

    public async Task<UserDto> CreateUserAsync(CreateUserDto dto, string adminUserId)
    {
        var existing = await _userRepository.GetByEmailAsync(dto.Email);
        if (existing != null)
        {
            throw new BadRequestException($"A user with email '{dto.Email}' already exists.");
        }

        var user = new User
        {
            Name = dto.Name,
            Email = dto.Email.ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Roles = dto.Roles.Any() ? dto.Roles : new List<string> { SystemRoles.Employee },
            Department = dto.Department,
            ManagerId = string.IsNullOrWhiteSpace(dto.ManagerId) ? null : dto.ManagerId,
            IsActive = true
        };

        var created = await _userRepository.CreateAsync(user);

        // Audit log
        var admin = await _userRepository.GetByIdAsync(adminUserId);
        await _auditLogRepository.CreateAsync(new AuditLog
        {
            UserId = adminUserId,
            UserName = admin?.Name ?? "Admin",
            Action = AuditActions.UserCreated,
            NewValue = created.Email,
            Metadata = new Dictionary<string, object>
            {
                { "createdUserId", created.Id },
                { "name", created.Name },
                { "roles", created.Roles }
            }
        });

        return AuthService.MapToUserDto(created);
    }

    public async Task<UserDto> UpdateUserAsync(string id, UpdateUserDto dto, string adminUserId)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) throw new NotFoundException($"User with ID '{id}' was not found.");

        if (!string.IsNullOrWhiteSpace(dto.Name)) user.Name = dto.Name;
        if (dto.Roles != null && dto.Roles.Any()) user.Roles = dto.Roles;
        if (!string.IsNullOrWhiteSpace(dto.Department)) user.Department = dto.Department;
        if (dto.ManagerId != null) user.ManagerId = string.IsNullOrWhiteSpace(dto.ManagerId) ? null : dto.ManagerId;
        if (dto.IsActive.HasValue) user.IsActive = dto.IsActive.Value;
        if (!string.IsNullOrWhiteSpace(dto.Password)) user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        await _userRepository.UpdateAsync(id, user);

        return AuthService.MapToUserDto(user);
    }

    public async Task<bool> DeleteUserAsync(string id, string adminUserId)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) throw new NotFoundException($"User with ID '{id}' was not found.");

        // Soft-deactivate user rather than hard-deleting to preserve historical audits & requests
        user.IsActive = false;
        return await _userRepository.UpdateAsync(id, user);
    }

    public async Task<List<Role>> GetRolesAsync()
    {
        return await _roleRepository.GetAllAsync();
    }
}
