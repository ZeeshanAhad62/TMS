using System.ComponentModel.DataAnnotations;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Dtos;

// ----- Driver advances (nested under a driver) -----

public class DriverAdvanceDto
{
    public int Id { get; set; }
    public int DriverId { get; set; }
    public DateOnly Date { get; set; }
    public decimal Amount { get; set; }
    public decimal RecoveredAmount { get; set; }
    public decimal Outstanding { get; set; }
    public bool IsFullyRecovered { get; set; }
    public string? Reason { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class DriverAdvanceUpsertDto
{
    [Required]
    public DateOnly Date { get; set; }

    [Range(0.01, 999999999)]
    public decimal Amount { get; set; }

    // Optional manual override of how much has been recovered. Left null on
    // create; the generate/recover flow keeps it in step with pay runs.
    [Range(0, 999999999)]
    public decimal? RecoveredAmount { get; set; }

    [MaxLength(300)]
    public string? Reason { get; set; }

    public string? Notes { get; set; }
}

// ----- Pay runs -----

public class PayRunListItemDto
{
    public int Id { get; set; }
    public string PayRunCode { get; set; } = string.Empty;
    public int DriverId { get; set; }
    public string DriverName { get; set; } = string.Empty;
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public PayRunStatus Status { get; set; }
    public decimal GrossPay { get; set; }
    public decimal AdvanceRecovery { get; set; }
    public decimal NetPay { get; set; }
    public int LineCount { get; set; }
}

public class PayRunDetailDto : PayRunUpsertDto
{
    public int Id { get; set; }
    public string PayRunCode { get; set; } = string.Empty;
    public string DriverName { get; set; } = string.Empty;
    public string DriverCode { get; set; } = string.Empty;
    public DriverPayType DriverPayType { get; set; }
    public decimal? DriverPayRate { get; set; }

    public decimal LinesTotal { get; set; }
    public decimal GrossPay { get; set; }
    public decimal NetPay { get; set; }

    // Advances still owed by this driver (across all pay runs), for the
    // "recover advances" helper on the editor.
    public decimal AdvancesOutstanding { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public List<PayRunLineDto> Lines { get; set; } = new();
}

public class PayRunUpsertDto
{
    [Required]
    public int DriverId { get; set; }

    [Required]
    public DateOnly PeriodStart { get; set; }

    [Required]
    public DateOnly PeriodEnd { get; set; }

    public PayRunStatus Status { get; set; } = PayRunStatus.Draft;

    [Range(0, 999999999)]
    public decimal AllowancesTotal { get; set; }

    [Range(0, 999999999)]
    public decimal AdvanceRecovery { get; set; }

    public string? Notes { get; set; }
}

public class PayRunLineDto
{
    public int Id { get; set; }
    public int? TripId { get; set; }
    public string? TripCode { get; set; }
    public string Description { get; set; } = string.Empty;
    public PayRunLineBasis Basis { get; set; }
    public decimal Quantity { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
}

public class PayRunLineUpsertDto
{
    public int? TripId { get; set; }

    [Required, MaxLength(300)]
    public string Description { get; set; } = string.Empty;

    public PayRunLineBasis Basis { get; set; } = PayRunLineBasis.Manual;

    [Range(0, 999999999)]
    public decimal Quantity { get; set; } = 1;

    [Range(0, 999999999)]
    public decimal Rate { get; set; }

    [Range(0, 999999999)]
    public decimal Amount { get; set; }
}

// Body for POST api/payruns/generate -- builds a Draft pay run + lines from
// the driver's pay configuration and their trips in the period.
public class GeneratePayRunDto
{
    [Required]
    public int DriverId { get; set; }

    [Required]
    public DateOnly PeriodStart { get; set; }

    [Required]
    public DateOnly PeriodEnd { get; set; }

    // When true, pre-fills AdvanceRecovery with the driver's outstanding
    // advances (capped at gross pay).
    public bool RecoverAdvances { get; set; } = true;
}
