using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartWorkflow.Api.Database;
using SmartWorkflow.Api.DTOs;
using SmartWorkflow.Api.Services;

namespace SmartWorkflow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly DatabaseSeeder _seeder;

    public AuthController(IAuthService authService, DatabaseSeeder seeder)
    {
        _authService = authService;
        _seeder = seeder;
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Login([FromBody] LoginRequestDto dto)
    {
        var result = await _authService.LoginAsync(dto);
        return Ok(ApiResponse<LoginResponseDto>.SuccessResult(result, "Login successful"));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetMe()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse.FailureResult("User not authenticated"));
        }

        var user = await _authService.GetCurrentUserAsync(userId);
        return Ok(ApiResponse<UserDto>.SuccessResult(user));
    }

    [HttpPost("seed")]
    public async Task<ActionResult<ApiResponse>> SeedDatabase()
    {
        await _seeder.SeedAsync();
        return Ok(ApiResponse.SuccessResult("Database seeded successfully with default users and workflows."));
    }
}
