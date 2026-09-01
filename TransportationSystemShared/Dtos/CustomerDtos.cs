using System.ComponentModel.DataAnnotations;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Dtos;

public class CustomerListItemDto
{
    public int Id { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public decimal? CreditLimit { get; set; }
    public int? PaymentTermsDays { get; set; }
    public CustomerStatus Status { get; set; }
}

public class CustomerDetailDto : CustomerUpsertDto
{
    public int Id { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CustomerUpsertDto
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? ContactPerson { get; set; }

    [Required, MaxLength(30)]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(150), EmailAddress]
    public string? Email { get; set; }

    public string? BillingAddress { get; set; }

    [MaxLength(50)]
    public string? TaxNumber { get; set; }

    [Range(0, 999999999)]
    public decimal? CreditLimit { get; set; }

    [Range(0, 365)]
    public int? PaymentTermsDays { get; set; }

    public CustomerStatus Status { get; set; } = CustomerStatus.Active;

    public string? Notes { get; set; }
}
