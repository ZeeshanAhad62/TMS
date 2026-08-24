using TransportationSystemApi.Dtos;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Mapping;

public static class VehicleMapper
{
    private const int ExpiringSoonThresholdDays = 30;

    public static VehicleListItemDto ToListItemDto(Vehicle v)
    {
        var expiries = GetDocumentExpiries(v).Where(d => d.ExpiryDate.HasValue).ToList();
        var nearest = expiries.OrderBy(d => d.ExpiryDate).FirstOrDefault();

        return new VehicleListItemDto
        {
            Id = v.Id,
            VehicleCode = v.VehicleCode,
            RegistrationNumber = v.RegistrationNumber,
            VehicleType = v.VehicleType,
            Make = v.Make,
            Model = v.Model,
            CurrentStatus = v.CurrentStatus,
            CurrentLocation = v.CurrentLocation,
            NearestDocumentExpiry = nearest?.ExpiryDate,
            HasExpiringDocument = expiries.Any(d => IsExpiringOrExpired(d.ExpiryDate!.Value))
        };
    }

    public static VehicleDetailDto ToDetailDto(Vehicle v)
    {
        var dto = new VehicleDetailDto
        {
            Id = v.Id,
            VehicleCode = v.VehicleCode,
            CreatedAt = v.CreatedAt,
            UpdatedAt = v.UpdatedAt,
            RegistrationNumber = v.RegistrationNumber,
            VehicleType = v.VehicleType,
            Make = v.Make,
            Model = v.Model,
            Variant = v.Variant,
            YearOfManufacture = v.YearOfManufacture,
            OwnershipType = v.OwnershipType,
            FuelType = v.FuelType,
            LoadCapacity = v.LoadCapacity,
            LoadCapacityUnit = v.LoadCapacityUnit,
            ChassisNumber = v.ChassisNumber,
            EngineNumber = v.EngineNumber,
            BodyType = v.BodyType,
            AxleCount = v.AxleCount,
            TrailerType = v.TrailerType,
            ContainerLiftCapacity = v.ContainerLiftCapacity,
            SeatingCapacity = v.SeatingCapacity,
            RCNumber = v.RCNumber,
            RCExpiryDate = v.RCExpiryDate,
            FitnessCertificateNo = v.FitnessCertificateNo,
            FitnessExpiryDate = v.FitnessExpiryDate,
            RoutePermitNo = v.RoutePermitNo,
            PermitExpiryDate = v.PermitExpiryDate,
            InsurancePolicyNo = v.InsurancePolicyNo,
            InsuranceProvider = v.InsuranceProvider,
            InsuranceExpiryDate = v.InsuranceExpiryDate,
            PollutionCertNo = v.PollutionCertNo,
            PollutionCertExpiryDate = v.PollutionCertExpiryDate,
            TaxPaidTill = v.TaxPaidTill,
            CurrentStatus = v.CurrentStatus,
            CurrentLocation = v.CurrentLocation,
            AssignedDriver = v.AssignedDriver,
            CurrentBookingReference = v.CurrentBookingReference,
            CurrentOdometerReading = v.CurrentOdometerReading,
            FuelConsumptionAverage = v.FuelConsumptionAverage,
            LastOilChangeDate = v.LastOilChangeDate,
            LastOilChangeOdometer = v.LastOilChangeOdometer,
            NextOilChangeDueDate = v.NextOilChangeDueDate,
            NextOilChangeDueOdometer = v.NextOilChangeDueOdometer,
            LastServiceDate = v.LastServiceDate,
            ServiceIntervalKm = v.ServiceIntervalKm,
            ServiceIntervalMonths = v.ServiceIntervalMonths,
            BatteryReplacementDate = v.BatteryReplacementDate,
            NumberOfTyres = v.NumberOfTyres,
            PurchasePrice = v.PurchasePrice,
            DepreciationInfo = v.DepreciationInfo,
            RunningCostPerKm = v.RunningCostPerKm,
            FuelCostTracking = v.FuelCostTracking,

            Documents = v.Documents.Select(ToDto).ToList(),
            AlertRules = v.AlertRules.Select(ToDto).ToList(),
            Tyres = v.Tyres.Select(ToDto).ToList(),
            MaintenanceRecords = v.MaintenanceRecords.Select(ToDto).ToList(),
            Trips = v.Trips.OrderByDescending(t => t.StartDate).Select(TripMapper.ToListItemDto).ToList(),
        };

        dto.TotalTripsCompleted = v.Trips.Count(t => t.Status == TripStatus.Completed);
        var activeOrCompleted = v.Trips.Count(t => t.Status is TripStatus.Completed or TripStatus.Active);
        dto.UtilizationPercent = v.Trips.Count == 0
            ? 0
            : Math.Round(100.0 * activeOrCompleted / v.Trips.Count, 1);
        dto.UpcomingTrips = v.Trips
            .Where(t => t.Status == TripStatus.Scheduled)
            .OrderBy(t => t.StartDate)
            .Select(TripMapper.ToListItemDto)
            .ToList();

        dto.DocumentExpiryStatuses = GetDocumentExpiries(v);

        return dto;
    }

