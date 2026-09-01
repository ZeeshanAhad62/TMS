using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportationSystemApi.Data;
using TransportationSystemApi.Dtos;
using TransportationSystemApi.Mapping;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Controllers;

[ApiController]
[Route("api/invoices/{invoiceId:int}/payments")]
public class InvoicePaymentsController : ControllerBase
{
    private readonly FleetDbContext _db;

    public InvoicePaymentsController(FleetDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<PaymentDto>>> GetAll(int invoiceId)
    {
        if (!await _db.Invoices.AnyAsync(i => i.Id == invoiceId)) return NotFound();
        var payments = await _db.Payments
            .Where(p => p.InvoiceId == invoiceId)
            .OrderByDescending(p => p.Date)
            .ThenBy(p => p.Id)
            .ToListAsync();
        return payments.Select(InvoiceMapper.ToPaymentDto).ToList();
    }

    [HttpPost]
    public async Task<ActionResult<PaymentDto>> Create(int invoiceId, PaymentUpsertDto dto)
    {
        if (!await _db.Invoices.AnyAsync(i => i.Id == invoiceId)) return NotFound("Invoice not found.");

        var payment = new Payment { InvoiceId = invoiceId };
        InvoiceMapper.ApplyUpsert(payment, dto);

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();
        return Ok(InvoiceMapper.ToPaymentDto(payment));
    }

    [HttpPut("{paymentId:int}")]
    public async Task<IActionResult> Update(int invoiceId, int paymentId, PaymentUpsertDto dto)
    {
        var payment = await _db.Payments.FirstOrDefaultAsync(p => p.Id == paymentId && p.InvoiceId == invoiceId);
        if (payment is null) return NotFound();

        InvoiceMapper.ApplyUpsert(payment, dto);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{paymentId:int}")]
    public async Task<IActionResult> Delete(int invoiceId, int paymentId)
    {
        var payment = await _db.Payments.FirstOrDefaultAsync(p => p.Id == paymentId && p.InvoiceId == invoiceId);
        if (payment is null) return NotFound();

        _db.Payments.Remove(payment);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
