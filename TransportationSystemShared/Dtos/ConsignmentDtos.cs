using System.ComponentModel.DataAnnotations;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Dtos;

public class ConsignmentListItemDto
{
    public int Id { get; set; }
    public string ConsignmentCode { get; set; } = string.Empty;
    public string? LrNumber { get; set; }
    public int TripId { get; set; }
    public string TripCode { get; set; } = string.Empty;
    public DateOnly BookingDate { get; set; }
    public string ConsignorName { get; set; } = string.Empty;
    public string ConsigneeName { get; set; } = string.Empty;
    public decimal? WeightKg { get; set; }
    public decimal? FreightAmount { get; set; }
    public FreightTerms FreightTerms { get; set; }
    public ConsignmentStatus Status { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public bool HasPod { get; set; }
}

public class ConsignmentDetailDto : ConsignmentUpsertDto
{
    public int Id { get; set; }
    public string ConsignmentCode { get; set; } = string.Empty;
    public string TripCode { get; set; } = string.Empty;

    public DateTime? DeliveredAt { get; set; }
    public string? ReceivedBy { get; set; }
    public string? DeliveryNotes { get; set; }

    public bool HasPod { get; set; }
    public string? PodFileName { get; set; }
    public long? PodFileSizeBytes { get; set; }
    public DateTime? PodUploadedAt { get; set; }
    public string? PodDownloadUrl { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class ConsignmentUpsertDto
{
    [Required]
    public int TripId { get; set; }

    [MaxLength(80)]
    public string? LrNumber { get; set; }

    [Required]
    public DateOnly BookingDate { get; set; }

    public ConsignmentStatus Status { get; set; } = ConsignmentStatus.Booked;

    [Required, MaxLength(200)]
    public string ConsignorName { get; set; } = string.Empty;
    public string? ConsignorAddress { get; set; }
    [MaxLength(40)]
    public string? ConsignorPhone { get; set; }

    [Required, MaxLength(200)]
    public string ConsigneeName { get; set; } = string.Empty;
    public string? ConsigneeAddress { get; set; }
    [MaxLength(40)]
    public string? ConsigneePhone { get; set; }

    public string? GoodsDescription { get; set; }
    [Range(0, 100000)]
    public int? Packages { get; set; }
    [Range(0, 9999999)]
    public decimal? WeightKg { get; set; }
    [Range(0, 999999999)]
    public decimal? DeclaredValue { get; set; }
    [Range(0, 999999999)]
    public decimal? FreightAmount { get; set; }
    public FreightTerms FreightTerms { get; set; } = FreightTerms.Prepaid;
}

// POST api/consignments/{id}/deliver
public class DeliverConsignmentDto
{
    [Required, MaxLength(150)]
    public string ReceivedBy { get; set; } = string.Empty;

    public DateTime? DeliveredAt { get; set; }
    public string? DeliveryNotes { get; set; }
}
