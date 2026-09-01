using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportationSystemApi.Data;
using TransportationSystemApi.Dtos;
using TransportationSystemApi.Mapping;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Controllers;

[ApiController]
[Route("api/invoices")]
public class InvoicesController : ControllerBase
{
    private readonly FleetDbContext _db;

    public InvoicesController(FleetDbContext db)
    {
        _db = db;
    }

    private IQueryable<Invoice> WithChildren() =>
        _db.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Lines)
            .Include(i => i.Payments);

    [HttpGet]
    public async Task<ActionResult<List<InvoiceListItemDto>>> GetAll(
        [FromQuery] int? customerId,
        [FromQuery] InvoiceStatus? status,
        [FromQuery] bool? overdue,
        [FromQuery] string? search)
    {
        var query = WithChildren().AsNoTracking().AsQueryable();

        if (customerId.HasValue)
            query = query.Where(i => i.CustomerId == customerId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(i =>
                i.InvoiceNumber.Contains(search) ||
                (i.Customer != null && i.Customer.Name.Contains(search)));
        }

        var invoices = await query.OrderByDescending(i => i.InvoiceDate).ThenByDescending(i => i.Id).ToListAsync();
        var result = invoices.Select(InvoiceMapper.ToListItemDto);

        if (status.HasValue)
            result = result.Where(i => i.Status == status.Value);

        if (overdue == true)
            result = result.Where(i => i.IsOverdue);

        return result.ToList();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<InvoiceDetailDto>> GetById(int id)
    {
        var invoice = await WithChildren().FirstOrDefaultAsync(i => i.Id == id);
        if (invoice is null) return NotFound();
        return InvoiceMapper.ToDetailDto(invoice);
    }

    [HttpPost]
    public async Task<ActionResult<InvoiceDetailDto>> Create(InvoiceUpsertDto dto)
    {
        if (!await _db.Customers.AnyAsync(c => c.Id == dto.CustomerId))
            return NotFound("Customer not found.");

        var invoice = new Invoice();
        InvoiceMapper.ApplyUpsert(invoice, dto);

        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();

        invoice.InvoiceNumber = $"INV-{invoice.Id:D5}";
        await _db.SaveChangesAsync();

        await _db.Entry(invoice).Reference(i => i.Customer).LoadAsync();
        return CreatedAtAction(nameof(GetById), new { id = invoice.Id }, InvoiceMapper.ToDetailDto(invoice));
    }

    [HttpPost("from-trips")]
    public async Task<ActionResult<InvoiceDetailDto>> CreateFromTrips(CreateInvoiceFromTripsDto dto)
    {
        if (!await _db.Customers.AnyAsync(c => c.Id == dto.CustomerId))
            return NotFound("Customer not found.");

        var trips = await _db.Trips
            .Where(t => dto.TripIds.Contains(t.Id))
            .ToListAsync();

        var missing = dto.TripIds.Except(trips.Select(t => t.Id)).ToList();
        if (missing.Count > 0)
            return NotFound($"Trip(s) not found: {string.Join(", ", missing)}.");

        var wrongCustomer = trips.Where(t => t.CustomerId != dto.CustomerId).Select(t => t.Id).ToList();
        if (wrongCustomer.Count > 0)
            return BadRequest($"Trip(s) not linked to this customer: {string.Join(", ", wrongCustomer)}.");

        var alreadyBilled = await _db.InvoiceLines
            .Where(l => l.TripId != null && dto.TripIds.Contains(l.TripId.Value))
            .Select(l => l.TripId!.Value)
            .Distinct()
            .ToListAsync();
        if (alreadyBilled.Count > 0)
            return BadRequest($"Trip(s) already on an invoice: {string.Join(", ", alreadyBilled)}.");

        var invoice = new Invoice
        {
            CustomerId = dto.CustomerId,
            InvoiceDate = dto.InvoiceDate,
            DueDate = dto.DueDate,
            TaxPercent = dto.TaxPercent,
            Notes = dto.Notes,
            Status = InvoiceStatus.Draft,
            Lines = trips
                .OrderBy(t => t.StartDate)
                .Select(t => new InvoiceLine
                {
                    TripId = t.Id,
                    Description = $"Trip {t.TripCode}: {t.Origin} - {t.Destination}",
                    Quantity = 1,
                    UnitPrice = t.Revenue ?? 0m
                })
                .ToList()
        };

        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();

        invoice.InvoiceNumber = $"INV-{invoice.Id:D5}";
        await _db.SaveChangesAsync();

        await _db.Entry(invoice).Reference(i => i.Customer).LoadAsync();
        return CreatedAtAction(nameof(GetById), new { id = invoice.Id }, InvoiceMapper.ToDetailDto(invoice));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, InvoiceUpsertDto dto)
    {
        var invoice = await _db.Invoices.FindAsync(id);
        if (invoice is null) return NotFound();

        if (!await _db.Customers.AnyAsync(c => c.Id == dto.CustomerId))
            return NotFound("Customer not found.");

        InvoiceMapper.ApplyUpsert(invoice, dto);
        invoice.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var invoice = await _db.Invoices.FindAsync(id);
        if (invoice is null) return NotFound();

        _db.Invoices.Remove(invoice);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("billable-trips")]
    public async Task<ActionResult<List<BillableTripDto>>> BillableTrips([FromQuery] int customerId)
    {
        if (!await _db.Customers.AnyAsync(c => c.Id == customerId))
            return NotFound("Customer not found.");

        var billedTripIds = await _db.InvoiceLines
            .Where(l => l.TripId != null)
            .Select(l => l.TripId!.Value)
            .Distinct()
            .ToListAsync();

        var trips = await _db.Trips
            .AsNoTracking()
            .Where(t => t.CustomerId == customerId && !billedTripIds.Contains(t.Id))
            .OrderBy(t => t.StartDate)
            .Select(t => new BillableTripDto
            {
                TripId = t.Id,
                TripCode = t.TripCode,
                Origin = t.Origin,
                Destination = t.Destination,
                StartDate = t.StartDate,
                Status = t.Status,
                Revenue = t.Revenue ?? 0m
            })
            .ToListAsync();

        return trips;
    }

    [HttpGet("aging")]
    public async Task<ActionResult<InvoiceAgingDto>> Aging()
    {
        var invoices = await WithChildren().AsNoTracking().ToListAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var collectedThisMonth = invoices
            .SelectMany(i => i.Payments)
            .Where(p => p.Date.Year == today.Year && p.Date.Month == today.Month)
            .Sum(p => p.Amount);

        var open = invoices
            .Select(i => new
            {
                Balance = InvoiceMapper.Balance(i),
                Effective = InvoiceMapper.EffectiveStatus(i),
                i.DueDate
            })
            .Where(x => x.Balance > 0 && x.Effective != InvoiceStatus.Cancelled)
            .ToList();

        int DaysOverdue(DateOnly due) => today.DayNumber - due.DayNumber;

        InvoiceAgingBucketDto Bucket(string label, Func<int, bool> daysPredicate)
        {
            var rows = open.Where(x => daysPredicate(DaysOverdue(x.DueDate))).ToList();
            return new InvoiceAgingBucketDto { Label = label, Count = rows.Count, Amount = rows.Sum(r => r.Balance) };
        }

        var buckets = new List<InvoiceAgingBucketDto>
        {
            Bucket("Current", d => d <= 0),
            Bucket("1-30 days", d => d is > 0 and <= 30),
            Bucket("31-60 days", d => d is > 30 and <= 60),
            Bucket("61-90 days", d => d is > 60 and <= 90),
            Bucket("90+ days", d => d > 90)
        };

        return new InvoiceAgingDto
        {
            TotalOutstanding = open.Sum(x => x.Balance),
            OverdueAmount = open.Where(x => x.DueDate < today).Sum(x => x.Balance),
            CollectedThisMonth = collectedThisMonth,
            Buckets = buckets
        };
    }
}
