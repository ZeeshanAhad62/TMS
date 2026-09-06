namespace TransportationSystemApi.Models;

// Supplier master. VendorType lets workshops and fuel stations be catalogued
// alongside parts suppliers; the free-text Workshop / StationName fields on
// WorkOrder / FuelEntry are unchanged for now (linking them is a follow-up).
public class Vendor
{
    public int Id { get; set; }

    // System-generated identity: VND-00001
    public string VendorCode { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public VendorType VendorType { get; set; } = VendorType.PartsSupplier;

    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? TaxNumber { get; set; }
    public int? PaymentTermsDays { get; set; }

    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public List<PurchaseOrder> PurchaseOrders { get; set; } = new();
}
