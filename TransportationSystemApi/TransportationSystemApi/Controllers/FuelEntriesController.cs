using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportationSystemApi.Data;
using TransportationSystemApi.Dtos;
using TransportationSystemApi.Mapping;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Controllers;

[ApiController]
[Route("api/fuel-entries")]
public class FuelEntriesController : ControllerBase
{
    private readonly FleetDbContext _db;

    public FuelEntriesController(FleetDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<FuelEntryListItemDto>>> GetAll(
        [FromQuery] int? vehicleId,
        [FromQuery] int? driverId,
        [FromQuery] int? tripId,
        [FromQuery] FuelType? fuelType,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] string? search)
    {
        var query = _db.FuelEntries.AsNoTracking()
            .Include(f => f.Vehicle)
            .Include(f => f.Driver)
            .AsQueryable();

        if (vehicleId.HasValue) query = query.Where(f => f.VehicleId == vehicleId.Value);
        if (driverId.HasValue) query = query.Where(f => f.DriverId == driverId.Value);
        if (tripId.HasValue) query = query.Where(f => f.TripId == tripId.Value);
        if (fuelType.HasValue) query = query.Where(f => f.FuelType == fuelType.Value);
        if (from.HasValue) query = query.Where(f => f.Date >= from.Value);
        if (to.HasValue) query = query.Where(f => f.Date <= to.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(f =>
                f.FuelEntryCode.Contains(search) ||
                (f.StationName != null && f.StationName.Contains(search)) ||
                (f.SlipNumber != null && f.SlipNumber.Contains(search)));
        }

        var entries = await query
            .OrderByDescending(f => f.Date)
            .ThenByDescending(f => f.Id)
            .ToListAsync();

        // Build a per-vehicle chain to derive distance/mileage against the prior fill.
        var prevOdo = new Dictionary<int, decimal?>();
        var prevFull = new Dictionary<int, bool>();
        foreach (var g in entries.GroupBy(e => e.VehicleId))
        {
            decimal? last = null;
            var lastFull = false;
            foreach (var e in g.OrderBy(e => e.Date).ThenBy(e => e.Id))
            {
                prevOdo[e.Id] = last;
                prevFull[e.Id] = lastFull;
                last = e.OdometerReading;
                lastFull = e.IsTankFull;
            }
        }

        return entries
            .Select(e => FuelEntryMapper.ToListItemDto(e, prevOdo[e.Id], prevFull[e.Id]))
            .ToList();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<FuelEntryDetailDto>> GetById(int id)
    {
        var entry = await _db.FuelEntries
            .Include(f => f.Vehicle)
            .Include(f => f.Driver)
            .Include(f => f.Trip)
            .FirstOrDefaultAsync(f => f.Id == id);
        if (entry is null) return NotFound();

        var (prevOdo, prevFull) = await PreviousFillAsync(entry.VehicleId, entry.Date, entry.Id);
        return FuelEntryMapper.ToDetailDto(entry, prevOdo, prevFull);
    }

    [HttpPost]
    public async Task<ActionResult<FuelEntryDetailDto>> Create(FuelEntryUpsertDto dto)
    {
        var validation = await ValidateRefsAsync(dto);
        if (validation is not null) return validation;

        var entry = new FuelEntry();
        FuelEntryMapper.ApplyUpsert(entry, dto);

        _db.FuelEntries.Add(entry);
        await _db.SaveChangesAsync();

        entry.FuelEntryCode = $"FE-{entry.Id:D5}";
        await _db.SaveChangesAsync();

        await _db.Entry(entry).Reference(e => e.Vehicle).LoadAsync();
        await _db.Entry(entry).Reference(e => e.Driver).LoadAsync();
        await _db.Entry(entry).Reference(e => e.Trip).LoadAsync();

        var (prevOdo, prevFull) = await PreviousFillAsync(entry.VehicleId, entry.Date, entry.Id);
        return CreatedAtAction(nameof(GetById), new { id = entry.Id },
            FuelEntryMapper.ToDetailDto(entry, prevOdo, prevFull));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, FuelEntryUpsertDto dto)
    {
        var entry = await _db.FuelEntries.FindAsync(id);
        if (entry is null) return NotFound();

        var validation = await ValidateRefsAsync(dto);
        if (validation is not null) return validation;

        FuelEntryMapper.ApplyUpsert(entry, dto);
        entry.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var entry = await _db.FuelEntries.FindAsync(id);
        if (entry is null) return NotFound();

        _db.FuelEntries.Remove(entry);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<ActionResult?> ValidateRefsAsync(FuelEntryUpsertDto dto)
    {
        if (!await _db.Vehicles.AnyAsync(v => v.Id == dto.VehicleId))
            return NotFound("Vehicle not found.");

        if (dto.DriverId.HasValue && !await _db.Drivers.AnyAsync(d => d.Id == dto.DriverId.Value))
            return NotFound("Driver not found.");

        if (dto.TripId.HasValue && !await _db.Trips.AnyAsync(t => t.Id == dto.TripId.Value))
            return NotFound("Trip not found.");

        return null;
    }

    private async Task<(decimal? odometer, bool wasFull)> PreviousFillAsync(int vehicleId, DateOnly date, int excludeId)
    {
        var prev = await _db.FuelEntries.AsNoTracking()
            .Where(f => f.VehicleId == vehicleId && f.Id != excludeId &&
                        (f.Date < date || (f.Date == date && f.Id < excludeId)))
            .OrderByDescending(f => f.Date)
            .ThenByDescending(f => f.Id)
            .FirstOrDefaultAsync();

        return prev is null ? (null, false) : (prev.OdometerReading, prev.IsTankFull);
    }
}
