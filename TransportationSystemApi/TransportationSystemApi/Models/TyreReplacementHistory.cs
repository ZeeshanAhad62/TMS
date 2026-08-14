namespace TransportationSystemApi.Models;

public class TyreReplacementHistory
{
    public int Id { get; set; }
    public int TyreId { get; set; }
    public Tyre? Tyre { get; set; }

    public DateOnly ReplacedDate { get; set; }
    public decimal? OdometerAtReplacement { get; set; }
    public string? OldBrandAndSize { get; set; }
    public string? NewBrandAndSize { get; set; }
    public string? Reason { get; set; }
}
