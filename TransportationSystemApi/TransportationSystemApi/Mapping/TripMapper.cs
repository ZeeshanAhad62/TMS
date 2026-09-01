using TransportationSystemApi.Dtos;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Mapping;

public static class TripMapper
{
    public static TripListItemDto ToListItemDto(Trip t) => new()
    {
        Id = t.Id,
        TripCode = t.TripCode,
        VehicleCode = t.Vehicle?.VehicleCode ?? string.Empty,
        VehicleRegistrationNumber = t.Vehicle?.RegistrationNumber ?? string.Empty,
        DriverName = t.Driver?.FullName ?? string.Empty,
        CustomerName = t.Customer?.Name,
        Origin = t.Origin,
        Destination = t.Destination,
        StartDate = t.StartDate,
        EndDate = t.EndDate,
        Status = t.Status
    };

    public static TripDetailDto ToDetailDto(Trip t) => new()
    {
        Id = t.Id,
        TripCode = t.TripCode,
        CreatedAt = t.CreatedAt,
        UpdatedAt = t.UpdatedAt,
        VehicleCode = t.Vehicle?.VehicleCode ?? string.Empty,
        VehicleRegistrationNumber = t.Vehicle?.RegistrationNumber ?? string.Empty,
        DriverName = t.Driver?.FullName ?? string.Empty,
        CustomerName = t.Customer?.Name,
        VehicleId = t.VehicleId,
        DriverId = t.DriverId,
        CustomerId = t.CustomerId,
        Origin = t.Origin,
        Destination = t.Destination,
        StartDate = t.StartDate,
        EndDate = t.EndDate,
        Status = t.Status,
        Notes = t.Notes,
        Revenue = t.Revenue
    };

    public static void ApplyUpsert(Trip t, TripUpsertDto dto)
    {
        t.VehicleId = dto.VehicleId;
        t.DriverId = dto.DriverId;
        t.CustomerId = dto.CustomerId;
        t.Origin = dto.Origin;
        t.Destination = dto.Destination;
        t.StartDate = dto.StartDate;
        t.EndDate = dto.EndDate;
        t.Status = dto.Status;
        t.Notes = dto.Notes;
        t.Revenue = dto.Revenue;
    }
}
