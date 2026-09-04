using TransportationSystemApi.Dtos;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Mapping;

public static class TyreMapper
{
    public static TyreListItemDto ToListItemDto(Tyre t) => new()
    {
        Id = t.Id,
        SerialNumber = t.SerialNumber,
        BrandAndSize = t.BrandAndSize,
        Pattern = t.Pattern,
        Status = t.Status,
        VehicleId = t.VehicleId,
        VehicleRegistrationNumber = t.Vehicle?.RegistrationNumber,
        Position = t.Position,
        PurchaseCost = t.PurchaseCost,
        DistanceRun = DistanceRun(t),
        CostPerKm = CostPerKm(t),
        RetreadCount = t.Events.Count(e => e.EventType == TyreEventType.Retread)
    };

    public static TyreDetailDto ToDetailDto(Tyre t) => new()
    {
        Id = t.Id,
        SerialNumber = t.SerialNumber,
        BrandAndSize = t.BrandAndSize,
        Pattern = t.Pattern,
        Status = t.Status,
        VehicleId = t.VehicleId,
        VehicleRegistrationNumber = t.Vehicle?.RegistrationNumber,
        Position = t.Position,
        PurchaseCost = t.PurchaseCost,
        DistanceRun = DistanceRun(t),
        CostPerKm = CostPerKm(t),
        RetreadCount = t.Events.Count(e => e.EventType == TyreEventType.Retread),
        PurchaseDate = t.PurchaseDate,
        InstallationDate = t.InstallationDate,
        InstallationOdometer = t.InstallationOdometer,
        TotalDistanceRunCarried = t.TotalDistanceRunCarried,
        CurrentCondition = t.CurrentCondition,
        LastRotationDate = t.LastRotationDate,
        LastRetreadDate = t.Events
            .Where(e => e.EventType == TyreEventType.Retread)
            .OrderByDescending(e => e.EventDate)
            .Select(e => (DateOnly?)e.EventDate)
            .FirstOrDefault(),
        Events = t.Events
            .OrderByDescending(e => e.EventDate).ThenByDescending(e => e.Id)
            .Select(ToEventDto)
            .ToList()
    };

    public static TyreEventDto ToEventDto(TyreEvent e) => new()
    {
        Id = e.Id,
        EventType = e.EventType,
        EventDate = e.EventDate,
        VehicleId = e.VehicleId,
        Position = e.Position,
        Odometer = e.Odometer,
        Cost = e.Cost,
        Notes = e.Notes
    };

    public static void ApplyUpsert(Tyre t, TyreCreateDto dto)
    {
        t.SerialNumber = dto.SerialNumber;
        t.BrandAndSize = dto.BrandAndSize;
        t.Pattern = dto.Pattern;
        t.PurchaseDate = dto.PurchaseDate;
        t.PurchaseCost = dto.PurchaseCost;
        t.CurrentCondition = dto.CurrentCondition;
    }

    // Distance covered by completed stints plus, if currently fitted, the
    // distance covered since the last fit/rotate baseline.
    private static decimal DistanceRun(Tyre t)
    {
        var currentStint = t.Status == TyreStatus.Fitted
            && t.Vehicle?.CurrentOdometerReading is decimal odo
            && t.InstallationOdometer is decimal baseOdo
            ? Math.Max(0, odo - baseOdo)
            : 0;

        return t.TotalDistanceRunCarried + currentStint;
    }

    private static decimal? CostPerKm(Tyre t)
    {
        var distance = DistanceRun(t);
        if (distance <= 0) return null;

        var cost = (t.PurchaseCost ?? 0) + t.Events.Where(e => e.EventType == TyreEventType.Retread).Sum(e => e.Cost ?? 0);
        return cost <= 0 ? null : Math.Round(cost / distance, 2);
    }
}
