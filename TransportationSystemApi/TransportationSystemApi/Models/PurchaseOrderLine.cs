namespace TransportationSystemApi.Models;

// One ordered item on a PO. QuantityReceived accumulates across partial goods
// receipts; when PartId is set, each receipt also writes a Receipt
// StockMovement (ReferenceType = PurchaseOrder). LineTotal = Quantity *
// UnitPrice is computed in the DTO.
public class PurchaseOrderLine
{
    public int Id { get; set; }

    public int PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }

    public int? PartId { get; set; }
    public Part? Part { get; set; }

    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal QuantityReceived { get; set; }
}
