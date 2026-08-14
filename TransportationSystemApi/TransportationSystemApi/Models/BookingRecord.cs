namespace TransportationSystemApi.Models;

// Stub for the future Bookings module. Fleet Master references this to show
// booking history, total trips, utilization %, and upcoming bookings.
public class BookingRecord
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }

    public string TripReference { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Scheduled;
    public string? Notes { get; set; }
}
