namespace TransportationSystemApi.Models;

public class TripExpense
{
    public int Id { get; set; }

    public int TripId { get; set; }
    public Trip? Trip { get; set; }

    public TripExpenseCategory Category { get; set; }
    public decimal Amount { get; set; }
    public DateOnly Date { get; set; }
    public ExpensePaidBy PaidBy { get; set; } = ExpensePaidBy.Company;
    public string? ReceiptNumber { get; set; }
    public string? Notes { get; set; }
}