    public static void ApplyUpsert(Vehicle v, VehicleUpsertDto dto)
    {
        v.RegistrationNumber = dto.RegistrationNumber;
        v.VehicleType = dto.VehicleType;
        v.Make = dto.Make;
        v.Model = dto.Model;
        v.Variant = dto.Variant;
        v.YearOfManufacture = dto.YearOfManufacture;
        v.OwnershipType = dto.OwnershipType;
        v.FuelType = dto.FuelType;
        v.LoadCapacity = dto.LoadCapacity;
        v.LoadCapacityUnit = dto.LoadCapacityUnit;

        v.ChassisNumber = dto.ChassisNumber;
        v.EngineNumber = dto.EngineNumber;
        v.BodyType = dto.BodyType;
        v.AxleCount = dto.AxleCount;
        v.TrailerType = dto.TrailerType;
        v.ContainerLiftCapacity = dto.ContainerLiftCapacity;
        v.SeatingCapacity = dto.SeatingCapacity;

        v.RCNumber = dto.RCNumber;
        v.RCExpiryDate = dto.RCExpiryDate;
        v.FitnessCertificateNo = dto.FitnessCertificateNo;
        v.FitnessExpiryDate = dto.FitnessExpiryDate;
        v.RoutePermitNo = dto.RoutePermitNo;
        v.PermitExpiryDate = dto.PermitExpiryDate;
        v.InsurancePolicyNo = dto.InsurancePolicyNo;
        v.InsuranceProvider = dto.InsuranceProvider;
        v.InsuranceExpiryDate = dto.InsuranceExpiryDate;
        v.PollutionCertNo = dto.PollutionCertNo;
        v.PollutionCertExpiryDate = dto.PollutionCertExpiryDate;
        v.TaxPaidTill = dto.TaxPaidTill;

        v.CurrentStatus = dto.CurrentStatus;
        v.CurrentLocation = dto.CurrentLocation;
        v.AssignedDriver = dto.AssignedDriver;
        v.CurrentBookingReference = dto.CurrentBookingReference;
        v.CurrentOdometerReading = dto.CurrentOdometerReading;
        v.FuelConsumptionAverage = dto.FuelConsumptionAverage;

        v.LastOilChangeDate = dto.LastOilChangeDate;
        v.LastOilChangeOdometer = dto.LastOilChangeOdometer;
        v.NextOilChangeDueDate = dto.NextOilChangeDueDate;
        v.NextOilChangeDueOdometer = dto.NextOilChangeDueOdometer;
        v.LastServiceDate = dto.LastServiceDate;
        v.ServiceIntervalKm = dto.ServiceIntervalKm;
        v.ServiceIntervalMonths = dto.ServiceIntervalMonths;
        v.BatteryReplacementDate = dto.BatteryReplacementDate;

        v.NumberOfTyres = dto.NumberOfTyres;

        v.PurchasePrice = dto.PurchasePrice;
        v.DepreciationInfo = dto.DepreciationInfo;
        v.RunningCostPerKm = dto.RunningCostPerKm;
        v.FuelCostTracking = dto.FuelCostTracking;
    }

