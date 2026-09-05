using TransportationSystemApi.Dtos;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Mapping;

public static class PayrollMapper
{
    // ----- Advances -----

    public static decimal Outstanding(DriverAdvance a) => Math.Max(0m, a.Amount - a.RecoveredAmount);

    public static decimal AdvancesOutstanding(IEnumerable<DriverAdvance> advances) =>
        advances.Sum(Outstanding);

    public static DriverAdvanceDto ToDto(DriverAdvance a) => new()
    {
        Id = a.Id,
        DriverId = a.DriverId,
        Date = a.Date,
        Amount = a.Amount,
        RecoveredAmount = a.RecoveredAmount,
        Outstanding = Outstanding(a),
        IsFullyRecovered = a.RecoveredAmount >= a.Amount,
        Reason = a.Reason,
        Notes = a.Notes,
        CreatedAt = a.CreatedAt,
        UpdatedAt = a.UpdatedAt
    };

    public static void ApplyUpsert(DriverAdvance a, DriverAdvanceUpsertDto dto)
    {
        a.Date = dto.Date;
        a.Amount = dto.Amount;
        if (dto.RecoveredAmount.HasValue)
            a.RecoveredAmount = dto.RecoveredAmount.Value;
        a.Reason = dto.Reason;
        a.Notes = dto.Notes;
    }

    // ----- Pay runs -----

    public static decimal LinesTotal(PayRun p) => p.Lines.Sum(l => l.Amount);

    public static decimal GrossPay(PayRun p) => LinesTotal(p) + p.AllowancesTotal;

    public static decimal NetPay(PayRun p) => GrossPay(p) - p.AdvanceRecovery;

    public static PayRunListItemDto ToListItemDto(PayRun p) => new()
    {
        Id = p.Id,
        PayRunCode = p.PayRunCode,
        DriverId = p.DriverId,
        DriverName = p.Driver?.FullName ?? string.Empty,
        PeriodStart = p.PeriodStart,
        PeriodEnd = p.PeriodEnd,
        Status = p.Status,
        GrossPay = GrossPay(p),
        AdvanceRecovery = p.AdvanceRecovery,
        NetPay = NetPay(p),
        LineCount = p.Lines.Count
    };

    public static PayRunDetailDto ToDetailDto(PayRun p, decimal advancesOutstanding, IReadOnlyDictionary<int, string> tripCodes)
    {
        return new PayRunDetailDto
        {
            Id = p.Id,
            PayRunCode = p.PayRunCode,
            DriverId = p.DriverId,
            DriverName = p.Driver?.FullName ?? string.Empty,
            DriverCode = p.Driver?.DriverCode ?? string.Empty,
            DriverPayType = p.Driver?.PayType ?? DriverPayType.PerTrip,
            DriverPayRate = p.Driver?.PayRate,
            PeriodStart = p.PeriodStart,
            PeriodEnd = p.PeriodEnd,
            Status = p.Status,
            AllowancesTotal = p.AllowancesTotal,
            AdvanceRecovery = p.AdvanceRecovery,
            Notes = p.Notes,
            LinesTotal = LinesTotal(p),
            GrossPay = GrossPay(p),
            NetPay = NetPay(p),
            AdvancesOutstanding = advancesOutstanding,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt,
            Lines = p.Lines
                .OrderBy(l => l.Id)
                .Select(l => ToDto(l, l.TripId is int tid && tripCodes.TryGetValue(tid, out var code) ? code : null))
                .ToList()
        };
    }

    public static void ApplyUpsert(PayRun p, PayRunUpsertDto dto)
    {
        p.DriverId = dto.DriverId;
        p.PeriodStart = dto.PeriodStart;
        p.PeriodEnd = dto.PeriodEnd;
        p.Status = dto.Status;
        p.AllowancesTotal = dto.AllowancesTotal;
        p.AdvanceRecovery = dto.AdvanceRecovery;
        p.Notes = dto.Notes;
    }

    // ----- Pay-run lines -----

    public static PayRunLineDto ToDto(PayRunLine l, string? tripCode) => new()
    {
        Id = l.Id,
        TripId = l.TripId,
        TripCode = tripCode,
        Description = l.Description,
        Basis = l.Basis,
        Quantity = l.Quantity,
        Rate = l.Rate,
        Amount = l.Amount
    };

    public static void ApplyUpsert(PayRunLine l, PayRunLineUpsertDto dto)
    {
        l.TripId = dto.TripId;
        l.Description = dto.Description;
        l.Basis = dto.Basis;
        l.Quantity = dto.Quantity;
        l.Rate = dto.Rate;
        l.Amount = dto.Amount;
    }
}
