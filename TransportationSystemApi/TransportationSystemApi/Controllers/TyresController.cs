using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportationSystemApi.Data;
using TransportationSystemApi.Dtos;
using TransportationSystemApi.Mapping;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Controllers;

// Full tyre-as-asset module. The nested api/vehicles/{id}/tyres (VehicleTyresController)
// stays as-is for the vehicle editor's quick-add tab; this controller is the standalone
// list/editor + lifecycle event log, including tyres currently sitting in stock
// (VehicleId == null).
[ApiController]
[Route("api/tyres")]
public class TyresController : ControllerBase
{
    private readonly FleetDbContext _db;

    public TyresController(FleetDbContext db)
    {
        _db = db;
    }

    private IQueryable<Tyre> BaseQuery() =>
        _db.Tyres.Include(t => t.Vehicle).Include(t => t.Events).AsNoTracking();

    [HttpGet]
    public async Task<ActionResult<List<TyreListItemDto>>> GetAll(
        [FromQuery] TyreStatus? status, [FromQuery] int? vehicleId, [FromQuery] string? search)
    {
        var query = BaseQuery();

        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);

        if (vehicleId.HasValue)
            query = query.Where(t => t.VehicleId == vehicleId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(t =>
                (t.SerialNumber != null && t.SerialNumber.Contains(search)) ||
                (t.BrandAndSize != null && t.BrandAndSize.Contains(search)) ||
                (t.Vehicle != null && t.Vehicle.RegistrationNumber.Contains(search)));
        }

