namespace TransportationSystemApi.Models;

public class FuelEntry
{
    public int Id { get; set; }

    // System-generated identity
    public string FuelEntryCode { get; set; } = string.Empty;

    public int VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }

    public int? DriverId { get; set; }
    public Driver? Driver { get; set; }

    public int? TripId { get; set; }
    public Trip? Trip { get; set; }

    public DateOnly Date { get; set; }
    public decimal OdometerReading { get; set; }
    public decimal Litres { get; set; }
    public decimal RatePerLitre { get; set; }

    // Always recomputed by the API as Litres * RatePerLitre.
    public decimal TotalCost { get; set; }

    public FuelType FuelType { get; set; }
    public FuelPaymentMode PaymentMode { get; set; } = FuelPaymentMode.Cash;
    public string? StationName { get; set; }
    public string? SlipNumber { get; set; }
    public bool IsTankFull { get; set; } = true;
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
