using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportationSystemApi.Data;
using TransportationSystemApi.Dtos;
using TransportationSystemApi.Models;
using TransportationSystemApi.Services;

namespace TransportationSystemApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly FleetDbContext _db;
    private readonly JwtTokenService _tokenService;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public AuthController(FleetDbContext db, JwtTokenService tokenService)
    {
        _db = db;
        _tokenService = tokenService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponseDto>> Login(LoginRequestDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == dto.Username);
        if (user is null || !user.IsActive)
            return Unauthorized("Invalid username or password.");

        var verifyResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
        if (verifyResult == PasswordVerificationResult.Failed)
            return Unauthorized("Invalid username or password.");

        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var (token, expiresAt) = _tokenService.CreateToken(user);

        return new LoginResponseDto
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = ToDto(user)
        };
    }

    internal static UserDto ToDto(User u) => new()
    {
        Id = u.Id,
        Username = u.Username,
        Email = u.Email,
        FullName = u.FullName,
        Role = u.Role,
        IsActive = u.IsActive,
        CreatedAt = u.CreatedAt,
        LastLoginAt = u.LastLoginAt
    };
}
