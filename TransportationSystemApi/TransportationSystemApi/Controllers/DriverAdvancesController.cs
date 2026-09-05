using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportationSystemApi.Data;
using TransportationSystemApi.Dtos;
using TransportationSystemApi.Mapping;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Controllers;

[ApiController]
[Route("api/drivers/{driverId:int}/advances")]
public class DriverAdvancesController : ControllerBase
{
    private readonly FleetDbContext _db;

    public DriverAdvancesController(FleetDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<DriverAdvanceDto>>> GetAll(int driverId)
    {
        if (!await _db.Drivers.AnyAsync(d => d.Id == driverId)) return NotFound();
        var advances = await _db.DriverAdvances.AsNoTracking()
            .Where(a => a.DriverId == driverId)
            .OrderByDescending(a => a.Date).ThenByDescending(a => a.Id)
            .ToListAsync();
        return advances.Select(PayrollMapper.ToDto).ToList();
    }

    [HttpPost]
    public async Task<ActionResult<DriverAdvanceDto>> Create(int driverId, DriverAdvanceUpsertDto dto)
    {
        if (!await _db.Drivers.AnyAsync(d => d.Id == driverId)) return NotFound("Driver not found.");

        var advance = new DriverAdvance { DriverId = driverId };
        PayrollMapper.ApplyUpsert(advance, dto);

        _db.DriverAdvances.Add(advance);
        await _db.SaveChangesAsync();
        return Ok(PayrollMapper.ToDto(advance));
    }

    [HttpPut("{advanceId:int}")]
    public async Task<IActionResult> Update(int driverId, int advanceId, DriverAdvanceUpsertDto dto)
    {
        var advance = await _db.DriverAdvances.FirstOrDefaultAsync(a => a.Id == advanceId && a.DriverId == driverId);
        if (advance is null) return NotFound();

        PayrollMapper.ApplyUpsert(advance, dto);
        advance.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{advanceId:int}")]
    public async Task<IActionResult> Delete(int driverId, int advanceId)
    {
        var advance = await _db.DriverAdvances.FirstOrDefaultAsync(a => a.Id == advanceId && a.DriverId == driverId);
        if (advance is null) return NotFound();

        _db.DriverAdvances.Remove(advance);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
