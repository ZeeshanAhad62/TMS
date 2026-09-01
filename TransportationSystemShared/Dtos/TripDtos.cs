using System.ComponentModel.DataAnnotations;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Dtos;

public class TripListItemDto
{
    public int Id { get; set; }
    public string TripCode { get; set; } = string.Empty;
    public string VehicleCode { get; set; } = string.Empty;
    public string VehicleRegistrationNumber { get; set; } = string.Empty;
    public string DriverName { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public TripStatus Status { get; set; }
}

public class TripDetailDto : TripUpsertDto
{
    public int Id { get; set; }
    public string TripCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public string VehicleCode { get; set; } = string.Empty;
    public string VehicleRegistrationNumber { get; set; } = string.Empty;
    public string DriverName { get; set; } = string.Empty;
    public string? CustomerName { get; set; }

    // Per-trip P&L (driver pay stays 0 until the Payroll module lands).
    public decimal RevenueAmount { get; set; }
    public decimal FuelCost { get; set; }
    public decimal ExpensesTotal { get; set; }
    public decimal DriverPay { get; set; }
    public decimal NetProfit { get; set; }

    public List<TripExpenseDto> Expenses { get; set; } = new();
}

public class TripExpenseDto
{
    public int Id { get; set; }
    public TripExpenseCategory Category { get; set; }
    public decimal Amount { get; set; }
    public DateOnly Date { get; set; }
    public ExpensePaidBy PaidBy { get; set; }
    public string? ReceiptNumber { get; set; }
    public string? Notes { get; set; }
}

public class TripExpenseUpsertDto
{
    public TripExpenseCategory Category { get; set; } = TripExpenseCategory.Toll;

    [Range(0, 99999999)]
    public decimal Amount { get; set; }

    [Required]
    public DateOnly Date { get; set; }

    public ExpensePaidBy PaidBy { get; set; } = ExpensePaidBy.Company;

    [MaxLength(80)]
    public string? ReceiptNumber { get; set; }

    public string? Notes { get; set; }
}

public class TripUpsertDto
{
    [Required]
    public int VehicleId { get; set; }

    [Required]
    public int DriverId { get; set; }

    public int? CustomerId { get; set; }

    [Required, MaxLength(150)]
    public string Origin { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string Destination { get; set; } = string.Empty;

    [Required]
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    public TripStatus Status { get; set; } = TripStatus.Scheduled;
    public string? Notes { get; set; }
    public decimal? Revenue { get; set; }
}
