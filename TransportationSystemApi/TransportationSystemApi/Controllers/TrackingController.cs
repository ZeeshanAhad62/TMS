using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TransportationSystemApi.Data;
using TransportationSystemApi.Dtos;
using TransportationSystemApi.Mapping;
using TransportationSystemApi.Models;
using TransportationSystemApi.Services;

namespace TransportationSystemApi.Controllers;

[ApiController]
[Route("api/tracking")]
public class TrackingController : ControllerBase
{
    private readonly FleetDbContext _db;
    private readonly TrackingOptions _opts;

    public TrackingController(FleetDbContext db, IOptions<TrackingOptions> opts)
    {
        _db = db;
        _opts = opts.Value;
    }

    // Provider-agnostic webhook. AllowAnonymous because a telematics provider
    // can't do an interactive login; when Tracking:IngestKey is configured the
    // caller must send it as X-Tracking-Key.
    [AllowAnonymous]
    [HttpPost("ingest")]
    public async Task<ActionResult<IngestResultDto>> Ingest(IngestRequestDto request)
    {
        if (!string.IsNullOrEmpty(_opts.IngestKey))
        {
            var key = Request.Headers["X-Tracking-Key"].ToString();
            if (!string.Equals(key, _opts.IngestKey, StringComparison.Ordinal))
                return Unauthorized("Invalid or missing X-Tracking-Key.");
        }

        var result = new IngestResultDto();
        if (request.Reports.Count == 0) return Ok(result);

        var vehicleIds = request.Reports.Select(r => r.VehicleId).Distinct().ToList();
        var knownVehicleIds = (await _db.Vehicles
                .Where(v => vehicleIds.Contains(v.Id))
                .Select(v => v.Id)
                .ToListAsync())
            .ToHashSet();

        var geofences = await _db.Geofences.AsNoTracking().Where(g => g.IsActive).ToListAsync();

        // Per-(vehicle,geofence) "currently inside?" state, seeded from the last
        // stored event and then advanced as we walk this batch in time order.
        var insideState = new Dictionary<(int VehicleId, int GeofenceId), bool>();
        var affectedVehicles = new HashSet<int>();

        foreach (var group in request.Reports.GroupBy(r => r.VehicleId))
        {
            if (!knownVehicleIds.Contains(group.Key))
            {
                result.Rejected += group.Count();
                result.Errors.Add($"Vehicle {group.Key} not found.");
                continue;
            }

            foreach (var report in group.OrderBy(r => r.DeviceTimeUtc ?? DateTime.UtcNow))
            {
                var deviceTime = DateTime.SpecifyKind(report.DeviceTimeUtc ?? DateTime.UtcNow, DateTimeKind.Utc);

                _db.VehiclePositions.Add(new VehiclePosition
                {
                    VehicleId = report.VehicleId,
                    Latitude = report.Latitude,
                    Longitude = report.Longitude,
                    SpeedKph = report.SpeedKph,
                    Heading = report.Heading,
                    Ignition = report.Ignition,
                    DeviceTimeUtc = deviceTime,
                    Source = report.Source
                });
                result.Accepted++;
                affectedVehicles.Add(report.VehicleId);

                foreach (var fence in geofences)
                {
                    var stateKey = (report.VehicleId, fence.Id);
                    if (!insideState.TryGetValue(stateKey, out var wasInside))
                    {
                        var lastEvent = await _db.GeofenceEvents.AsNoTracking()
                            .Where(e => e.VehicleId == report.VehicleId && e.GeofenceId == fence.Id)
                            .OrderByDescending(e => e.OccurredAtUtc).ThenByDescending(e => e.Id)
                            .FirstOrDefaultAsync();
                        wasInside = lastEvent?.EventType == GeofenceEventType.Enter;
                        insideState[stateKey] = wasInside;
                    }

                    var nowInside = GeoGeometry.Contains(fence, (double)report.Latitude, (double)report.Longitude);
                    if (nowInside == wasInside) continue;

                    _db.GeofenceEvents.Add(new GeofenceEvent
                    {
                        GeofenceId = fence.Id,
                        VehicleId = report.VehicleId,
                        EventType = nowInside ? GeofenceEventType.Enter : GeofenceEventType.Exit,
                        OccurredAtUtc = deviceTime,
                        Latitude = report.Latitude,
                        Longitude = report.Longitude
                    });
                    insideState[stateKey] = nowInside;
                    result.GeofenceEventsRaised++;
                }
            }
        }

        await _db.SaveChangesAsync();

        // Prune each affected vehicle's position history down to the hot set.
        foreach (var vid in affectedVehicles)
        {
            var cutoff = await _db.VehiclePositions
                .Where(p => p.VehicleId == vid)
                .OrderByDescending(p => p.DeviceTimeUtc).ThenByDescending(p => p.Id)
                .Skip(_opts.MaxHotPositionsPerVehicle)
                .Select(p => (DateTime?)p.DeviceTimeUtc)
                .FirstOrDefaultAsync();

            if (cutoff is not null)
                await _db.VehiclePositions
                    .Where(p => p.VehicleId == vid && p.DeviceTimeUtc <= cutoff)
                    .ExecuteDeleteAsync();
        }

        return Ok(result);
    }

