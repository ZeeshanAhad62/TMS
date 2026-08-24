using System.ComponentModel.DataAnnotations;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Dtos;

public class VehicleListItemDto
{
    public int Id { get; set; }
    public string VehicleCode { get; set; } = string.Empty;
    public string RegistrationNumber { get; set; } = string.Empty;
    public VehicleType VehicleType { get; set; }
    public string? Make { get; set; }
    public string? Model { get; set; }
    public OperationalStatus CurrentStatus { get; set; }
    public string? CurrentLocation { get; set; }
    public DateOnly? NearestDocumentExpiry { get; set; }
    public bool HasExpiringDocument { get; set; }
}

public class VehicleDetailDto : VehicleUpsertDto
{
    public int Id { get; set; }
    public string VehicleCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public List<VehicleDocumentDto> Documents { get; set; } = new();
    public List<AlertRuleDto> AlertRules { get; set; } = new();
    public List<TyreDto> Tyres { get; set; } = new();
    public List<MaintenanceRecordDto> MaintenanceRecords { get; set; } = new();
    public List<TripListItemDto> Trips { get; set; } = new();

    // 3.7 Booking & Utilization - system-calculated
    public int TotalTripsCompleted { get; set; }
    public double UtilizationPercent { get; set; }
    public List<TripListItemDto> UpcomingTrips { get; set; } = new();

    public List<DocumentExpiryStatusDto> DocumentExpiryStatuses { get; set; } = new();
}

public class VehicleUpsertDto
{
    [Required, MaxLength(50)]
    public string RegistrationNumber { get; set; } = string.Empty;

    [Required]
    public VehicleType VehicleType { get; set; }

    public string? Make { get; set; }
    public string? Model { get; set; }
    public string? Variant { get; set; }
    public int? YearOfManufacture { get; set; }

    [Required]
    public OwnershipType OwnershipType { get; set; }

    [Required]
    public FuelType FuelType { get; set; }

    public decimal? LoadCapacity { get; set; }
    public string? LoadCapacityUnit { get; set; }

    public string? ChassisNumber { get; set; }
    public string? EngineNumber { get; set; }
    public string? BodyType { get; set; }
    public int? AxleCount { get; set; }
    public string? TrailerType { get; set; }
    public decimal? ContainerLiftCapacity { get; set; }
    public int? SeatingCapacity { get; set; }

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

    public OperationalStatus CurrentStatus { get; set; } = OperationalStatus.Available;
    public string? CurrentLocation { get; set; }
    public string? AssignedDriver { get; set; }
    public string? CurrentBookingReference { get; set; }
    public decimal? CurrentOdometerReading { get; set; }
    public decimal? FuelConsumptionAverage { get; set; }

    public DateOnly? LastOilChangeDate { get; set; }
    public decimal? LastOilChangeOdometer { get; set; }
    public DateOnly? NextOilChangeDueDate { get; set; }
    public decimal? NextOilChangeDueOdometer { get; set; }
    public DateOnly? LastServiceDate { get; set; }
    public decimal? ServiceIntervalKm { get; set; }
    public int? ServiceIntervalMonths { get; set; }
    public DateOnly? BatteryReplacementDate { get; set; }

    public int? NumberOfTyres { get; set; }

    public decimal? PurchasePrice { get; set; }
    public string? DepreciationInfo { get; set; }
    public decimal? RunningCostPerKm { get; set; }
    public decimal? FuelCostTracking { get; set; }
}

public class DocumentExpiryStatusDto
{
    public DocumentCategory Category { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public int? DaysRemaining { get; set; }
    public bool IsExpired { get; set; }
    public bool IsExpiringSoon { get; set; }
}
