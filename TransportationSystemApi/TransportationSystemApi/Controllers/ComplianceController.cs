using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportationSystemApi.Data;
using TransportationSystemApi.Dtos;
using TransportationSystemApi.Mapping;

namespace TransportationSystemApi.Controllers;

[ApiController]
[Route("api/compliance")]
public class ComplianceController : ControllerBase
{
    private readonly FleetDbContext _db;

    public ComplianceController(FleetDbContext db)
    {
        _db = db;
    }

    private async Task<List<ComplianceItemDto>> ScanAsync(int withinDays)
    {
        var vehicles = await _db.Vehicles.AsNoTracking().ToListAsync();
        var drivers = await _db.Drivers.AsNoTracking().ToListAsync();
        return ComplianceScanner.Scan(vehicles, drivers, withinDays);
    }

    [HttpGet("expiries")]
    public async Task<ActionResult<List<ComplianceItemDto>>> GetExpiries(
        [FromQuery] ComplianceEntityType? entityType,
        [FromQuery] ComplianceSeverity? severity,
        [FromQuery] int withinDays = ComplianceScanner.DefaultWindowDays,
        [FromQuery] string? search = null)
    {
        withinDays = Math.Clamp(withinDays, 0, 3650);
        var items = await ScanAsync(withinDays);

        if (entityType.HasValue)
            items = items.Where(i => i.EntityType == entityType.Value).ToList();

        if (severity.HasValue)
            items = items.Where(i => i.Severity == severity.Value).ToList();

        if (!string.IsNullOrWhiteSpace(search))
        {
            items = items.Where(i =>
                i.EntityCode.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                i.EntityName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                i.DocumentType.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return items;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ComplianceSummaryDto>> GetSummary(
        [FromQuery] int withinDays = ComplianceScanner.DefaultWindowDays)
    {
        withinDays = Math.Clamp(withinDays, 0, 3650);
        var items = await ScanAsync(withinDays);
        return ComplianceScanner.Summarise(items);
    }
}
