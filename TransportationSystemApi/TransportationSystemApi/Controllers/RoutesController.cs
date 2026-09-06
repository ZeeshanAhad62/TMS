using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportationSystemApi.Data;
using TransportationSystemApi.Dtos;
using TransportationSystemApi.Mapping;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Controllers;

[ApiController]
[Route("api/routes")]
public class RoutesController : ControllerBase
{
    private readonly FleetDbContext _db;

    public RoutesController(FleetDbContext db)
    {
        _db = db;
    }

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    private IQueryable<RouteMaster> WithContracts() =>
        _db.Routes.Include(r => r.RateContracts).ThenInclude(c => c.Customer);

    [HttpGet]
    public async Task<ActionResult<List<RouteListItemDto>>> GetAll(
        [FromQuery] bool? activeOnly, [FromQuery] string? search)
    {
        var query = _db.Routes.Include(r => r.RateContracts).AsNoTracking().AsQueryable();

        if (activeOnly == true)
            query = query.Where(r => r.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(r =>
                r.RouteCode.Contains(search) ||
                (r.Name != null && r.Name.Contains(search)) ||
                r.Origin.Contains(search) ||
                r.Destination.Contains(search));
        }

        var routes = await query.OrderBy(r => r.Origin).ThenBy(r => r.Destination).ToListAsync();
        return routes.Select(RouteRateMapper.ToListItemDto).ToList();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RouteDetailDto>> GetById(int id)
    {
        var route = await WithContracts().AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
        if (route is null) return NotFound();
        return RouteRateMapper.ToDetailDto(route, Today);
    }

    [HttpPost]
    public async Task<ActionResult<RouteDetailDto>> Create(RouteUpsertDto dto)
    {
        var route = new RouteMaster();
        RouteRateMapper.ApplyUpsert(route, dto);

        _db.Routes.Add(route);
        await _db.SaveChangesAsync();

        route.RouteCode = $"RT-{route.Id:D5}";
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = route.Id }, RouteRateMapper.ToDetailDto(route, Today));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, RouteUpsertDto dto)
    {
        var route = await _db.Routes.FindAsync(id);
        if (route is null) return NotFound();

        RouteRateMapper.ApplyUpsert(route, dto);
        route.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var route = await _db.Routes.FindAsync(id);
        if (route is null) return NotFound();

        // Trips.RouteId is ON DELETE SET NULL; RateContracts cascade.
        _db.Routes.Remove(route);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
