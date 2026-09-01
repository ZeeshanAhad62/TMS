namespace TransportationSystemApi.Models;

public class InvoiceLine
{
    public int Id { get; set; }

    public int InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }

    // Soft link to the trip this line bills for (no FK -- an issued invoice
    // is a frozen record and must survive a trip deletion unchanged).
    public int? TripId { get; set; }

    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
}
