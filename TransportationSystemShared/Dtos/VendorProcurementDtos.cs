using System.ComponentModel.DataAnnotations;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Dtos;

// ----- Vendors -----

public class VendorListItemDto
{
    public int Id { get; set; }
    public string VendorCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public VendorType VendorType { get; set; }
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public int? PaymentTermsDays { get; set; }
    public bool IsActive { get; set; }
    public int OpenPurchaseOrderCount { get; set; }
}

public class VendorDetailDto : VendorUpsertDto
{
    public int Id { get; set; }
    public string VendorCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<PurchaseOrderListItemDto> PurchaseOrders { get; set; } = new();
}

public class VendorUpsertDto
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public VendorType VendorType { get; set; } = VendorType.PartsSupplier;

    [MaxLength(150)]
    public string? ContactPerson { get; set; }

    [MaxLength(40)]
    public string? Phone { get; set; }

    [MaxLength(150)]
    public string? Email { get; set; }

    public string? Address { get; set; }

    [MaxLength(60)]
    public string? TaxNumber { get; set; }

    [Range(0, 3650)]
    public int? PaymentTermsDays { get; set; }

    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
}

// ----- Purchase orders -----

public class PurchaseOrderListItemDto
{
    public int Id { get; set; }
    public string PoNumber { get; set; } = string.Empty;
    public int VendorId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public DateOnly OrderDate { get; set; }
    public DateOnly? ExpectedDate { get; set; }
    public PurchaseOrderStatus Status { get; set; }
    public PurchaseOrderStatus EffectiveStatus { get; set; }
    public decimal Total { get; set; }
    public decimal ReceivedPercent { get; set; }
    public int LineCount { get; set; }
}

public class PurchaseOrderDetailDto : PurchaseOrderUpsertDto
{
    public int Id { get; set; }
    public string PoNumber { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public PurchaseOrderStatus EffectiveStatus { get; set; }

    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public decimal ReceivedPercent { get; set; }
    public bool FullyReceived { get; set; }
    public bool HasAnyReceipt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public List<PurchaseOrderLineDto> Lines { get; set; } = new();
}

public class PurchaseOrderUpsertDto
{
    [Required]
    public int VendorId { get; set; }

    [Required]
    public DateOnly OrderDate { get; set; }
    public DateOnly? ExpectedDate { get; set; }

    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;

    [Range(0, 100)]
    public decimal TaxPercent { get; set; }

    public string? Notes { get; set; }
}

public class PurchaseOrderLineDto
{
    public int Id { get; set; }
    public int? PartId { get; set; }
    public string? PartNumber { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal QuantityOutstanding { get; set; }
}

public class PurchaseOrderLineUpsertDto
{
    public int? PartId { get; set; }

    [Required, MaxLength(300)]
    public string Description { get; set; } = string.Empty;

    [Range(0.01, 999999999)]
    public decimal Quantity { get; set; } = 1;

    [Range(0, 999999999)]
    public decimal UnitPrice { get; set; }
}

// ----- Goods receipt -----

public class ReceiveGoodsDto
{
    [Required]
    public List<ReceiveLineDto> Lines { get; set; } = new();

    // Applied to every Receipt movement created; defaults to today.
    public DateOnly? Date { get; set; }
}

public class ReceiveLineDto
{
    [Required]
    public int LineId { get; set; }

    [Range(0.01, 999999999)]
    public decimal Quantity { get; set; }

    // Overrides the line's UnitPrice on the stock movement when supplied.
    public decimal? UnitCost { get; set; }
}

public class ReceiveGoodsResultDto
{
    public int LinesReceived { get; set; }
    public int StockMovementsCreated { get; set; }
    public PurchaseOrderStatus NewStatus { get; set; }
    public List<string> Errors { get; set; } = new();
}
