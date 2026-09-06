using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportationSystemApi.Data;
using TransportationSystemApi.Dtos;
using TransportationSystemApi.Mapping;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Controllers;

[ApiController]
[Route("api/purchase-orders")]
public class PurchaseOrdersController : ControllerBase
{
    private readonly FleetDbContext _db;

    public PurchaseOrdersController(FleetDbContext db)
    {
        _db = db;
    }

    private IQueryable<PurchaseOrder> WithChildren() =>
        _db.PurchaseOrders
            .Include(p => p.Vendor)
            .Include(p => p.Lines).ThenInclude(l => l.Part);

    [HttpGet]
    public async Task<ActionResult<List<PurchaseOrderListItemDto>>> GetAll(
        [FromQuery] int? vendorId,
        [FromQuery] PurchaseOrderStatus? status,
        [FromQuery] string? search)
    {
        var query = WithChildren().AsNoTracking().AsQueryable();

        if (vendorId.HasValue) query = query.Where(p => p.VendorId == vendorId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p =>
                p.PoNumber.Contains(search) ||
                (p.Vendor != null && p.Vendor.Name.Contains(search)));
        }

        var orders = await query.OrderByDescending(p => p.OrderDate).ThenByDescending(p => p.Id).ToListAsync();
        var result = orders.Select(PurchaseOrderMapper.ToListItemDto);

        if (status.HasValue)
            result = result.Where(p => p.EffectiveStatus == status.Value);

        return result.ToList();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PurchaseOrderDetailDto>> GetById(int id)
    {
        var po = await WithChildren().AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (po is null) return NotFound();
        return PurchaseOrderMapper.ToDetailDto(po);
    }

    [HttpPost]
    public async Task<ActionResult<PurchaseOrderDetailDto>> Create(PurchaseOrderUpsertDto dto)
    {
        var vendor = await _db.Vendors.FindAsync(dto.VendorId);
        if (vendor is null) return NotFound("Vendor not found.");

        var po = new PurchaseOrder { Vendor = vendor };
        PurchaseOrderMapper.ApplyUpsert(po, dto);

        _db.PurchaseOrders.Add(po);
        await _db.SaveChangesAsync();

        po.PoNumber = $"PO-{po.Id:D5}";
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = po.Id }, PurchaseOrderMapper.ToDetailDto(po));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, PurchaseOrderUpsertDto dto)
    {
        var po = await _db.PurchaseOrders.FindAsync(id);
        if (po is null) return NotFound();
        if (!await _db.Vendors.AnyAsync(v => v.Id == dto.VendorId))
            return NotFound("Vendor not found.");

        PurchaseOrderMapper.ApplyUpsert(po, dto);
        po.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var po = await _db.PurchaseOrders.Include(p => p.Lines).FirstOrDefaultAsync(p => p.Id == id);
        if (po is null) return NotFound();

        if (PurchaseOrderMapper.HasAnyReceipt(po))
            return BadRequest("Goods have already been received against this PO; cancel it instead of deleting.");

        _db.PurchaseOrders.Remove(po); // lines cascade
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // Record a (partial) goods receipt: bump each line's QuantityReceived and,
    // for part-linked lines, write a Receipt StockMovement against the Parts
    // master. Then roll the PO status forward.
    [HttpPost("{id:int}/receive")]
    public async Task<ActionResult<ReceiveGoodsResultDto>> Receive(int id, ReceiveGoodsDto dto)
    {
        var po = await _db.PurchaseOrders
            .Include(p => p.Vendor)
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (po is null) return NotFound();

        if (po.Status == PurchaseOrderStatus.Cancelled)
            return BadRequest("This PO is cancelled.");
        if (po.Status == PurchaseOrderStatus.Draft)
            return BadRequest("Mark the PO as Ordered before receiving goods against it.");

        var result = new ReceiveGoodsResultDto();
        var date = dto.Date ?? DateOnly.FromDateTime(DateTime.UtcNow);

        foreach (var recv in dto.Lines)
        {
            if (recv.Quantity <= 0) continue;

            var line = po.Lines.FirstOrDefault(l => l.Id == recv.LineId);
            if (line is null)
            {
                result.Errors.Add($"Line {recv.LineId} is not on this PO.");
                continue;
            }

            var outstanding = line.Quantity - line.QuantityReceived;
            if (recv.Quantity > outstanding)
            {
                result.Errors.Add($"Line {recv.LineId}: receiving {recv.Quantity} exceeds the {outstanding} still outstanding.");
                continue;
            }

            line.QuantityReceived += recv.Quantity;
            result.LinesReceived++;

            if (line.PartId.HasValue)
            {
                _db.StockMovements.Add(new StockMovement
                {
                    PartId = line.PartId.Value,
                    MovementType = PartMovementType.Receipt,
                    Quantity = recv.Quantity,
                    UnitCost = recv.UnitCost ?? line.UnitPrice,
                    Date = date,
                    ReferenceType = StockMovementReferenceType.PurchaseOrder,
                    ReferenceId = po.Id,
                    SupplierName = po.Vendor?.Name,
                    Notes = $"Received on {po.PoNumber}"
                });
                result.StockMovementsCreated++;
            }
        }

        if (PurchaseOrderMapper.FullyReceived(po))
            po.Status = PurchaseOrderStatus.Received;
        else if (PurchaseOrderMapper.HasAnyReceipt(po) && po.Status == PurchaseOrderStatus.Ordered)
            po.Status = PurchaseOrderStatus.PartiallyReceived;

        po.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        result.NewStatus = po.Status;
        return Ok(result);
    }
}
