namespace TransportationSystemApi.Dtos;

// A single "label -> count" bucket used by every breakdown chart/table on the
// Reports & Analytics dashboard.
public class ReportBucketDto
{
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Amount { get; set; }
}

public class MonthlySeriesPointDto
{
    public string Month { get; set; } = string.Empty; // "yyyy-MM"
    public int Count { get; set; }
    public decimal Amount { get; set; }
}

public class NamedCountDto
{
    public string Name { get; set; } = string.Empty;
    public string SubLabel { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Amount { get; set; }
}

public class FleetReportDto
{
    public int TotalVehicles { get; set; }
    public int VehiclesWithExpiringDocs { get; set; }
    public List<ReportBucketDto> ByStatus { get; set; } = new();
    public List<ReportBucketDto> ByType { get; set; } = new();
    public List<ReportBucketDto> ByFuelType { get; set; } = new();
    public List<ReportBucketDto> ByOwnership { get; set; } = new();
}

public class DriverReportDto
{
    public int TotalDrivers { get; set; }
    public int LicensesExpiringSoon { get; set; }
    public List<ReportBucketDto> ByStatus { get; set; } = new();
}

public class TripReportDto
{
    public int TotalTrips { get; set; }
    public int CompletedTrips { get; set; }
    public int ActiveTrips { get; set; }
    public int ScheduledTrips { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal CompletedRevenue { get; set; }
    public List<ReportBucketDto> ByStatus { get; set; } = new();
    public List<MonthlySeriesPointDto> ByMonth { get; set; } = new();
    public List<NamedCountDto> TopVehicles { get; set; } = new();
    public List<NamedCountDto> TopDrivers { get; set; } = new();
}

public class MaintenanceReportDto
{
    public int TotalWorkOrders { get; set; }
    public int OpenWorkOrders { get; set; }
    public int InProgressWorkOrders { get; set; }
    public int CompletedWorkOrders { get; set; }
    public decimal TotalCost { get; set; }
    public List<ReportBucketDto> ByStatus { get; set; } = new();
    public List<ReportBucketDto> ByType { get; set; } = new();
    public List<MonthlySeriesPointDto> CostByMonth { get; set; } = new();
    public List<NamedCountDto> CostByVehicle { get; set; } = new();
}

public class ReportsSummaryDto
{
    public DateTime GeneratedAt { get; set; }
    public FleetReportDto Fleet { get; set; } = new();
    public DriverReportDto Drivers { get; set; } = new();
    public TripReportDto Trips { get; set; } = new();
    public MaintenanceReportDto Maintenance { get; set; } = new();
}
