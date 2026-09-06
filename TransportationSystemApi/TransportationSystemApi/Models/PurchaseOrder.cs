namespace TransportationSystemApi.Models;

// A purchase order raised against a vendor. SubTotal / TaxAmount / Total and
// the effective received status are computed at read time (PurchaseOrderMapper),
// not stored.
public class PurchaseOrder
{
    public int Id { get; set; }

    // System-generated identity: PO-00001
    public string PoNumber { get; set; } = string.Empty;

    public int VendorId { get; set; }
    public Vendor? Vendor { get; set; }

    public DateOnly OrderDate { get; set; }
    public DateOnly? ExpectedDate { get; set; }

    // User-set intent only. PartiallyReceived / Received are also derived from
    // the lines' QuantityReceived at read time.
    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;

    public decimal TaxPercent { get; set; }
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public List<PurchaseOrderLine> Lines { get; set; } = new();
}
