using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportationSystemApi.Data;
using TransportationSystemApi.Dtos;
using TransportationSystemApi.Mapping;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Controllers;

[ApiController]
[Route("api/trips")]
public class TripsController : ControllerBase
{
    private readonly FleetDbContext _db;

    public TripsController(FleetDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<TripListItemDto>>> GetAll(
        [FromQuery] int? vehicleId,
        [FromQuery] int? driverId,
        [FromQuery] TripStatus? status,
        [FromQuery] string? search)
    {
        var query = _db.Trips.AsNoTracking().Include(t => t.Vehicle).Include(t => t.Driver).AsQueryable();

        if (vehicleId.HasValue)
            query = query.Where(t => t.VehicleId == vehicleId.Value);

        if (driverId.HasValue)
            query = query.Where(t => t.DriverId == driverId.Value);

        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(t =>
                t.TripCode.Contains(search) ||
                t.Origin.Contains(search) ||
                t.Destination.Contains(search));
        }

        var trips = await query.OrderByDescending(t => t.StartDate).ToListAsync();
        return trips.Select(TripMapper.ToListItemDto).ToList();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TripDetailDto>> GetById(int id)
    {
        var trip = await _db.Trips.Include(t => t.Vehicle).Include(t => t.Driver).FirstOrDefaultAsync(t => t.Id == id);
        if (trip is null) return NotFound();
        return TripMapper.ToDetailDto(trip);
    }

    [HttpPost]
    public async Task<ActionResult<TripDetailDto>> Create(TripUpsertDto dto)
    {
        var vehicle = await _db.Vehicles.FindAsync(dto.VehicleId);
        if (vehicle is null) return NotFound("Vehicle not found.");

        var driver = await _db.Drivers.FindAsync(dto.DriverId);
        if (driver is null) return NotFound("Driver not found.");

        var trip = new Trip { Vehicle = vehicle, Driver = driver };
        TripMapper.ApplyUpsert(trip, dto);

        _db.Trips.Add(trip);
        await _db.SaveChangesAsync();

        trip.TripCode = $"TRP-{trip.Id:D5}";
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = trip.Id }, TripMapper.ToDetailDto(trip));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, TripUpsertDto dto)
    {
        var trip = await _db.Trips.FindAsync(id);
        if (trip is null) return NotFound();

        if (!await _db.Vehicles.AnyAsync(v => v.Id == dto.VehicleId)) return NotFound("Vehicle not found.");
        if (!await _db.Drivers.AnyAsync(d => d.Id == dto.DriverId)) return NotFound("Driver not found.");

        TripMapper.ApplyUpsert(trip, dto);
        trip.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var trip = await _db.Trips.FindAsync(id);
        if (trip is null) return NotFound();

        _db.Trips.Remove(trip);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
