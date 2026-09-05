namespace TransportationSystemApi.Models;

// A single pay line on a pay run. Amount is stored (not always Quantity * Rate:
// a Manual line may be a flat figure). TripId is a soft link only -- no FK, so
// deleting a trip never touches a settled pay run (see migration 014).
public class PayRunLine
{
    public int Id { get; set; }

    public int PayRunId { get; set; }
    public PayRun? PayRun { get; set; }

    public int? TripId { get; set; }

    public string Description { get; set; } = string.Empty;
    public PayRunLineBasis Basis { get; set; } = PayRunLineBasis.Manual;
    public decimal Quantity { get; set; } = 1;
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
}
