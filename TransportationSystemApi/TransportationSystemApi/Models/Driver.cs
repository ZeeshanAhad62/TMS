namespace TransportationSystemApi.Models;

public class Driver
{
    public int Id { get; set; }

    // System-generated identity
    public string DriverCode { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Address { get; set; }

    public string LicenseNumber { get; set; } = string.Empty;
    public string? LicenseType { get; set; }
    public DateOnly? LicenseExpiryDate { get; set; }

    public DriverStatus Status { get; set; } = DriverStatus.Active;

    // Pay configuration (Payroll module). PayRate is interpreted per PayType:
    // PerTrip = amount per completed trip, PerKm = amount per km, Monthly =
    // fixed amount for the period, Percentage = percent of trip revenue.
    public DriverPayType PayType { get; set; } = DriverPayType.PerTrip;
    public decimal? PayRate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public List<DriverDocument> Documents { get; set; } = new();
    public List<DriverVehicleAssignment> Assignments { get; set; } = new();
    public List<DriverAdvance> Advances { get; set; } = new();
}
