using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
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
    private readonly GeoLocationService _geoLocation;

    public AuthController(FleetDbContext db, JwtTokenService tokenService, GeoLocationService geoLocation)
    {
        _db = db;
        _tokenService = tokenService;
        _geoLocation = geoLocation;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponseDto>> Login(LoginRequestDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == dto.Username);
        var passwordOk = user is not null && FixedTimeEquals(user.Password, dto.Password);

        if (user is null || !user.IsActive || !passwordOk)
        {
            await RecordAttemptAsync(user, dto, isSuccessful: false);
            return Unauthorized("Invalid username or password.");
        }

        user.LastLoginAt = DateTime.UtcNow;
        await RecordAttemptAsync(user, dto, isSuccessful: true);

        var (token, expiresAt) = _tokenService.CreateToken(user);

        return new LoginResponseDto
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = ToDto(user)
        };
    }

    private async Task RecordAttemptAsync(User? user, LoginRequestDto dto, bool isSuccessful)
    {
        var (browser, os) = UserAgentParser.Parse(dto.UserAgent);
        var (city, region, country) = await _geoLocation.LookupAsync(dto.IpAddress);

        _db.LoginHistories.Add(new LoginHistory
        {
            UserId = user?.Id,
            AttemptedUsername = dto.Username,
            IsSuccessful = isSuccessful,
            IpAddress = dto.IpAddress,
            DeviceId = dto.DeviceId,
            UserAgent = dto.UserAgent,
            Browser = browser,
            OperatingSystem = os,
            City = city,
            Region = region,
            Country = country
        });

        await _db.SaveChangesAsync();
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        var bytesA = Encoding.UTF8.GetBytes(a);
        var bytesB = Encoding.UTF8.GetBytes(b);
        if (bytesA.Length != bytesB.Length) return false;
        return CryptographicOperations.FixedTimeEquals(bytesA, bytesB);
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
