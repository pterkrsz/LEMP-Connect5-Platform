using LEMP.Application.Constants;
using LEMP.Application.Interfaces;
using LEMP.Application.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace LEMP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly ITwoFactorService _twoFactor;

    public AuthController(IConfiguration config, ITwoFactorService twoFactor)
    {
        _config = config;
        _twoFactor = twoFactor;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        string? role = dto.Username switch
        {
            "admin" when dto.Password == "password" => Roles.Admin,
            "operator" when dto.Password == "password" => Roles.Operator,
            "reader" when dto.Password == "password" => Roles.ReadOnly,
            _ => null
        };

        if (role is null)
        {
            return Unauthorized();
        }

        var secret = await _twoFactor.GetSecretAsync(dto.Username);
        if (secret is not null)
        {
            if (string.IsNullOrWhiteSpace(dto.Code) || !TotpGenerator.Verify(secret, dto.Code))
            {
                return Unauthorized();
            }
        }

        var token = GenerateToken(dto.Username, role);
        return Ok(new { token });
    }

    private string GenerateToken(string username, string role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role)
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Issuer"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [HttpPost("2fa")]
    public async Task<IActionResult> Verify2Fa([FromBody] TwoFaDto dto)
    {
        var secret = await _twoFactor.GetSecretAsync(dto.Username);
        if (secret is null)
        {
            return BadRequest();
        }

        if (!TotpGenerator.Verify(secret, dto.Code))
        {
            return Unauthorized();
        }

        return Ok();
    }

    public record LoginDto(string Username, string Password, string? Code);

    public record TwoFaDto(string Username, string Code);
}
