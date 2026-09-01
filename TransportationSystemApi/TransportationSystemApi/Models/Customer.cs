namespace TransportationSystemApi.Models;

public class Customer
{
    public int Id { get; set; }

    // System-generated identity
    public string CustomerCode { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? BillingAddress { get; set; }
    public string? TaxNumber { get; set; }
    public decimal? CreditLimit { get; set; }
    public int? PaymentTermsDays { get; set; }
    public CustomerStatus Status { get; set; } = CustomerStatus.Active;
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
