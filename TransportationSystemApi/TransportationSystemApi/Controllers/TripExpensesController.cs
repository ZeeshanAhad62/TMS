using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportationSystemApi.Data;
using TransportationSystemApi.Dtos;
using TransportationSystemApi.Mapping;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Controllers;

[ApiController]
[Route("api/trips/{tripId:int}/expenses")]
public class TripExpensesController : ControllerBase
{
    private readonly FleetDbContext _db;

    public TripExpensesController(FleetDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<TripExpenseDto>>> GetAll(int tripId)
    {
        if (!await _db.Trips.AnyAsync(t => t.Id == tripId)) return NotFound();
        var expenses = await _db.TripExpenses
            .Where(e => e.TripId == tripId)
            .OrderByDescending(e => e.Date)
            .ThenBy(e => e.Id)
            .ToListAsync();
        return expenses.Select(TripMapper.ToExpenseDto).ToList();
    }

    [HttpPost]
    public async Task<ActionResult<TripExpenseDto>> Create(int tripId, TripExpenseUpsertDto dto)
    {
        if (!await _db.Trips.AnyAsync(t => t.Id == tripId)) return NotFound("Trip not found.");

        var expense = new TripExpense { TripId = tripId };
        TripMapper.ApplyUpsert(expense, dto);

        _db.TripExpenses.Add(expense);
        await _db.SaveChangesAsync();
        return Ok(TripMapper.ToExpenseDto(expense));
    }

    [HttpPut("{expenseId:int}")]
    public async Task<IActionResult> Update(int tripId, int expenseId, TripExpenseUpsertDto dto)
    {
        var expense = await _db.TripExpenses.FirstOrDefaultAsync(e => e.Id == expenseId && e.TripId == tripId);
        if (expense is null) return NotFound();

        TripMapper.ApplyUpsert(expense, dto);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{expenseId:int}")]
    public async Task<IActionResult> Delete(int tripId, int expenseId)
    {
        var expense = await _db.TripExpenses.FirstOrDefaultAsync(e => e.Id == expenseId && e.TripId == tripId);
        if (expense is null) return NotFound();

        _db.TripExpenses.Remove(expense);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
