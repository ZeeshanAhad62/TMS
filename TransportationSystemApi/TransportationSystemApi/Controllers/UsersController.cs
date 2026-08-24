using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportationSystemApi.Data;
using TransportationSystemApi.Dtos;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = nameof(UserRole.Admin))]
public class UsersController : ControllerBase
{
    private readonly FleetDbContext _db;

    public UsersController(FleetDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> GetAll()
    {
        var users = await _db.Users.AsNoTracking().OrderBy(u => u.Username).ToListAsync();
        return users.Select(AuthController.ToDto).ToList();
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create(UserCreateDto dto)
    {
        if (await _db.Users.AnyAsync(u => u.Username == dto.Username))
            return Conflict($"A user with username '{dto.Username}' already exists.");
        if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
            return Conflict($"A user with email '{dto.Email}' already exists.");

        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            Password = dto.Password,
            FullName = dto.FullName,
            Role = dto.Role,
            IsActive = true
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), AuthController.ToDto(user));
    }

    [HttpPut("{id:int}/deactivate")]
    public async Task<IActionResult> Deactivate(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is null) return NotFound();

        user.IsActive = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("{id:int}/activate")]
    public async Task<IActionResult> Activate(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is null) return NotFound();

        user.IsActive = true;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
