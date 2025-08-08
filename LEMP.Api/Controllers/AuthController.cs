using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LEMP.Api.Models;
using LEMP.Api.Models.Login;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging;

namespace LEMP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccessTestController : ControllerBase
{
    private readonly ILogger<AccessTestController> _logger;

    public AccessTestController(ILogger<AccessTestController> logger)
    {
        _logger = logger;
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("admin-only")]
    public IActionResult AdminAccess()
    {
        _logger.LogInformation("AdminAccess endpoint called by user: {User}", User.Identity?.Name);
        return Ok("Ez csak az Admin szerepkörrel érhető el.");
    }

    [Authorize(Roles = "Operator")]
    [HttpGet("operator-access")]
    public IActionResult OperatorAccess()
    {
        _logger.LogInformation("OperatorAccess endpoint called by user: {User}", User.Identity?.Name);
        return Ok("Ez csak az Operatornak szól.");
    }

    [Authorize(Roles = "KEP")]
    [HttpGet("kep-endpoint")]
    public IActionResult KepAccess()
    {
        _logger.LogInformation("KepAccess endpoint called by user: {User}", User.Identity?.Name);
        return Ok("Ez a KEP rendszernek van fenntartva.");
    }

    [Authorize(Roles = "Viewer")]
    [HttpGet("viewer")]
    public IActionResult ViewerUI()
    {
        _logger.LogInformation("ViewerUI endpoint called by user: {User}", User.Identity?.Name);
        return Ok("Ez a grafikus UI felülethez tartozik.");
    }
}

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IConfiguration config, ILogger<AuthController> logger)
    {
        _config = config;
        _logger = logger;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        _logger.LogInformation("Login attempt for user {Username}", request.Username);

        var usersSection = _config.GetSection("Users");

        foreach (var user in usersSection.GetChildren())
        {
            if (user["Username"] == request.Username && user["Password"] == request.Password)
            {
                _logger.LogInformation("Login successful for user: {Username}", request.Username);

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

                _logger.LogInformation("User {Username} authenticated", request.Username);

                return Ok(new LoginResponse
                {
                    Token = new JwtSecurityTokenHandler().WriteToken(token),
                    ExpiresAt = token.ValidTo
                });
            }
        }

        _logger.LogWarning("Invalid login attempt for user {Username}", request.Username);
        return Unauthorized("Invalid username or password");
    }
}
