namespace TransportationSystemApi.Models;

// A named area. Circle = CenterLat/CenterLng + RadiusMeters.
// Polygon = PolygonJson, a JSON array of { "lat": .., "lng": .. } points.
public class Geofence
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public GeofenceShape Shape { get; set; } = GeofenceShape.Circle;

    public decimal? CenterLat { get; set; }
    public decimal? CenterLng { get; set; }
    public decimal? RadiusMeters { get; set; }
    public string? PolygonJson { get; set; }

    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public List<GeofenceEvent> Events { get; set; } = new();
}
