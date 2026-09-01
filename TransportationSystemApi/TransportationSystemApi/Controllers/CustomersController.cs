using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportationSystemApi.Data;
using TransportationSystemApi.Dtos;
using TransportationSystemApi.Mapping;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Controllers;

[ApiController]
[Route("api/customers")]
public class CustomersController : ControllerBase
{
    private readonly FleetDbContext _db;

    public CustomersController(FleetDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<CustomerListItemDto>>> GetAll(
        [FromQuery] CustomerStatus? status,
        [FromQuery] string? search)
    {
        var query = _db.Customers.AsNoTracking().AsQueryable();

        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(c =>
                c.CustomerCode.Contains(search) ||
                c.Name.Contains(search) ||
                c.Phone.Contains(search) ||
                (c.ContactPerson != null && c.ContactPerson.Contains(search)));
        }

        var customers = await query.OrderBy(c => c.Name).ToListAsync();
        return customers.Select(CustomerMapper.ToListItemDto).ToList();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CustomerDetailDto>> GetById(int id)
    {
        var customer = await _db.Customers.FindAsync(id);
        if (customer is null) return NotFound();
        return CustomerMapper.ToDetailDto(customer);
    }

    [HttpPost]
    public async Task<ActionResult<CustomerDetailDto>> Create(CustomerUpsertDto dto)
    {
        var customer = new Customer();
        CustomerMapper.ApplyUpsert(customer, dto);

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();

        customer.CustomerCode = $"CUST-{customer.Id:D5}";
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = customer.Id }, CustomerMapper.ToDetailDto(customer));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, CustomerUpsertDto dto)
    {
        var customer = await _db.Customers.FindAsync(id);
        if (customer is null) return NotFound();

        CustomerMapper.ApplyUpsert(customer, dto);
        customer.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var customer = await _db.Customers.FindAsync(id);
        if (customer is null) return NotFound();

        _db.Customers.Remove(customer);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
