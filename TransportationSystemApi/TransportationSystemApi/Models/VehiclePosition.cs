namespace TransportationSystemApi.Models;

// One GPS fix for a vehicle. Written by the tracking webhook; the oldest rows
// per vehicle are pruned on ingest (Tracking:MaxHotPositionsPerVehicle).
public class VehiclePosition
{
    public long Id { get; set; }

    public int VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }

    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public decimal? SpeedKph { get; set; }
    public decimal? Heading { get; set; }
    public bool? Ignition { get; set; }

    public DateTime DeviceTimeUtc { get; set; }
    public string? Source { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
