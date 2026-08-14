namespace TransportationSystemApi.Models;

public class Vehicle
{
    public int Id { get; set; }

    // System-generated identity
    public string VehicleCode { get; set; } = string.Empty;

    // 3.1 Vehicle Identity - common fields
    public string RegistrationNumber { get; set; } = string.Empty;
    public VehicleType VehicleType { get; set; }
    public string? Make { get; set; }
    public string? Model { get; set; }
    public string? Variant { get; set; }
    public int? YearOfManufacture { get; set; }
    public OwnershipType OwnershipType { get; set; }
    public FuelType FuelType { get; set; }
    public decimal? LoadCapacity { get; set; }
    public string? LoadCapacityUnit { get; set; }

    // 3.1 Vehicle Identity - type-specific fields
    public string? ChassisNumber { get; set; }          // self-propelled
    public string? EngineNumber { get; set; }            // self-propelled
    public string? BodyType { get; set; }                 // trucks/vans
    public int? AxleCount { get; set; }                   // trailers
    public string? TrailerType { get; set; }               // trailers
    public decimal? ContainerLiftCapacity { get; set; }    // container carriers
    public int? SeatingCapacity { get; set; }              // buses/vans

    // 3.2 Registration & Legal Documents
    public string? RCNumber { get; set; }
    public DateOnly? RCExpiryDate { get; set; }
    public string? FitnessCertificateNo { get; set; }
    public DateOnly? FitnessExpiryDate { get; set; }
    public string? RoutePermitNo { get; set; }
    public DateOnly? PermitExpiryDate { get; set; }
    public string? InsurancePolicyNo { get; set; }
    public string? InsuranceProvider { get; set; }
    public DateOnly? InsuranceExpiryDate { get; set; }
    public string? PollutionCertNo { get; set; }
    public DateOnly? PollutionCertExpiryDate { get; set; }
    public DateOnly? TaxPaidTill { get; set; }

    // 3.6 Operational Status
    public OperationalStatus CurrentStatus { get; set; } = OperationalStatus.Available;
    public string? CurrentLocation { get; set; }
    public string? AssignedDriver { get; set; }
    public string? CurrentBookingReference { get; set; }
    public decimal? CurrentOdometerReading { get; set; }
    public decimal? FuelConsumptionAverage { get; set; }

    // 3.5 Maintenance & Service (single-value fields; logs are child records)
    public DateOnly? LastOilChangeDate { get; set; }
    public decimal? LastOilChangeOdometer { get; set; }
    public DateOnly? NextOilChangeDueDate { get; set; }
    public decimal? NextOilChangeDueOdometer { get; set; }
    public DateOnly? LastServiceDate { get; set; }
    public decimal? ServiceIntervalKm { get; set; }
    public int? ServiceIntervalMonths { get; set; }
    public DateOnly? BatteryReplacementDate { get; set; }

    // 3.4 Tyre Information (summary fields; per-position detail is a child collection)
    public int? NumberOfTyres { get; set; }

    // 3.8 Financials (optional)
    public decimal? PurchasePrice { get; set; }
    public string? DepreciationInfo { get; set; }
    public decimal? RunningCostPerKm { get; set; }
    public decimal? FuelCostTracking { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public List<VehicleDocument> Documents { get; set; } = new();
    public List<AlertRule> AlertRules { get; set; } = new();
    public List<Tyre> Tyres { get; set; } = new();
    public List<MaintenanceRecord> MaintenanceRecords { get; set; } = new();
    public List<BookingRecord> BookingRecords { get; set; } = new();
}