    public static VehicleDocumentDto ToDto(VehicleDocument d) => new()
    {
        Id = d.Id,
        Category = d.Category,
        FileName = d.FileName,
        ContentType = d.ContentType,
        FileSizeBytes = d.FileSizeBytes,
        UploadedAt = d.UploadedAt,
        DownloadUrl = $"/api/vehicles/{d.VehicleId}/documents/{d.Id}/download"
    };

    public static AlertRuleDto ToDto(AlertRule a) => new()
    {
        Id = a.Id,
        DocumentCategory = a.DocumentCategory,
        ThresholdDays = a.ThresholdDays,
        Channel = a.Channel,
        RecipientRole = a.RecipientRole,
        Status = a.Status
    };

    public static TyreDto ToDto(Tyre t) => new()
    {
        Id = t.Id,
        Position = t.Position,
        BrandAndSize = t.BrandAndSize,
        InstallationDate = t.InstallationDate,
        InstallationOdometer = t.InstallationOdometer,
        CurrentCondition = t.CurrentCondition,
        LastRotationDate = t.LastRotationDate,
        ReplacementHistory = t.ReplacementHistory
            .OrderByDescending(r => r.ReplacedDate)
            .Select(ToDto)
            .ToList()
    };

    public static TyreReplacementHistoryDto ToDto(TyreReplacementHistory r) => new()
    {
        Id = r.Id,
        ReplacedDate = r.ReplacedDate,
        OdometerAtReplacement = r.OdometerAtReplacement,
        OldBrandAndSize = r.OldBrandAndSize,
        NewBrandAndSize = r.NewBrandAndSize,
        Reason = r.Reason
    };

    public static MaintenanceRecordDto ToDto(MaintenanceRecord m) => new()
    {
        Id = m.Id,
        Type = m.Type,
        Date = m.Date,
        Odometer = m.Odometer,
        Description = m.Description,
        ServiceVendor = m.ServiceVendor,
        Cost = m.Cost
    };

    private static bool IsExpiringOrExpired(DateOnly expiry) =>
        expiry <= DateOnly.FromDateTime(DateTime.UtcNow).AddDays(ExpiringSoonThresholdDays);

    public static List<DocumentExpiryStatusDto> GetDocumentExpiries(Vehicle v)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var pairs = new (DocumentCategory Category, DateOnly? Expiry)[]
        {
            (DocumentCategory.RegistrationCertificate, v.RCExpiryDate),
            (DocumentCategory.FitnessCertificate, v.FitnessExpiryDate),
            (DocumentCategory.RoutePermit, v.PermitExpiryDate),
            (DocumentCategory.Insurance, v.InsuranceExpiryDate),
            (DocumentCategory.PollutionCertificate, v.PollutionCertExpiryDate),
            (DocumentCategory.RoadTax, v.TaxPaidTill),
        };

        return pairs.Select(p =>
        {
            int? days = p.Expiry.HasValue ? p.Expiry.Value.DayNumber - today.DayNumber : null;
            return new DocumentExpiryStatusDto
            {
                Category = p.Category,
                ExpiryDate = p.Expiry,
                DaysRemaining = days,
                IsExpired = days.HasValue && days.Value < 0,
                IsExpiringSoon = days.HasValue && days.Value >= 0 && days.Value <= ExpiringSoonThresholdDays
            };
        }).ToList();
    }
}
