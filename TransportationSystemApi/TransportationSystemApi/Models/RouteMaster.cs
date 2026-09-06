namespace TransportationSystemApi.Models;

// Lane master. Named RouteMaster (table dbo.Routes) so it doesn't collide with
// Microsoft.AspNetCore.Mvc.RouteAttribute inside controllers.
public class RouteMaster
{
    public int Id { get; set; }

    // System-generated identity: RT-00001
    public string RouteCode { get; set; } = string.Empty;

    public string? Name { get; set; }
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public decimal? DistanceKm { get; set; }
    public decimal? EstimatedDurationHours { get; set; }

    // Default trip revenue when no customer rate contract applies.
    public decimal? StandardRate { get; set; }

    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public List<RateContract> RateContracts { get; set; } = new();
}
