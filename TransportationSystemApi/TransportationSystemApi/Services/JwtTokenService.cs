using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Services;

public class JwtTokenService
{
    private readonly IConfiguration _config;

    public JwtTokenService(IConfiguration config)
    {
        _config = config;
    }

    public (string Token, DateTime ExpiresAt) CreateToken(User user)
    {
        var jwtSection = _config.GetSection("Jwt");
        var key = jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.");
        var issuer = jwtSection["Issuer"] ?? "TransportationSystemApi";
        var audience = jwtSection["Audience"] ?? "TransportationSystemApi.Web";
        var expiresMinutes = int.TryParse(jwtSection["ExpiresMinutes"], out var m) ? m : 480;

        var expiresAt = DateTime.UtcNow.AddMinutes(expiresMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new("fullName", user.FullName),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    // Token for the mobile driver app. Carries token_type = "driver-app" and
    // the driver id; deliberately has NO role claim so the StaffOnly filter
    // rejects it on every non-driver-app controller.
    public (string Token, DateTime ExpiresAt) CreateDriverToken(Driver driver)
    {
        var jwtSection = _config.GetSection("Jwt");
        var key = jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.");
        var issuer = jwtSection["Issuer"] ?? "TransportationSystemApi";
        var audience = jwtSection["Audience"] ?? "TransportationSystemApi.Web";
        var expiresDays = int.TryParse(jwtSection["DriverExpiresDays"], out var d) ? d : 30;

        var expiresAt = DateTime.UtcNow.AddDays(expiresDays);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, driver.Id.ToString()),
            new(ClaimTypes.NameIdentifier, driver.Id.ToString()),
            new(ClaimTypes.Name, driver.FullName),
            new("driverId", driver.Id.ToString()),
            new("driverCode", driver.DriverCode),
            new("token_type", "driver-app")
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
