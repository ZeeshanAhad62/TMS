namespace TransportationSystemApi.Models;

// Unified log for: major repair history, brake service history, and general
// service entries. Oil-change "current" values live on Vehicle; each oil
// change event performed is also logged here for history.
public class MaintenanceRecord
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }

    public MaintenanceType Type { get; set; }
    public DateOnly Date { get; set; }
    public decimal? Odometer { get; set; }
    public string? Description { get; set; }
    public string? ServiceVendor { get; set; }
    public decimal? Cost { get; set; }
}
