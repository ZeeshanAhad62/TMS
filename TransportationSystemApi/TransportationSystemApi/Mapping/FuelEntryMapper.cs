using TransportationSystemApi.Dtos;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Mapping;

public static class FuelEntryMapper
{
    // prevOdometer: the odometer reading of the immediately preceding entry for
    // the same vehicle (null if this is the first known fill). Mileage/cost-per-km
    // are only meaningful full-tank to full-tank, so they are reported only when
    // both this entry and the previous one were full-tank fills.
    public static FuelEntryListItemDto ToListItemDto(FuelEntry e, decimal? prevOdometer, bool prevWasFull)
    {
        var (distance, mileage, costPerKm) = Derive(e, prevOdometer, prevWasFull);
        return new FuelEntryListItemDto
        {
            Id = e.Id,
            FuelEntryCode = e.FuelEntryCode,
            VehicleCode = e.Vehicle?.VehicleCode ?? string.Empty,
            VehicleRegistrationNumber = e.Vehicle?.RegistrationNumber ?? string.Empty,
            DriverName = e.Driver?.FullName,
            Date = e.Date,
            OdometerReading = e.OdometerReading,
            Litres = e.Litres,
            RatePerLitre = e.RatePerLitre,
            TotalCost = e.TotalCost,
            FuelType = e.FuelType,
            PaymentMode = e.PaymentMode,
            IsTankFull = e.IsTankFull,
            StationName = e.StationName,
            DistanceSinceLast = distance,
            Mileage = mileage,
            CostPerKm = costPerKm
        };
    }

    public static FuelEntryDetailDto ToDetailDto(FuelEntry e, decimal? prevOdometer, bool prevWasFull)
    {
        var (distance, mileage, costPerKm) = Derive(e, prevOdometer, prevWasFull);
        return new FuelEntryDetailDto
        {
            Id = e.Id,
            FuelEntryCode = e.FuelEntryCode,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt,
            VehicleCode = e.Vehicle?.VehicleCode ?? string.Empty,
            VehicleRegistrationNumber = e.Vehicle?.RegistrationNumber ?? string.Empty,
            DriverName = e.Driver?.FullName,
            TripCode = e.Trip?.TripCode,
            VehicleId = e.VehicleId,
            DriverId = e.DriverId,
            TripId = e.TripId,
            Date = e.Date,
            OdometerReading = e.OdometerReading,
            Litres = e.Litres,
            RatePerLitre = e.RatePerLitre,
            TotalCost = e.TotalCost,
            FuelType = e.FuelType,
            PaymentMode = e.PaymentMode,
            StationName = e.StationName,
            SlipNumber = e.SlipNumber,
            IsTankFull = e.IsTankFull,
            Notes = e.Notes,
            DistanceSinceLast = distance,
            Mileage = mileage,
            CostPerKm = costPerKm
        };
    }

    public static void ApplyUpsert(FuelEntry e, FuelEntryUpsertDto dto)
    {
        e.VehicleId = dto.VehicleId;
        e.DriverId = dto.DriverId;
        e.TripId = dto.TripId;
        e.Date = dto.Date;
        e.OdometerReading = dto.OdometerReading;
        e.Litres = dto.Litres;
        e.RatePerLitre = dto.RatePerLitre;
        e.TotalCost = decimal.Round(dto.Litres * dto.RatePerLitre, 2);
        e.FuelType = dto.FuelType;
        e.PaymentMode = dto.PaymentMode;
        e.StationName = dto.StationName;
        e.SlipNumber = dto.SlipNumber;
        e.IsTankFull = dto.IsTankFull;
        e.Notes = dto.Notes;
    }

    private static (decimal? distance, decimal? mileage, decimal? costPerKm) Derive(
        FuelEntry e, decimal? prevOdometer, bool prevWasFull)
    {
        if (prevOdometer is null) return (null, null, null);

        var distance = e.OdometerReading - prevOdometer.Value;
        if (distance <= 0) return (null, null, null);

        decimal? mileage = null;
        decimal? costPerKm = null;
        if (e.IsTankFull && prevWasFull && e.Litres > 0)
        {
            mileage = decimal.Round(distance / e.Litres, 2);
            costPerKm = distance > 0 ? decimal.Round(e.TotalCost / distance, 2) : null;
        }

        return (distance, mileage, costPerKm);
    }
}
