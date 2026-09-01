using System.ComponentModel.DataAnnotations;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Dtos;

public class InvoiceListItemDto
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public DateOnly InvoiceDate { get; set; }
    public DateOnly DueDate { get; set; }
    public InvoiceStatus Status { get; set; }   // effective status
    public decimal Total { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal Balance { get; set; }
    public bool IsOverdue { get; set; }
}

public class InvoiceDetailDto : InvoiceUpsertDto
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal Balance { get; set; }
    public InvoiceStatus EffectiveStatus { get; set; }
    public bool IsOverdue { get; set; }

    public List<InvoiceLineDto> Lines { get; set; } = new();
    public List<PaymentDto> Payments { get; set; } = new();
}

public class InvoiceUpsertDto
{
    [Required]
    public int CustomerId { get; set; }

    [Required]
    public DateOnly InvoiceDate { get; set; }

    [Required]
    public DateOnly DueDate { get; set; }

    // Only Draft / Sent / Cancelled are honoured; Paid / PartiallyPaid are derived.
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

    [Range(0, 100)]
    public decimal TaxPercent { get; set; }

    public string? Notes { get; set; }
}

public class InvoiceLineDto
{
    public int Id { get; set; }
    public int? TripId { get; set; }
    public string? TripCode { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

public class InvoiceLineUpsertDto
{
    public int? TripId { get; set; }

    [Required, MaxLength(300)]
    public string Description { get; set; } = string.Empty;

    [Range(0, 9999999)]
    public decimal Quantity { get; set; } = 1;

    [Range(0, 99999999)]
    public decimal UnitPrice { get; set; }
}

public class PaymentDto
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public decimal Amount { get; set; }
    public PaymentMode Mode { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
}

public class PaymentUpsertDto
{
    [Required]
    public DateOnly Date { get; set; }

    [Range(0.01, 99999999)]
    public decimal Amount { get; set; }

    public PaymentMode Mode { get; set; } = PaymentMode.Cash;

    [MaxLength(120)]
    public string? Reference { get; set; }

    public string? Notes { get; set; }
}

public class CreateInvoiceFromTripsDto
{
    [Required]
    public int CustomerId { get; set; }

    [Required, MinLength(1)]
    public List<int> TripIds { get; set; } = new();

    [Required]
    public DateOnly InvoiceDate { get; set; }

    [Required]
    public DateOnly DueDate { get; set; }

    [Range(0, 100)]
    public decimal TaxPercent { get; set; }

    public string? Notes { get; set; }
}

public class BillableTripDto
{
    public int TripId { get; set; }
    public string TripCode { get; set; } = string.Empty;
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public TripStatus Status { get; set; }
    public decimal Revenue { get; set; }
}

public class InvoiceAgingBucketDto
{
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Amount { get; set; }
}

public class InvoiceAgingDto
{
    public decimal TotalOutstanding { get; set; }
    public decimal OverdueAmount { get; set; }
    public decimal CollectedThisMonth { get; set; }
    public List<InvoiceAgingBucketDto> Buckets { get; set; } = new();
}
