using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportationSystemApi.Data;
using TransportationSystemApi.Dtos;
using TransportationSystemApi.Mapping;
using TransportationSystemApi.Services;

namespace TransportationSystemApi.Controllers;

[ApiController]
[Route("api/compliance")]
public class ComplianceController : ControllerBase
{
    private readonly FleetDbContext _db;
    private readonly ComplianceAlertService _alertService;

    public ComplianceController(FleetDbContext db, ComplianceAlertService alertService)
    {
        _db = db;
        _alertService = alertService;
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

    [HttpGet("document-types")]
    public ActionResult<string[]> GetDocumentTypes() => ComplianceScanner.KnownDocumentTypes;

    [HttpGet("alert-log")]
    public async Task<ActionResult<List<AlertLogDto>>> GetAlertLog([FromQuery] int take = 50)
    {
        take = Math.Clamp(take, 1, 500);
        var log = await _db.AlertLog.AsNoTracking()
            .OrderByDescending(l => l.SentAt)
            .Take(take)
            .ToListAsync();
        return log.Select(AlertConfigMapper.ToLogDto).ToList();
    }

    [HttpPost("run-alerts")]
    public async Task<ActionResult<AlertRunResultDto>> RunAlertsNow(CancellationToken ct)
    {
        return await _alertService.RunScanAsync(ct);
    }
}