    [HttpGet("live")]
    public async Task<ActionResult<List<LiveVehicleDto>>> GetLive()
    {
        var vehicles = await _db.Vehicles.AsNoTracking()
            .OrderBy(v => v.VehicleCode)
            .ToListAsync();

        var latestByVehicle = await _db.VehiclePositions.AsNoTracking()
            .GroupBy(p => p.VehicleId)
            .Select(grp => grp.OrderByDescending(p => p.DeviceTimeUtc).ThenByDescending(p => p.Id).First())
            .ToListAsync();
        var latestMap = latestByVehicle.ToDictionary(p => p.VehicleId);

        var now = DateTime.UtcNow;
        return vehicles
            .Select(v => TrackingMapper.ToLiveDto(v, latestMap.GetValueOrDefault(v.Id), _opts, now))
            .ToList();
    }

    [HttpGet("vehicle/{vehicleId:int}/history")]
    public async Task<ActionResult<List<VehiclePositionDto>>> GetHistory(
        int vehicleId, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        if (!await _db.Vehicles.AnyAsync(v => v.Id == vehicleId)) return NotFound();

        var fromUtc = DateTime.SpecifyKind(from ?? DateTime.UtcNow.AddHours(-24), DateTimeKind.Utc);
        var toUtc = DateTime.SpecifyKind(to ?? DateTime.UtcNow, DateTimeKind.Utc);

        var points = await _db.VehiclePositions.AsNoTracking()
            .Where(p => p.VehicleId == vehicleId && p.DeviceTimeUtc >= fromUtc && p.DeviceTimeUtc <= toUtc)
            .OrderBy(p => p.DeviceTimeUtc).ThenBy(p => p.Id)
            .ToListAsync();

        return points.Select(TrackingMapper.ToDto).ToList();
    }

    [HttpGet("vehicle/{vehicleId:int}/latest")]
    public async Task<ActionResult<VehiclePositionDto>> GetLatest(int vehicleId)
    {
        if (!await _db.Vehicles.AnyAsync(v => v.Id == vehicleId)) return NotFound();

        var latest = await _db.VehiclePositions.AsNoTracking()
            .Where(p => p.VehicleId == vehicleId)
            .OrderByDescending(p => p.DeviceTimeUtc).ThenByDescending(p => p.Id)
            .FirstOrDefaultAsync();

        if (latest is null) return NoContent();
        return TrackingMapper.ToDto(latest);
    }

    // Positions recorded during a trip's date range, for replay.
    [HttpGet("trip/{tripId:int}/path")]
    public async Task<ActionResult<TripPathDto>> GetTripPath(int tripId)
    {
        var trip = await _db.Trips.AsNoTracking().Include(t => t.Vehicle).FirstOrDefaultAsync(t => t.Id == tripId);
        if (trip is null) return NotFound();

        var fromUtc = trip.StartDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endDate = trip.EndDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var toUtc = endDate.ToDateTime(new TimeOnly(23, 59, 59), DateTimeKind.Utc);

        var points = await _db.VehiclePositions.AsNoTracking()
            .Where(p => p.VehicleId == trip.VehicleId && p.DeviceTimeUtc >= fromUtc && p.DeviceTimeUtc <= toUtc)
            .OrderBy(p => p.DeviceTimeUtc).ThenBy(p => p.Id)
            .ToListAsync();

        return new TripPathDto
        {
            TripId = trip.Id,
            TripCode = trip.TripCode,
            VehicleCode = trip.Vehicle?.VehicleCode ?? string.Empty,
            FromUtc = fromUtc,
            ToUtc = toUtc,
            Points = points.Select(TrackingMapper.ToDto).ToList()
        };
    }

    [HttpGet("geofence-events")]
    public async Task<ActionResult<List<GeofenceEventDto>>> GetGeofenceEvents(
        [FromQuery] int? vehicleId, [FromQuery] int? geofenceId, [FromQuery] int take = 100)
    {
        var query = _db.GeofenceEvents.AsNoTracking()
            .Include(e => e.Geofence)
            .Include(e => e.Vehicle)
            .AsQueryable();

        if (vehicleId.HasValue) query = query.Where(e => e.VehicleId == vehicleId.Value);
        if (geofenceId.HasValue) query = query.Where(e => e.GeofenceId == geofenceId.Value);

        var events = await query
            .OrderByDescending(e => e.OccurredAtUtc).ThenByDescending(e => e.Id)
            .Take(Math.Clamp(take, 1, 500))
            .ToListAsync();

        return events.Select(TrackingMapper.ToEventDto).ToList();
    }
}
