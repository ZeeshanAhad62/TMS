using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportationSystemApi.Data;
using TransportationSystemApi.Dtos;
using TransportationSystemApi.Mapping;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Controllers;

[ApiController]
[Route("api/vendors")]
public class VendorsController : ControllerBase
{
    private readonly FleetDbContext _db;

    public VendorsController(FleetDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<VendorListItemDto>>> GetAll(
        [FromQuery] VendorType? vendorType,
        [FromQuery] bool? activeOnly,
        [FromQuery] string? search)
    {
        var query = _db.Vendors.Include(v => v.PurchaseOrders).AsNoTracking().AsQueryable();

        if (vendorType.HasValue) query = query.Where(v => v.VendorType == vendorType.Value);
        if (activeOnly == true) query = query.Where(v => v.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(v =>
                v.VendorCode.Contains(search) ||
                v.Name.Contains(search) ||
                (v.ContactPerson != null && v.ContactPerson.Contains(search)) ||
                (v.Phone != null && v.Phone.Contains(search)));
        }

        var vendors = await query.OrderBy(v => v.Name).ToListAsync();
        return vendors.Select(VendorMapper.ToListItemDto).ToList();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<VendorDetailDto>> GetById(int id)
    {
        var vendor = await _db.Vendors
            .Include(v => v.PurchaseOrders).ThenInclude(p => p.Lines)
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == id);
        if (vendor is null) return NotFound();
        return VendorMapper.ToDetailDto(vendor);
    }

    [HttpPost]
    public async Task<ActionResult<VendorDetailDto>> Create(VendorUpsertDto dto)
    {
        var vendor = new Vendor();
        VendorMapper.ApplyUpsert(vendor, dto);

        _db.Vendors.Add(vendor);
        await _db.SaveChangesAsync();

        vendor.VendorCode = $"VND-{vendor.Id:D5}";
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = vendor.Id }, VendorMapper.ToDetailDto(vendor));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, VendorUpsertDto dto)
    {
        var vendor = await _db.Vendors.FindAsync(id);
        if (vendor is null) return NotFound();

        VendorMapper.ApplyUpsert(vendor, dto);
        vendor.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var vendor = await _db.Vendors.FindAsync(id);
        if (vendor is null) return NotFound();

        // PurchaseOrders (and their lines) cascade. Stock already received
        // against those POs stays -- the Receipt StockMovements are not touched.
        _db.Vendors.Remove(vendor);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
