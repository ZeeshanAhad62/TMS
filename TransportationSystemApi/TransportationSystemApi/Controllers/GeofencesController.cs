using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportationSystemApi.Data;
using TransportationSystemApi.Dtos;
using TransportationSystemApi.Mapping;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Controllers;

[ApiController]
[Route("api/geofences")]
public class GeofencesController : ControllerBase
{
    private readonly FleetDbContext _db;

    public GeofencesController(FleetDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<GeofenceListItemDto>>> GetAll([FromQuery] bool? activeOnly, [FromQuery] string? search)
    {
        var query = _db.Geofences.AsNoTracking().AsQueryable();

        if (activeOnly == true)
            query = query.Where(g => g.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(g => g.Name.Contains(search));

        var fences = await query.OrderBy(g => g.Name).ToListAsync();
        return fences.Select(TrackingMapper.ToListItemDto).ToList();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<GeofenceDetailDto>> GetById(int id)
    {
        var fence = await _db.Geofences.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id);
        if (fence is null) return NotFound();
        return TrackingMapper.ToDetailDto(fence);
    }

    [HttpPost]
    public async Task<ActionResult<GeofenceDetailDto>> Create(GeofenceUpsertDto dto)
    {
        var problem = Validate(dto);
        if (problem is not null) return BadRequest(problem);

        if (await _db.Geofences.AnyAsync(g => g.Name == dto.Name.Trim()))
            return BadRequest("A geofence with this name already exists.");

        var fence = new Geofence();
        TrackingMapper.ApplyUpsert(fence, dto);

        _db.Geofences.Add(fence);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = fence.Id }, TrackingMapper.ToDetailDto(fence));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, GeofenceUpsertDto dto)
    {
        var fence = await _db.Geofences.FindAsync(id);
        if (fence is null) return NotFound();

        var problem = Validate(dto);
        if (problem is not null) return BadRequest(problem);

        if (await _db.Geofences.AnyAsync(g => g.Id != id && g.Name == dto.Name.Trim()))
            return BadRequest("A geofence with this name already exists.");

        TrackingMapper.ApplyUpsert(fence, dto);
        fence.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var fence = await _db.Geofences.FindAsync(id);
        if (fence is null) return NotFound();

        _db.Geofences.Remove(fence); // GeofenceEvents cascade
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static string? Validate(GeofenceUpsertDto dto)
    {
        if (dto.Shape == GeofenceShape.Circle)
        {
            if (dto.CenterLat is null || dto.CenterLng is null || dto.RadiusMeters is null || dto.RadiusMeters <= 0)
                return "A circle geofence needs a centre latitude, longitude, and a radius greater than zero.";
        }
        else
        {
            if (GeoGeometry.ParsePolygon(dto.PolygonJson).Count < 3)
                return "A polygon geofence needs at least 3 points as JSON: [{\"lat\":..,\"lng\":..}, ...].";
        }
        return null;
    }
}
