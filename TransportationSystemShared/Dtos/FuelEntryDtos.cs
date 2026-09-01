using System.ComponentModel.DataAnnotations;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Dtos;

public class FuelEntryListItemDto
{
    public int Id { get; set; }
    public string FuelEntryCode { get; set; } = string.Empty;
    public string VehicleCode { get; set; } = string.Empty;
    public string VehicleRegistrationNumber { get; set; } = string.Empty;
    public string? DriverName { get; set; }
    public DateOnly Date { get; set; }
    public decimal OdometerReading { get; set; }
    public decimal Litres { get; set; }
    public decimal RatePerLitre { get; set; }
    public decimal TotalCost { get; set; }
    public FuelType FuelType { get; set; }
    public FuelPaymentMode PaymentMode { get; set; }
    public bool IsTankFull { get; set; }
    public string? StationName { get; set; }

    // Derived from the previous entry for the same vehicle.
    public decimal? DistanceSinceLast { get; set; }
    public decimal? Mileage { get; set; }      // km per litre
    public decimal? CostPerKm { get; set; }
}

public class FuelEntryDetailDto : FuelEntryUpsertDto
{
    public int Id { get; set; }
    public string FuelEntryCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public string VehicleCode { get; set; } = string.Empty;
    public string VehicleRegistrationNumber { get; set; } = string.Empty;
    public string? DriverName { get; set; }
    public string? TripCode { get; set; }

    public decimal TotalCost { get; set; }
    public decimal? DistanceSinceLast { get; set; }
    public decimal? Mileage { get; set; }
    public decimal? CostPerKm { get; set; }
}

public class FuelEntryUpsertDto : IValidatableObject
{
    [Required]
    public int VehicleId { get; set; }

    public int? DriverId { get; set; }
    public int? TripId { get; set; }

    [Required]
    public DateOnly Date { get; set; }

    [Range(0, 99999999)]
    public decimal OdometerReading { get; set; }

    [Range(0.01, 99999)]
    public decimal Litres { get; set; }

    [Range(0, 999999)]
    public decimal RatePerLitre { get; set; }

    public FuelType FuelType { get; set; } = FuelType.Diesel;
    public FuelPaymentMode PaymentMode { get; set; } = FuelPaymentMode.Cash;

    [MaxLength(150)]
    public string? StationName { get; set; }

    [MaxLength(80)]
    public string? SlipNumber { get; set; }

    public bool IsTankFull { get; set; } = true;

    public string? Notes { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Litres <= 0)
            yield return new ValidationResult("Litres must be greater than zero.", new[] { nameof(Litres) });
    }
}
