using System.ComponentModel.DataAnnotations;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Dtos;

// ----- Ingest (webhook) -----

public class PositionReportDto
{
    [Required]
    public int VehicleId { get; set; }

    [Range(-90, 90)]
    public decimal Latitude { get; set; }

    [Range(-180, 180)]
    public decimal Longitude { get; set; }

    public decimal? SpeedKph { get; set; }
    public decimal? Heading { get; set; }
    public bool? Ignition { get; set; }

    // Provider's fix timestamp (UTC). Defaults to "now" when omitted.
    public DateTime? DeviceTimeUtc { get; set; }
    public string? Source { get; set; }
}

public class IngestRequestDto
{
    [Required]
    public List<PositionReportDto> Reports { get; set; } = new();
}

public class IngestResultDto
{
    public int Accepted { get; set; }
    public int Rejected { get; set; }
    public int GeofenceEventsRaised { get; set; }
    public List<string> Errors { get; set; } = new();
}

// ----- Live view -----

public class LiveVehicleDto
{
    public int VehicleId { get; set; }
    public string VehicleCode { get; set; } = string.Empty;
    public string RegistrationNumber { get; set; } = string.Empty;
    public OperationalStatus CurrentStatus { get; set; }

    public bool HasPosition { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public decimal? SpeedKph { get; set; }
    public decimal? Heading { get; set; }
    public bool? Ignition { get; set; }
    public DateTime? DeviceTimeUtc { get; set; }
    public double? MinutesSinceReport { get; set; }
    public VehicleMovementState MovementState { get; set; }
}

public class VehiclePositionDto
{
    public long Id { get; set; }
    public int VehicleId { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public decimal? SpeedKph { get; set; }
    public decimal? Heading { get; set; }
    public bool? Ignition { get; set; }
    public DateTime DeviceTimeUtc { get; set; }
    public string? Source { get; set; }
}

public class TripPathDto
{
    public int TripId { get; set; }
    public string TripCode { get; set; } = string.Empty;
    public string VehicleCode { get; set; } = string.Empty;
    public DateTime FromUtc { get; set; }
    public DateTime ToUtc { get; set; }
    public List<VehiclePositionDto> Points { get; set; } = new();
}

// ----- Geofences -----

public class GeofenceListItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public GeofenceShape Shape { get; set; }
    public bool IsActive { get; set; }
    public decimal? CenterLat { get; set; }
    public decimal? CenterLng { get; set; }
    public decimal? RadiusMeters { get; set; }
    public int PointCount { get; set; }
}

public class GeofenceDetailDto : GeofenceUpsertDto
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class GeofenceUpsertDto
{
    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    public GeofenceShape Shape { get; set; } = GeofenceShape.Circle;

    // Circle
    [Range(-90, 90)]
    public decimal? CenterLat { get; set; }
    [Range(-180, 180)]
    public decimal? CenterLng { get; set; }
    [Range(1, 1000000)]
    public decimal? RadiusMeters { get; set; }

    // Polygon: JSON array of { "lat": .., "lng": .. }
    public string? PolygonJson { get; set; }

    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
}

public class GeofenceEventDto
{
    public long Id { get; set; }
    public int GeofenceId { get; set; }
    public string GeofenceName { get; set; } = string.Empty;
    public int VehicleId { get; set; }
    public string VehicleCode { get; set; } = string.Empty;
    public string RegistrationNumber { get; set; } = string.Empty;
    public GeofenceEventType EventType { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
}

// Shape used inside PolygonJson.
public class GeoPoint
{
    public decimal Lat { get; set; }
    public decimal Lng { get; set; }
}
