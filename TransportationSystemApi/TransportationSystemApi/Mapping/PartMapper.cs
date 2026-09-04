using TransportationSystemApi.Dtos;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Mapping;

public static class PartMapper
{
    public static decimal OnHandQty(Part p) => p.Movements.Sum(m => m.MovementType switch
    {
        PartMovementType.Receipt => m.Quantity,
        PartMovementType.Issue => -m.Quantity,
        PartMovementType.Adjust => m.Quantity,
        _ => 0
    });

    public static decimal StockValue(Part p) => OnHandQty(p) * (p.StandardCost ?? 0);

    public static PartListItemDto ToListItemDto(Part p)
    {
        var onHand = OnHandQty(p);
        return new PartListItemDto
        {
            Id = p.Id,
            PartNumber = p.PartNumber,
            Name = p.Name,
            Unit = p.Unit,
            ReorderLevel = p.ReorderLevel,
            StandardCost = p.StandardCost,
            OnHandQty = onHand,
            StockValue = onHand * (p.StandardCost ?? 0),
            BelowReorder = onHand < p.ReorderLevel
        };
    }

    public static PartDetailDto ToDetailDto(Part p)
    {
        var onHand = OnHandQty(p);
        return new PartDetailDto
        {
            Id = p.Id,
            PartNumber = p.PartNumber,
            Name = p.Name,
            Unit = p.Unit,
            ReorderLevel = p.ReorderLevel,
            StandardCost = p.StandardCost,
            OnHandQty = onHand,
            StockValue = onHand * (p.StandardCost ?? 0),
            BelowReorder = onHand < p.ReorderLevel,
            Notes = p.Notes,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt,
            Movements = p.Movements
                .OrderByDescending(m => m.Date).ThenByDescending(m => m.Id)
                .Select(ToDto)
                .ToList()
        };
    }

    public static void ApplyUpsert(Part p, PartUpsertDto dto)
    {
        p.PartNumber = dto.PartNumber;
        p.Name = dto.Name;
        p.Unit = dto.Unit;
        p.ReorderLevel = dto.ReorderLevel;
        p.StandardCost = dto.StandardCost;
        p.Notes = dto.Notes;
    }

    public static StockMovementDto ToDto(StockMovement m) => new()
    {
        Id = m.Id,
        MovementType = m.MovementType,
        Quantity = m.Quantity,
        UnitCost = m.UnitCost,
        Date = m.Date,
        ReferenceType = m.ReferenceType,
        ReferenceId = m.ReferenceId,
        SupplierName = m.SupplierName,
        Notes = m.Notes
    };

    public static void ApplyUpsert(StockMovement m, StockMovementUpsertDto dto)
    {
        m.MovementType = dto.MovementType;
        m.Quantity = dto.Quantity;
        m.UnitCost = dto.UnitCost;
        m.Date = dto.Date;
        m.SupplierName = dto.SupplierName;
        m.Notes = dto.Notes;
    }
}
