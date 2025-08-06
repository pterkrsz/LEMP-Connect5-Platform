using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LEMP.Api.Models;
using LEMP.Api.Models.Login;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace LEMP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccessTestController : ControllerBase
{
    [Authorize(Roles = "Admin")]
    [HttpGet("admin-only")]
    public IActionResult AdminAccess() => Ok("Ez csak az Admin szerepkörrel érhető el.");

    [Authorize(Roles = "Operator")]
    [HttpGet("operator-access")]
    public IActionResult OperatorAccess() => Ok("Ez csak az Operatornak szól.");

    [Authorize(Roles = "KEP")]
    [HttpGet("kep-endpoint")]
    public IActionResult KepAccess() => Ok("Ez a KEP rendszernek van fenntartva.");

    [Authorize(Roles = "Viewer")]
    [HttpGet("viewer")]
    public IActionResult ViewerUI() => Ok("Ez a grafikus UI felülethez tartozik.");
}

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _config;

    public AuthController(IConfiguration config)
    {
        _config = config;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        var usersSection = _config.GetSection("Users");

        foreach (var user in usersSection.GetChildren())
        {
            if (user["Username"] == request.Username && user["Password"] == request.Password)
            {
                var roles = user.GetSection("Roles").Get<string[]>() ?? Array.Empty<string>();
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var claims = new List<Claim>
                {
                    new(ClaimTypes.Name, request.Username)
                };
                claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

                var token = new JwtSecurityToken(
                    issuer: _config["Jwt:Issuer"],
                    audience: _config["Jwt:Audience"],
                    claims: claims,
                    expires: DateTime.UtcNow.AddHours(1),
                    signingCredentials: creds
                );

                return Ok(new LoginResponse
                {
                    Token = new JwtSecurityTokenHandler().WriteToken(token),
                    ExpiresAt = token.ValidTo
                });
            }
        }

        return Unauthorized("Invalid username or password");
    }
}
