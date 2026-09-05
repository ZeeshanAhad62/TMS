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

    public static decimal ExpensesTotal(Trip t) => t.Expenses.Sum(e => e.Amount);

    public static TripDetailDto ToDetailDto(Trip t, decimal fuelCost, decimal driverPay = 0m)
    {
        var revenue = t.Revenue ?? 0m;
        var expenses = ExpensesTotal(t);
        // driverPay = Σ settled pay-run line amounts tagged to this trip
        // (Payroll module). 0 until a pay run covering the trip exists.

        return new TripDetailDto
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
            Revenue = t.Revenue,
            RevenueAmount = revenue,
            FuelCost = fuelCost,
            ExpensesTotal = expenses,
            DriverPay = driverPay,
            NetProfit = revenue - fuelCost - expenses - driverPay,
            Expenses = t.Expenses
                .OrderByDescending(e => e.Date)
                .ThenBy(e => e.Id)
                .Select(ToExpenseDto)
                .ToList()
        };
    }

    public static TripExpenseDto ToExpenseDto(TripExpense e) => new()
    {
        Id = e.Id,
        Category = e.Category,
        Amount = e.Amount,
        Date = e.Date,
        PaidBy = e.PaidBy,
        ReceiptNumber = e.ReceiptNumber,
        Notes = e.Notes
    };

    public static void ApplyUpsert(TripExpense e, TripExpenseUpsertDto dto)
    {
        e.Category = dto.Category;
        e.Amount = dto.Amount;
        e.Date = dto.Date;
        e.PaidBy = dto.PaidBy;
        e.ReceiptNumber = dto.ReceiptNumber;
        e.Notes = dto.Notes;
    }

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
