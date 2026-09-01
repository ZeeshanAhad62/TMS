namespace TransportationSystemApi.Models;

public class Payment
{
    public int Id { get; set; }

    public int InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }

    public DateOnly Date { get; set; }
    public decimal Amount { get; set; }
    public PaymentMode Mode { get; set; } = PaymentMode.Cash;
    public string? Reference { get; set; }
    public string? Notes { get; set; }
}
