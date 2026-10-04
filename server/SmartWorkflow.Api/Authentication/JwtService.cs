using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SmartWorkflow.Api.Models;

namespace SmartWorkflow.Api.Authentication;

public interface IJwtService
{
    string GenerateToken(User user);
}

public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(User user)
    {
        var secret = Environment.GetEnvironmentVariable("JWT_SECRET") 
                     ?? _configuration["Jwt:Secret"] 
                     ?? "YourSuperSecretKeyWithAtLeast32CharactersRequiredForHmacSha256!";
        var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") 
                     ?? _configuration["Jwt:Issuer"] 
                     ?? "SmartWorkflowApi";
        var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") 
                       ?? _configuration["Jwt:Audience"] 
                       ?? "SmartWorkflowClient";
        var expiryMinutes = int.TryParse(_configuration["Jwt:ExpiryMinutes"], out var mins) ? mins : 1440;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email),
            new("department", user.Department)
        };

        foreach (var role in user.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
