namespace TransportationSystemApi.Models;

public class Trip
{
    public int Id { get; set; }

    // System-generated identity
    public string TripCode { get; set; } = string.Empty;

    public int VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }

    public int DriverId { get; set; }
    public Driver? Driver { get; set; }

    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public TripStatus Status { get; set; } = TripStatus.Scheduled;
    public string? Notes { get; set; }
    public decimal? Revenue { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
