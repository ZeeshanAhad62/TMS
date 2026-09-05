namespace TransportationSystemApi.Models;

// An Enter or Exit transition, emitted on ingest when a fix's inside/outside
// state for a geofence differs from the vehicle's previous state for it.
public class GeofenceEvent
{
    public long Id { get; set; }

    public int GeofenceId { get; set; }
    public Geofence? Geofence { get; set; }

    public int VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }

    public GeofenceEventType EventType { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
