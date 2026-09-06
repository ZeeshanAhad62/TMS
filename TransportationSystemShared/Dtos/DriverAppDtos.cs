using System.ComponentModel.DataAnnotations;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Dtos;

// ----- Auth -----

public class DriverAppLoginRequestDto
{
    [Required]
    public string DriverCode { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class DriverAppLoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DriverAppProfileDto Driver { get; set; } = new();
}

public class DriverAppProfileDto
{
    public int Id { get; set; }
    public string DriverCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? LicenseNumber { get; set; }
    public DateOnly? LicenseExpiryDate { get; set; }
}

// ----- My trips -----

public class DriverAppTripListItemDto
{
    public int Id { get; set; }
    public string TripCode { get; set; } = string.Empty;
    public string VehicleCode { get; set; } = string.Empty;
    public string VehicleRegistrationNumber { get; set; } = string.Empty;
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public TripStatus Status { get; set; }
    public int ConsignmentCount { get; set; }
    public int PendingPodCount { get; set; }
}

public class DriverAppTripDetailDto : DriverAppTripListItemDto
{
    public string? CustomerName { get; set; }
    public string? Notes { get; set; }
    public decimal ExpensesTotal { get; set; }
    public List<DriverAppExpenseDto> Expenses { get; set; } = new();
    public List<DriverAppConsignmentDto> Consignments { get; set; } = new();
}

public class DriverAppExpenseDto
{
    public int Id { get; set; }
    public TripExpenseCategory Category { get; set; }
    public decimal Amount { get; set; }
    public DateOnly Date { get; set; }
    public string? Notes { get; set; }
}

public class DriverAppConsignmentDto
{
    public int Id { get; set; }
    public string ConsignmentCode { get; set; } = string.Empty;
    public string? LrNumber { get; set; }
    public string ConsignorName { get; set; } = string.Empty;
    public string ConsigneeName { get; set; } = string.Empty;
    public ConsignmentStatus Status { get; set; }
    public bool HasPod { get; set; }
}

// ----- Actions -----

public class DriverAppTripStatusDto
{
    [Required]
    public TripStatus Status { get; set; }
}

public class DriverAppExpenseCreateDto
{
    public TripExpenseCategory Category { get; set; } = TripExpenseCategory.Misc;

    [Range(0.01, 99999999)]
    public decimal Amount { get; set; }

    public DateOnly? Date { get; set; }
    public string? Notes { get; set; }
}

public class DriverAppFuelCreateDto
{
    [Range(0, 99999999)]
    public decimal OdometerReading { get; set; }

    [Range(0.01, 99999)]
    public decimal Litres { get; set; }

    [Range(0, 999999)]
    public decimal RatePerLitre { get; set; }

    public DateOnly? Date { get; set; }
    public bool IsTankFull { get; set; } = true;

    [MaxLength(150)]
    public string? StationName { get; set; }
}

public class DriverAppDeliverDto
{
    [Required, MaxLength(150)]
    public string ReceivedBy { get; set; } = string.Empty;

    public string? DeliveryNotes { get; set; }
}
