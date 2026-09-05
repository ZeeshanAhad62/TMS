namespace TransportationSystemApi.Models;

// A cash advance paid to a driver (khata). RecoveredAmount is the running
// total clawed back through pay runs; Outstanding (= Amount - RecoveredAmount)
// and the fully-recovered flag are derived at read time by PayrollMapper.
public class DriverAdvance
{
    public int Id { get; set; }

    public int DriverId { get; set; }
    public Driver? Driver { get; set; }

    public DateOnly Date { get; set; }
    public decimal Amount { get; set; }
    public decimal RecoveredAmount { get; set; }
    public string? Reason { get; set; }
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
