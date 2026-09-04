using System.ComponentModel.DataAnnotations;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Dtos;

public class PartListItemDto
{
    public int Id { get; set; }
    public string PartNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal ReorderLevel { get; set; }
    public decimal? StandardCost { get; set; }
    public decimal OnHandQty { get; set; }
    public decimal StockValue { get; set; }
    public bool BelowReorder { get; set; }
}

public class PartDetailDto : PartListItemDto
{
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<StockMovementDto> Movements { get; set; } = new();
}

public class PartUpsertDto
{
    [Required, MaxLength(100)]
    public string PartNumber { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(30)]
    public string Unit { get; set; } = "pcs";

    [Range(0, 999999999)]
    public decimal ReorderLevel { get; set; }

    [Range(0, 999999999)]
    public decimal? StandardCost { get; set; }

    public string? Notes { get; set; }
}

public class StockMovementDto
{
    public int Id { get; set; }
    public PartMovementType MovementType { get; set; }
    public decimal Quantity { get; set; }
    public decimal? UnitCost { get; set; }
    public DateOnly Date { get; set; }
    public StockMovementReferenceType ReferenceType { get; set; }
    public int? ReferenceId { get; set; }
    public string? SupplierName { get; set; }
    public string? Notes { get; set; }
}

// Manual movements only (Receipt / Issue / Adjust entered directly against a
// part). Movements auto-created from a WorkOrderItem are managed through the
// work order line itself, not this endpoint.
public class StockMovementUpsertDto
{
    [Required]
    public PartMovementType MovementType { get; set; }

    [Required]
    public decimal Quantity { get; set; }

    [Range(0, 999999999)]
    public decimal? UnitCost { get; set; }

    [Required]
    public DateOnly Date { get; set; }

    [MaxLength(150)]
    public string? SupplierName { get; set; }

    public string? Notes { get; set; }
}
