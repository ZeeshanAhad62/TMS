using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportationSystemApi.Data;
using TransportationSystemApi.Dtos;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Controllers;

[ApiController]
[Route("api/login-history")]
[Authorize(Roles = nameof(UserRole.Admin))]
public class LoginHistoryController : ControllerBase
{
    private readonly FleetDbContext _db;

    public LoginHistoryController(FleetDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<LoginHistoryDto>>> GetAll([FromQuery] int? userId, [FromQuery] int limit = 100)
    {
        limit = Math.Clamp(limit, 1, 500);

        var query = _db.LoginHistories.AsNoTracking().AsQueryable();
        if (userId.HasValue) query = query.Where(h => h.UserId == userId.Value);

        var history = await query
            .OrderByDescending(h => h.LoginAt)
            .Take(limit)
            .ToListAsync();

        return history.Select(ToDto).ToList();
    }

    private static LoginHistoryDto ToDto(LoginHistory h) => new()
    {
        Id = h.Id,
        UserId = h.UserId,
        AttemptedUsername = h.AttemptedUsername,
        IsSuccessful = h.IsSuccessful,
        LoginAt = h.LoginAt,
        IpAddress = h.IpAddress,
        DeviceId = h.DeviceId,
        Browser = h.Browser,
        OperatingSystem = h.OperatingSystem,
        City = h.City,
        Region = h.Region,
        Country = h.Country
    };
}
