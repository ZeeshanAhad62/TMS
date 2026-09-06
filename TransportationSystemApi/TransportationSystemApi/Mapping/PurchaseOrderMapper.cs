using TransportationSystemApi.Dtos;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Mapping;

public static class PurchaseOrderMapper
{
    public static decimal LineTotal(PurchaseOrderLine l) => l.Quantity * l.UnitPrice;

    public static decimal SubTotal(PurchaseOrder p) => p.Lines.Sum(LineTotal);

    public static decimal TaxAmount(PurchaseOrder p) => Math.Round(SubTotal(p) * p.TaxPercent / 100m, 2);

    public static decimal Total(PurchaseOrder p) => SubTotal(p) + TaxAmount(p);

    public static decimal ReceivedPercent(PurchaseOrder p)
    {
        var ordered = p.Lines.Sum(l => l.Quantity);
        if (ordered <= 0) return 0;
        var received = p.Lines.Sum(l => Math.Min(l.QuantityReceived, l.Quantity));
        return Math.Round(received / ordered * 100m, 1);
    }

    public static bool FullyReceived(PurchaseOrder p) =>
        p.Lines.Count > 0 && p.Lines.All(l => l.QuantityReceived >= l.Quantity);

    public static bool HasAnyReceipt(PurchaseOrder p) => p.Lines.Any(l => l.QuantityReceived > 0);

    // Draft / Ordered / Cancelled are user intent. Once goods start arriving the
    // status reads as PartiallyReceived, then Received when every line is full,
    // unless the user has explicitly Cancelled it.
    public static PurchaseOrderStatus EffectiveStatus(PurchaseOrder p)
    {
        if (p.Status == PurchaseOrderStatus.Cancelled) return PurchaseOrderStatus.Cancelled;
        if (FullyReceived(p)) return PurchaseOrderStatus.Received;
        if (HasAnyReceipt(p)) return PurchaseOrderStatus.PartiallyReceived;
        return p.Status;
    }

    public static PurchaseOrderListItemDto ToListItemDto(PurchaseOrder p) => new()
    {
        Id = p.Id,
        PoNumber = p.PoNumber,
        VendorId = p.VendorId,
        VendorName = p.Vendor?.Name ?? string.Empty,
        OrderDate = p.OrderDate,
        ExpectedDate = p.ExpectedDate,
        Status = p.Status,
        EffectiveStatus = EffectiveStatus(p),
        Total = Total(p),
        ReceivedPercent = ReceivedPercent(p),
        LineCount = p.Lines.Count
    };

    public static PurchaseOrderDetailDto ToDetailDto(PurchaseOrder p) => new()
    {
        Id = p.Id,
        PoNumber = p.PoNumber,
        VendorId = p.VendorId,
        VendorName = p.Vendor?.Name ?? string.Empty,
        OrderDate = p.OrderDate,
        ExpectedDate = p.ExpectedDate,
        Status = p.Status,
        EffectiveStatus = EffectiveStatus(p),
        TaxPercent = p.TaxPercent,
        Notes = p.Notes,
        SubTotal = SubTotal(p),
        TaxAmount = TaxAmount(p),
        Total = Total(p),
        ReceivedPercent = ReceivedPercent(p),
        FullyReceived = FullyReceived(p),
        HasAnyReceipt = HasAnyReceipt(p),
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt,
        Lines = p.Lines.OrderBy(l => l.Id).Select(ToLineDto).ToList()
    };

    public static PurchaseOrderLineDto ToLineDto(PurchaseOrderLine l) => new()
    {
        Id = l.Id,
        PartId = l.PartId,
        PartNumber = l.Part?.PartNumber,
        Description = l.Description,
        Quantity = l.Quantity,
        UnitPrice = l.UnitPrice,
        LineTotal = LineTotal(l),
        QuantityReceived = l.QuantityReceived,
        QuantityOutstanding = Math.Max(0, l.Quantity - l.QuantityReceived)
    };

    public static void ApplyUpsert(PurchaseOrder p, PurchaseOrderUpsertDto dto)
    {
        p.VendorId = dto.VendorId;
        p.OrderDate = dto.OrderDate;
        p.ExpectedDate = dto.ExpectedDate;
        p.Status = dto.Status;
        p.TaxPercent = dto.TaxPercent;
        p.Notes = dto.Notes;
    }

    public static void ApplyUpsert(PurchaseOrderLine l, PurchaseOrderLineUpsertDto dto)
    {
        l.PartId = dto.PartId;
        l.Description = dto.Description;
        l.Quantity = dto.Quantity;
        l.UnitPrice = dto.UnitPrice;
    }
}
