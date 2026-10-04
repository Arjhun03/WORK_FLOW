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
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<UserDto>>>> GetUsers(
        [FromQuery] string? search = null, 
        [FromQuery] string? department = null)
    {
        var users = await _userService.GetAllUsersAsync(search, department);
        return Ok(ApiResponse<List<UserDto>>.SuccessResult(users));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetUserById(string id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        return Ok(ApiResponse<UserDto>.SuccessResult(user));
    }

    [Authorize(Roles = SystemRoles.Admin)]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserDto>>> CreateUser([FromBody] CreateUserDto dto)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var created = await _userService.CreateUserAsync(dto, adminId);
        return CreatedAtAction(nameof(GetUserById), new { id = created.Id }, ApiResponse<UserDto>.SuccessResult(created, "User created successfully"));
    }

    [Authorize(Roles = SystemRoles.Admin)]
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUser(string id, [FromBody] UpdateUserDto dto)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var updated = await _userService.UpdateUserAsync(id, dto, adminId);
        return Ok(ApiResponse<UserDto>.SuccessResult(updated, "User updated successfully"));
    }

    [Authorize(Roles = SystemRoles.Admin)]
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse>> DeleteUser(string id)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        await _userService.DeleteUserAsync(id, adminId);
        return Ok(ApiResponse.SuccessResult("User deactivated successfully"));
    }

    [HttpGet("roles")]
    public async Task<ActionResult<ApiResponse<List<Role>>>> GetRoles()
    {
        var roles = await _userService.GetRolesAsync();
        return Ok(ApiResponse<List<Role>>.SuccessResult(roles));
    }
}
