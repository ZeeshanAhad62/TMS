namespace TransportationSystemApi.Models;

// Lifecycle log for a tyre asset: fit / remove / rotate / retread / inspect / scrap.
// VehicleId is a soft snapshot of which vehicle the event happened on (or null
// for stock-side events like Retread) -- no FK, so it survives the tyre moving
// between vehicles and the vehicle itself being deleted later.
public class TyreEvent
{
    public int Id { get; set; }

    public int TyreId { get; set; }
    public Tyre? Tyre { get; set; }

    public TyreEventType EventType { get; set; }
    public DateOnly EventDate { get; set; }
    public int? VehicleId { get; set; }
    public TyrePosition? Position { get; set; }
    public decimal? Odometer { get; set; }
    public decimal? Cost { get; set; }
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
