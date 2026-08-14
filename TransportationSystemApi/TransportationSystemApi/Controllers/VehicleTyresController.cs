using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportationSystemApi.Data;
using TransportationSystemApi.Dtos;
using TransportationSystemApi.Mapping;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Controllers;

[ApiController]
[Route("api/vehicles/{vehicleId:int}/tyres")]
public class VehicleTyresController : ControllerBase
{
    private readonly FleetDbContext _db;

    public VehicleTyresController(FleetDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<TyreDto>>> GetAll(int vehicleId)
    {
        if (!await _db.Vehicles.AnyAsync(v => v.Id == vehicleId)) return NotFound();
        var tyres = await _db.Tyres.Include(t => t.ReplacementHistory)
            .Where(t => t.VehicleId == vehicleId).ToListAsync();
        return tyres.Select(VehicleMapper.ToDto).ToList();
    }

    [HttpPost]
    public async Task<ActionResult<TyreDto>> Create(int vehicleId, TyreUpsertDto dto)
    {
        if (!await _db.Vehicles.AnyAsync(v => v.Id == vehicleId)) return NotFound();

        var tyre = new Tyre
        {
            VehicleId = vehicleId,
            Position = dto.Position,
            BrandAndSize = dto.BrandAndSize,
            InstallationDate = dto.InstallationDate,
            InstallationOdometer = dto.InstallationOdometer,
            CurrentCondition = dto.CurrentCondition,
            LastRotationDate = dto.LastRotationDate
        };

        _db.Tyres.Add(tyre);
        await _db.SaveChangesAsync();
        return Ok(VehicleMapper.ToDto(tyre));
    }

    [HttpPut("{tyreId:int}")]
    public async Task<IActionResult> Update(int vehicleId, int tyreId, TyreUpsertDto dto)
    {
        var tyre = await _db.Tyres.FirstOrDefaultAsync(t => t.Id == tyreId && t.VehicleId == vehicleId);
        if (tyre is null) return NotFound();

        tyre.Position = dto.Position;
        tyre.BrandAndSize = dto.BrandAndSize;
        tyre.InstallationDate = dto.InstallationDate;
        tyre.InstallationOdometer = dto.InstallationOdometer;
        tyre.CurrentCondition = dto.CurrentCondition;
        tyre.LastRotationDate = dto.LastRotationDate;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{tyreId:int}")]
    public async Task<IActionResult> Delete(int vehicleId, int tyreId)
    {
        var tyre = await _db.Tyres.FirstOrDefaultAsync(t => t.Id == tyreId && t.VehicleId == vehicleId);
        if (tyre is null) return NotFound();

        _db.Tyres.Remove(tyre);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{tyreId:int}/replacements")]
    public async Task<ActionResult<TyreReplacementHistoryDto>> AddReplacement(int vehicleId, int tyreId, TyreReplacementHistoryUpsertDto dto)
    {
        var tyre = await _db.Tyres.FirstOrDefaultAsync(t => t.Id == tyreId && t.VehicleId == vehicleId);
        if (tyre is null) return NotFound();

        var history = new TyreReplacementHistory
        {
            TyreId = tyreId,
            ReplacedDate = dto.ReplacedDate,
            OdometerAtReplacement = dto.OdometerAtReplacement,
            OldBrandAndSize = dto.OldBrandAndSize,
            NewBrandAndSize = dto.NewBrandAndSize,
            Reason = dto.Reason
        };

        _db.TyreReplacementHistories.Add(history);

        // Replacing a tyre updates its current brand/size and installation baseline.
        if (!string.IsNullOrWhiteSpace(dto.NewBrandAndSize))
        {
            tyre.BrandAndSize = dto.NewBrandAndSize;
            tyre.InstallationDate = dto.ReplacedDate;
            tyre.InstallationOdometer = dto.OdometerAtReplacement;
        }

        await _db.SaveChangesAsync();
        return Ok(VehicleMapper.ToDto(history));
    }

    [HttpDelete("{tyreId:int}/replacements/{replacementId:int}")]
    public async Task<IActionResult> DeleteReplacement(int vehicleId, int tyreId, int replacementId)
    {
        var history = await _db.TyreReplacementHistories
            .Include(r => r.Tyre)
            .FirstOrDefaultAsync(r => r.Id == replacementId && r.TyreId == tyreId && r.Tyre!.VehicleId == vehicleId);
        if (history is null) return NotFound();

        _db.TyreReplacementHistories.Remove(history);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
