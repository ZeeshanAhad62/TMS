using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportationSystemApi.Data;
using TransportationSystemApi.Dtos;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private const int ExpiringSoonThresholdDays = 30;
    private const int MonthsBack = 6;
    private const int TopN = 5;

    private readonly FleetDbContext _db;

    public ReportsController(FleetDbContext db)
    {
        _db = db;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ReportsSummaryDto>> GetSummary()
    {
        // Data volumes here are small (single-company fleet), so pull the rows
        // once and aggregate in memory -- keeps the grouping logic readable and
        // avoids DateOnly/GroupBy translation quirks.
        var vehicles = await _db.Vehicles.AsNoTracking().ToListAsync();
        var drivers = await _db.Drivers.AsNoTracking().ToListAsync();
        var trips = await _db.Trips.AsNoTracking().Include(t => t.Vehicle).Include(t => t.Driver).ToListAsync();
        var workOrders = await _db.WorkOrders.AsNoTracking().Include(w => w.Vehicle).Include(w => w.Items).ToListAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var soonThreshold = today.AddDays(ExpiringSoonThresholdDays);
        var months = LastMonths(MonthsBack);

        return new ReportsSummaryDto
        {
            GeneratedAt = DateTime.UtcNow,
            Fleet = BuildFleet(vehicles, today, soonThreshold),
            Drivers = BuildDrivers(drivers, soonThreshold),
            Trips = BuildTrips(trips, months),
            Maintenance = BuildMaintenance(workOrders, months)
        };
    }

    private static FleetReportDto BuildFleet(List<Vehicle> vehicles, DateOnly today, DateOnly soonThreshold)
    {
        bool DocExpiringSoon(Vehicle v)
        {
            var dates = new[]
            {
                v.RCExpiryDate, v.FitnessExpiryDate, v.PermitExpiryDate,
                v.InsuranceExpiryDate, v.PollutionCertExpiryDate, v.TaxPaidTill
            };
            return dates.Any(d => d.HasValue && d.Value <= soonThreshold);
        }

        return new FleetReportDto
        {
            TotalVehicles = vehicles.Count,
            VehiclesWithExpiringDocs = vehicles.Count(DocExpiringSoon),
            ByStatus = Bucketize(vehicles, v => v.CurrentStatus.ToString()),
            ByType = Bucketize(vehicles, v => v.VehicleType.ToString()),
            ByFuelType = Bucketize(vehicles, v => v.FuelType.ToString()),
            ByOwnership = Bucketize(vehicles, v => v.OwnershipType.ToString())
        };
    }

    private static DriverReportDto BuildDrivers(List<Driver> drivers, DateOnly soonThreshold)
    {
        return new DriverReportDto
        {
            TotalDrivers = drivers.Count,
            LicensesExpiringSoon = drivers.Count(d => d.LicenseExpiryDate.HasValue && d.LicenseExpiryDate.Value <= soonThreshold),
            ByStatus = Bucketize(drivers, d => d.Status.ToString())
        };
    }

    private static TripReportDto BuildTrips(List<Trip> trips, List<string> months)
    {
        var byMonth = months
            .Select(m => new MonthlySeriesPointDto
            {
                Month = m,
                Count = trips.Count(t => MonthKey(t.StartDate) == m),
                Amount = trips.Where(t => MonthKey(t.StartDate) == m).Sum(t => t.Revenue ?? 0m)
            })
            .ToList();

        var topVehicles = trips
            .Where(t => t.Vehicle != null)
            .GroupBy(t => t.VehicleId)
            .Select(g => new NamedCountDto
            {
                Name = g.First().Vehicle!.VehicleCode,
                SubLabel = g.First().Vehicle!.RegistrationNumber,
                Count = g.Count(),
                Amount = g.Sum(t => t.Revenue ?? 0m)
            })
            .OrderByDescending(x => x.Count)
            .Take(TopN)
            .ToList();

        var topDrivers = trips
            .Where(t => t.Driver != null)
            .GroupBy(t => t.DriverId)
            .Select(g => new NamedCountDto
            {
                Name = g.First().Driver!.FullName,
                SubLabel = g.First().Driver!.DriverCode,
                Count = g.Count(),
                Amount = g.Sum(t => t.Revenue ?? 0m)
            })
            .OrderByDescending(x => x.Count)
            .Take(TopN)
            .ToList();

        return new TripReportDto
        {
            TotalTrips = trips.Count,
            CompletedTrips = trips.Count(t => t.Status == TripStatus.Completed),
            ActiveTrips = trips.Count(t => t.Status == TripStatus.Active),
            ScheduledTrips = trips.Count(t => t.Status == TripStatus.Scheduled),
            TotalRevenue = trips.Sum(t => t.Revenue ?? 0m),
            CompletedRevenue = trips.Where(t => t.Status == TripStatus.Completed).Sum(t => t.Revenue ?? 0m),
            ByStatus = Bucketize(trips, t => t.Status.ToString()),
            ByMonth = byMonth,
            TopVehicles = topVehicles,
            TopDrivers = topDrivers
        };
    }

    private static MaintenanceReportDto BuildMaintenance(List<WorkOrder> workOrders, List<string> months)
    {
        decimal CostOf(WorkOrder w) => (w.LabourCost ?? 0m) + w.Items.Sum(i => i.Quantity * i.UnitCost);

        var costByMonth = months
            .Select(m =>
            {
                var inMonth = workOrders.Where(w => w.CompletedDate.HasValue && MonthKey(w.CompletedDate.Value) == m).ToList();
                return new MonthlySeriesPointDto
                {
                    Month = m,
                    Count = inMonth.Count,
                    Amount = inMonth.Sum(CostOf)
                };
            })
            .ToList();

        var costByVehicle = workOrders
            .Where(w => w.Vehicle != null)
            .GroupBy(w => w.VehicleId)
            .Select(g => new NamedCountDto
            {
                Name = g.First().Vehicle!.VehicleCode,
                SubLabel = g.First().Vehicle!.RegistrationNumber,
                Count = g.Count(),
                Amount = g.Sum(CostOf)
            })
            .OrderByDescending(x => x.Amount)
            .Take(TopN)
            .ToList();

        var byStatus = Bucketize(workOrders, w => w.Status.ToString());
        foreach (var bucket in byStatus)
            bucket.Amount = workOrders.Where(w => w.Status.ToString() == bucket.Label).Sum(CostOf);

        var byType = Bucketize(workOrders, w => w.Type.ToString());
        foreach (var bucket in byType)
            bucket.Amount = workOrders.Where(w => w.Type.ToString() == bucket.Label).Sum(CostOf);

        return new MaintenanceReportDto
        {
            TotalWorkOrders = workOrders.Count,
            OpenWorkOrders = workOrders.Count(w => w.Status == WorkOrderStatus.Open),
            InProgressWorkOrders = workOrders.Count(w => w.Status == WorkOrderStatus.InProgress),
            CompletedWorkOrders = workOrders.Count(w => w.Status == WorkOrderStatus.Completed),
            TotalCost = workOrders.Where(w => w.Status == WorkOrderStatus.Completed).Sum(CostOf),
            ByStatus = byStatus,
            ByType = byType,
            CostByMonth = costByMonth,
            CostByVehicle = costByVehicle
        };
    }

    private static List<ReportBucketDto> Bucketize<T>(IEnumerable<T> source, Func<T, string> keySelector) =>
        source.GroupBy(keySelector)
            .Select(g => new ReportBucketDto { Label = g.Key, Count = g.Count() })
            .OrderByDescending(b => b.Count)
            .ToList();

    private static string MonthKey(DateOnly d) => $"{d.Year:D4}-{d.Month:D2}";

    private static List<string> LastMonths(int count)
    {
        var now = DateTime.UtcNow;
        var anchor = new DateTime(now.Year, now.Month, 1);
        return Enumerable.Range(0, count)
            .Select(i => anchor.AddMonths(-(count - 1 - i)))
            .Select(d => $"{d.Year:D4}-{d.Month:D2}")
            .ToList();
    }
}