        var tyres = await query.OrderBy(t => t.Status).ThenByDescending(t => t.Id).ToListAsync();
        return tyres.Select(TyreMapper.ToListItemDto).ToList();
    }

    [HttpGet("stock")]
    public async Task<ActionResult<List<TyreListItemDto>>> GetStock()
    {
        var tyres = await BaseQuery()
            .Where(t => t.VehicleId == null && t.Status != TyreStatus.Scrapped)
            .OrderByDescending(t => t.Id)
            .ToListAsync();
        return tyres.Select(TyreMapper.ToListItemDto).ToList();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TyreDetailDto>> GetById(int id)
    {
        var tyre = await BaseQuery().FirstOrDefaultAsync(t => t.Id == id);
        if (tyre is null) return NotFound();
        return TyreMapper.ToDetailDto(tyre);
    }

    [HttpPost]
    public async Task<ActionResult<TyreDetailDto>> Create(TyreCreateDto dto)
    {
        var tyre = new Tyre { Status = TyreStatus.InStock, Position = TyrePosition.Other };
        TyreMapper.ApplyUpsert(tyre, dto);

        _db.Tyres.Add(tyre);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = tyre.Id }, TyreMapper.ToDetailDto(tyre));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, TyreCreateDto dto)
    {
        var tyre = await _db.Tyres.FindAsync(id);
        if (tyre is null) return NotFound();

        TyreMapper.ApplyUpsert(tyre, dto);
        tyre.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var tyre = await _db.Tyres.FindAsync(id);
        if (tyre is null) return NotFound();

        _db.Tyres.Remove(tyre);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id:int}/events")]
    public async Task<ActionResult<List<TyreEventDto>>> GetEvents(int id)
    {
        if (!await _db.Tyres.AnyAsync(t => t.Id == id)) return NotFound();

        var events = await _db.TyreEvents.AsNoTracking()
            .Where(e => e.TyreId == id)
            .OrderByDescending(e => e.EventDate).ThenByDescending(e => e.Id)
            .ToListAsync();
        return events.Select(TyreMapper.ToEventDto).ToList();
    }

    // Fit/Remove/Rotate/Retread/Inspect/Scrap all funnel through here so the
    // tyre's Status/VehicleId/Position/InstallationOdometer stay in lock-step
    // with the event log instead of being edited independently.
    [HttpPost("{id:int}/events")]
    public async Task<ActionResult<TyreEventDto>> AddEvent(int id, TyreEventUpsertDto dto)
    {
        var tyre = await _db.Tyres.Include(t => t.Vehicle).FirstOrDefaultAsync(t => t.Id == id);
        if (tyre is null) return NotFound();

        if (tyre.Status == TyreStatus.Scrapped)
            return BadRequest("This tyre is scrapped and can no longer be fitted, rotated, or retreaded.");

        switch (dto.EventType)
        {
            case TyreEventType.Fit:
                if (dto.VehicleId is null) return BadRequest("VehicleId is required to fit a tyre.");
                if (tyre.VehicleId is not null) return BadRequest("This tyre is already fitted; remove it first.");
                if (!await _db.Vehicles.AnyAsync(v => v.Id == dto.VehicleId)) return BadRequest("Vehicle not found.");

                tyre.VehicleId = dto.VehicleId;
                tyre.Position = dto.Position ?? TyrePosition.Other;
                tyre.InstallationDate = dto.EventDate;
                tyre.InstallationOdometer = dto.Odometer;
                tyre.Status = TyreStatus.Fitted;
                break;

            case TyreEventType.Remove:
                if (tyre.VehicleId is null) return BadRequest("This tyre is not currently fitted.");

                tyre.TotalDistanceRunCarried += Stint(tyre, dto.Odometer);
                tyre.VehicleId = null;
                tyre.InstallationDate = null;
                tyre.InstallationOdometer = null;
                tyre.Status = TyreStatus.InStock;
                break;

            case TyreEventType.Rotate:
                if (tyre.VehicleId is null) return BadRequest("This tyre must be fitted before it can be rotated.");
                if (dto.Position is null) return BadRequest("Position is required to rotate a tyre.");

                tyre.TotalDistanceRunCarried += Stint(tyre, dto.Odometer);
                tyre.Position = dto.Position.Value;
                tyre.InstallationOdometer = dto.Odometer;
                tyre.LastRotationDate = dto.EventDate;
                break;

            case TyreEventType.Retread:
                if (tyre.VehicleId is not null) return BadRequest("Remove the tyre from its vehicle before logging a retread.");
                break; // logged as-is; its Cost feeds CostPerKm

            case TyreEventType.Inspect:
                if (!string.IsNullOrWhiteSpace(dto.Notes)) tyre.CurrentCondition = dto.Notes;
                break;

            case TyreEventType.Scrap:
                tyre.TotalDistanceRunCarried += Stint(tyre, dto.Odometer);
                tyre.VehicleId = null;
                tyre.InstallationDate = null;
                tyre.InstallationOdometer = null;
                tyre.Status = TyreStatus.Scrapped;
                break;
        }

        var evt = new TyreEvent
        {
            TyreId = id,
            EventType = dto.EventType,
            EventDate = dto.EventDate,
            VehicleId = dto.EventType is TyreEventType.Remove or TyreEventType.Scrap ? null : dto.VehicleId ?? tyre.VehicleId,
            Position = dto.Position ?? (dto.EventType == TyreEventType.Fit ? tyre.Position : null),
            Odometer = dto.Odometer,
            Cost = dto.Cost,
            Notes = dto.Notes
        };
        _db.TyreEvents.Add(evt);
        tyre.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(TyreMapper.ToEventDto(evt));
    }

    [HttpDelete("{id:int}/events/{eventId:int}")]
    public async Task<IActionResult> DeleteEvent(int id, int eventId)
    {
        var evt = await _db.TyreEvents.FirstOrDefaultAsync(e => e.Id == eventId && e.TyreId == id);
        if (evt is null) return NotFound();

        _db.TyreEvents.Remove(evt);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // Distance covered since the tyre's current InstallationOdometer baseline.
    private static decimal Stint(Tyre tyre, decimal? eventOdometer)
    {
        if (tyre.InstallationOdometer is not decimal baseOdo) return 0;
        var end = eventOdometer ?? tyre.Vehicle?.CurrentOdometerReading ?? baseOdo;
        return Math.Max(0, end - baseOdo);
    }
}
