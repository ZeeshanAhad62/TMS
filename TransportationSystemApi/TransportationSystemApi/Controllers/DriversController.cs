using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportationSystemApi.Data;
using TransportationSystemApi.Dtos;
using TransportationSystemApi.Mapping;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Controllers;

[ApiController]
[Route("api/drivers")]
public class DriversController : ControllerBase
{
    private readonly FleetDbContext _db;

    public DriversController(FleetDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<DriverListItemDto>>> GetAll(
        [FromQuery] DriverStatus? status,
        [FromQuery] string? search)
    {
        var query = _db.Drivers.AsNoTracking().Include(d => d.Advances).AsQueryable();

        if (status.HasValue)
            query = query.Where(d => d.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(d =>
                d.FullName.Contains(search) ||
                d.DriverCode.Contains(search) ||
                d.LicenseNumber.Contains(search));
        }

        var drivers = await query.OrderBy(d => d.DriverCode).ToListAsync();
        return drivers.Select(DriverMapper.ToListItemDto).ToList();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<DriverDetailDto>> GetById(int id)
    {
        var driver = await LoadFullDriver(id);
        if (driver is null) return NotFound();
        return DriverMapper.ToDetailDto(driver);
    }

    [HttpPost]
    public async Task<ActionResult<DriverDetailDto>> Create(DriverUpsertDto dto)
    {
        if (await _db.Drivers.AnyAsync(d => d.LicenseNumber == dto.LicenseNumber))
            return Conflict($"A driver with license number '{dto.LicenseNumber}' already exists.");

        var driver = new Driver();
        DriverMapper.ApplyUpsert(driver, dto);

        _db.Drivers.Add(driver);
        await _db.SaveChangesAsync();

        driver.DriverCode = $"DRV-{driver.Id:D5}";
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = driver.Id }, DriverMapper.ToDetailDto(driver));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, DriverUpsertDto dto)
    {
        var driver = await _db.Drivers.FindAsync(id);
        if (driver is null) return NotFound();

        if (await _db.Drivers.AnyAsync(d => d.Id != id && d.LicenseNumber == dto.LicenseNumber))
            return Conflict($"A driver with license number '{dto.LicenseNumber}' already exists.");

        DriverMapper.ApplyUpsert(driver, dto);
        driver.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var driver = await _db.Drivers.FindAsync(id);
        if (driver is null) return NotFound();

        _db.Drivers.Remove(driver);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    internal async Task<Driver?> LoadFullDriver(int id)
    {
        return await _db.Drivers
            .Include(d => d.Documents)
            .Include(d => d.Assignments).ThenInclude(a => a.Vehicle)
            .Include(d => d.Advances)
            .AsSplitQuery()
            .FirstOrDefaultAsync(d => d.Id == id);
    }
}
