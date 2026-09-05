namespace TransportationSystemApi.Models;

// One settlement for a driver over a period. GrossPay (= Σ line amounts +
// AllowancesTotal) and NetPay (= GrossPay - AdvanceRecovery) are computed at
// read time by PayrollMapper, not stored.
public class PayRun
{
    public int Id { get; set; }

    // System-generated identity: PR-00001
    public string PayRunCode { get; set; } = string.Empty;

    public int DriverId { get; set; }
    public Driver? Driver { get; set; }

    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }

    // User-set intent only. Draft while being edited; Approved / Paid once
    // signed off; Cancelled to void it.
    public PayRunStatus Status { get; set; } = PayRunStatus.Draft;

    public decimal AllowancesTotal { get; set; }
    public decimal AdvanceRecovery { get; set; }
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public List<PayRunLine> Lines { get; set; } = new();
}
