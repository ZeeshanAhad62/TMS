using TransportationSystemApi.Dtos;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Mapping;

public static class InvoiceMapper
{
    public static decimal SubTotal(Invoice inv) =>
        decimal.Round(inv.Lines.Sum(l => l.Quantity * l.UnitPrice), 2);

    public static decimal TaxAmount(Invoice inv) =>
        decimal.Round(SubTotal(inv) * (inv.TaxPercent / 100m), 2);

    public static decimal Total(Invoice inv) => SubTotal(inv) + TaxAmount(inv);

    public static decimal AmountPaid(Invoice inv) =>
        decimal.Round(inv.Payments.Sum(p => p.Amount), 2);

    public static decimal Balance(Invoice inv) => Total(inv) - AmountPaid(inv);

    // Draft / Sent / Cancelled are the user's choice; Paid / PartiallyPaid
    // are derived from how much has been received.
    public static InvoiceStatus EffectiveStatus(Invoice inv)
    {
        if (inv.Status == InvoiceStatus.Cancelled) return InvoiceStatus.Cancelled;

        var total = Total(inv);
        var paid = AmountPaid(inv);

        if (total > 0 && paid >= total) return InvoiceStatus.Paid;
        if (paid > 0) return InvoiceStatus.PartiallyPaid;

        return inv.Status == InvoiceStatus.Sent ? InvoiceStatus.Sent : InvoiceStatus.Draft;
    }

    public static bool IsOverdue(Invoice inv)
    {
        var effective = EffectiveStatus(inv);
        if (effective is InvoiceStatus.Paid or InvoiceStatus.Cancelled) return false;
        return Balance(inv) > 0 && inv.DueDate < DateOnly.FromDateTime(DateTime.UtcNow);
    }

    public static InvoiceListItemDto ToListItemDto(Invoice inv) => new()
    {
        Id = inv.Id,
        InvoiceNumber = inv.InvoiceNumber,
        CustomerName = inv.Customer?.Name ?? string.Empty,
        InvoiceDate = inv.InvoiceDate,
        DueDate = inv.DueDate,
        Status = EffectiveStatus(inv),
        Total = Total(inv),
        AmountPaid = AmountPaid(inv),
        Balance = Balance(inv),
        IsOverdue = IsOverdue(inv)
    };

    public static InvoiceDetailDto ToDetailDto(Invoice inv) => new()
    {
        Id = inv.Id,
        InvoiceNumber = inv.InvoiceNumber,
        CreatedAt = inv.CreatedAt,
        UpdatedAt = inv.UpdatedAt,
        CustomerName = inv.Customer?.Name ?? string.Empty,
        CustomerId = inv.CustomerId,
        InvoiceDate = inv.InvoiceDate,
        DueDate = inv.DueDate,
        Status = inv.Status,
        TaxPercent = inv.TaxPercent,
        Notes = inv.Notes,
        SubTotal = SubTotal(inv),
        TaxAmount = TaxAmount(inv),
        Total = Total(inv),
        AmountPaid = AmountPaid(inv),
        Balance = Balance(inv),
        EffectiveStatus = EffectiveStatus(inv),
        IsOverdue = IsOverdue(inv),
        Lines = inv.Lines.OrderBy(l => l.Id).Select(ToLineDto).ToList(),
        Payments = inv.Payments.OrderByDescending(p => p.Date).ThenBy(p => p.Id).Select(ToPaymentDto).ToList()
    };

    public static void ApplyUpsert(Invoice inv, InvoiceUpsertDto dto)
    {
        inv.CustomerId = dto.CustomerId;
        inv.InvoiceDate = dto.InvoiceDate;
        inv.DueDate = dto.DueDate;
        // Only persist a user-settable intent.
        inv.Status = dto.Status switch
        {
            InvoiceStatus.Sent => InvoiceStatus.Sent,
            InvoiceStatus.Cancelled => InvoiceStatus.Cancelled,
            _ => InvoiceStatus.Draft
        };
        inv.TaxPercent = dto.TaxPercent;
        inv.Notes = dto.Notes;
    }

    public static InvoiceLineDto ToLineDto(InvoiceLine l) => new()
    {
        Id = l.Id,
        TripId = l.TripId,
        TripCode = l.TripId is null ? null : $"TRP-{l.TripId:D5}",
        Description = l.Description,
        Quantity = l.Quantity,
        UnitPrice = l.UnitPrice,
        LineTotal = decimal.Round(l.Quantity * l.UnitPrice, 2)
    };

    public static void ApplyUpsert(InvoiceLine l, InvoiceLineUpsertDto dto)
    {
        l.TripId = dto.TripId;
        l.Description = dto.Description;
        l.Quantity = dto.Quantity;
        l.UnitPrice = dto.UnitPrice;
    }

    public static PaymentDto ToPaymentDto(Payment p) => new()
    {
        Id = p.Id,
        Date = p.Date,
        Amount = p.Amount,
        Mode = p.Mode,
        Reference = p.Reference,
        Notes = p.Notes
    };

    public static void ApplyUpsert(Payment p, PaymentUpsertDto dto)
    {
        p.Date = dto.Date;
        p.Amount = dto.Amount;
        p.Mode = dto.Mode;
        p.Reference = dto.Reference;
        p.Notes = dto.Notes;
    }
}
