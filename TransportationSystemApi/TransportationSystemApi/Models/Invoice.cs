namespace TransportationSystemApi.Models;

public class Invoice
{
    public int Id { get; set; }

    // System-generated identity
    public string InvoiceNumber { get; set; } = string.Empty;

    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public DateOnly InvoiceDate { get; set; }
    public DateOnly DueDate { get; set; }

    // User-set intent only: Draft / Sent / Cancelled. Paid / PartiallyPaid
    // are derived from payments at read time (see InvoiceMapper).
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

    public decimal TaxPercent { get; set; }
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public List<InvoiceLine> Lines { get; set; } = new();
    public List<Payment> Payments { get; set; } = new();
}
