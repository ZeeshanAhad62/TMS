using TransportationSystemApi.Dtos;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Mapping;

public static class RouteRateMapper
{
    // ----- Routes -----

    public static string Label(RouteMaster r) =>
        string.IsNullOrWhiteSpace(r.Name) ? $"{r.Origin} - {r.Destination}" : r.Name!;

    public static RouteListItemDto ToListItemDto(RouteMaster r) => new()
    {
        Id = r.Id,
        RouteCode = r.RouteCode,
        Name = r.Name,
        Origin = r.Origin,
        Destination = r.Destination,
        DistanceKm = r.DistanceKm,
        StandardRate = r.StandardRate,
        IsActive = r.IsActive,
        ActiveContractCount = r.RateContracts.Count(c => c.IsActive)
    };

    public static RouteDetailDto ToDetailDto(RouteMaster r, DateOnly today) => new()
    {
        Id = r.Id,
        RouteCode = r.RouteCode,
        Name = r.Name,
        Origin = r.Origin,
        Destination = r.Destination,
        DistanceKm = r.DistanceKm,
        EstimatedDurationHours = r.EstimatedDurationHours,
        StandardRate = r.StandardRate,
        IsActive = r.IsActive,
        Notes = r.Notes,
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt,
        Contracts = r.RateContracts
            .OrderByDescending(c => c.ValidFrom).ThenByDescending(c => c.Id)
            .Select(c => ToListItemDto(c, today))
            .ToList()
    };

    public static void ApplyUpsert(RouteMaster r, RouteUpsertDto dto)
    {
        r.Name = string.IsNullOrWhiteSpace(dto.Name) ? null : dto.Name!.Trim();
        r.Origin = dto.Origin.Trim();
        r.Destination = dto.Destination.Trim();
        r.DistanceKm = dto.DistanceKm;
        r.EstimatedDurationHours = dto.EstimatedDurationHours;
        r.StandardRate = dto.StandardRate;
        r.IsActive = dto.IsActive;
        r.Notes = dto.Notes;
    }

    // ----- Rate contracts -----

    public static bool IsValidOn(RateContract c, DateOnly on) =>
        c.IsActive && c.ValidFrom <= on && (c.ValidTo is null || c.ValidTo >= on);

    public static RateContractListItemDto ToListItemDto(RateContract c, DateOnly today) => new()
    {
        Id = c.Id,
        ContractCode = c.ContractCode,
        CustomerId = c.CustomerId,
        CustomerName = c.Customer?.Name ?? string.Empty,
        RouteId = c.RouteId,
        RouteCode = c.Route?.RouteCode ?? string.Empty,
        RouteLabel = c.Route is null ? string.Empty : Label(c.Route),
        VehicleType = c.VehicleType,
        Rate = c.Rate,
        RateBasis = c.RateBasis,
        ValidFrom = c.ValidFrom,
        ValidTo = c.ValidTo,
        IsActive = c.IsActive,
        IsCurrentlyValid = IsValidOn(c, today)
    };

    public static RateContractDetailDto ToDetailDto(RateContract c, DateOnly today) => new()
    {
        Id = c.Id,
        ContractCode = c.ContractCode,
        CustomerId = c.CustomerId,
        CustomerName = c.Customer?.Name ?? string.Empty,
        RouteId = c.RouteId,
        RouteCode = c.Route?.RouteCode ?? string.Empty,
        RouteLabel = c.Route is null ? string.Empty : Label(c.Route),
        VehicleType = c.VehicleType,
        Rate = c.Rate,
        RateBasis = c.RateBasis,
        ValidFrom = c.ValidFrom,
        ValidTo = c.ValidTo,
        IsActive = c.IsActive,
        IsCurrentlyValid = IsValidOn(c, today),
        Notes = c.Notes,
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt
    };

    public static void ApplyUpsert(RateContract c, RateContractUpsertDto dto)
    {
        c.CustomerId = dto.CustomerId;
        c.RouteId = dto.RouteId;
        c.VehicleType = dto.VehicleType;
        c.Rate = dto.Rate;
        c.RateBasis = dto.RateBasis;
        c.ValidFrom = dto.ValidFrom;
        c.ValidTo = dto.ValidTo;
        c.IsActive = dto.IsActive;
        c.Notes = dto.Notes;
    }

    // ----- Quote resolution -----
    //
    // Given a route, the candidate contracts for it, and the trip context,
    // pick the best rate: a vehicle-type-specific contract beats an "any type"
    // one; among equals the latest ValidFrom wins; failing any contract, fall
    // back to the route's StandardRate.
    public static RateQuoteDto ResolveQuote(
        RouteMaster route,
        IEnumerable<RateContract> routeContracts,
        int? customerId,
        VehicleType? vehicleType,
        DateOnly onDate)
    {
        var quote = new RateQuoteDto
        {
            DistanceKm = route.DistanceKm,
            RouteLabel = Label(route),
            Origin = route.Origin,
            Destination = route.Destination
        };

        RateContract? best = null;
        if (customerId is int cid)
        {
            best = routeContracts
                .Where(c => c.CustomerId == cid
                            && IsValidOn(c, onDate)
                            && (c.VehicleType is null || c.VehicleType == vehicleType))
                .OrderByDescending(c => c.VehicleType == vehicleType && vehicleType is not null) // specific first
                .ThenByDescending(c => c.ValidFrom)
                .ThenByDescending(c => c.Id)
                .FirstOrDefault();
        }

        if (best is not null)
        {
            quote.Source = RateQuoteSource.Contract;
            quote.Rate = best.Rate;
            quote.RateBasis = best.RateBasis;
            quote.ContractCode = best.ContractCode;
            quote.Amount = Amount(best.Rate, best.RateBasis, route.DistanceKm);
            return quote;
        }

        if (route.StandardRate is decimal std)
        {
            quote.Source = RateQuoteSource.RouteStandard;
            quote.Rate = std;
            quote.RateBasis = RateBasis.PerTrip;
            quote.Amount = std;
            return quote;
        }

        quote.Source = RateQuoteSource.None;
        return quote;
    }

    private static decimal? Amount(decimal rate, RateBasis basis, decimal? distanceKm) => basis switch
    {
        RateBasis.PerKm => distanceKm is decimal km ? Math.Round(rate * km, 2) : null,
        _ => rate
    };
}
