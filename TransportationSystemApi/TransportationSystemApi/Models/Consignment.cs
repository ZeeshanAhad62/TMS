namespace TransportationSystemApi.Models;

// A lorry receipt / consignment note carried on a trip. One trip carries many.
// The POD (proof-of-delivery) file's metadata is stored here; the file lives
// on disk (see ConsignmentsController).
public class Consignment
{
    public int Id { get; set; }

    // System-generated identity: CN-00001
    public string ConsignmentCode { get; set; } = string.Empty;

    public int TripId { get; set; }
    public Trip? Trip { get; set; }

    // The physical LR / consignment-note number written on the paper copy.
    public string? LrNumber { get; set; }
    public DateOnly BookingDate { get; set; }

    public string ConsignorName { get; set; } = string.Empty;
    public string? ConsignorAddress { get; set; }
    public string? ConsignorPhone { get; set; }

    public string ConsigneeName { get; set; } = string.Empty;
    public string? ConsigneeAddress { get; set; }
    public string? ConsigneePhone { get; set; }

    public string? GoodsDescription { get; set; }
    public int? Packages { get; set; }
    public decimal? WeightKg { get; set; }
    public decimal? DeclaredValue { get; set; }
    public decimal? FreightAmount { get; set; }
    public FreightTerms FreightTerms { get; set; } = FreightTerms.Prepaid;

    public ConsignmentStatus Status { get; set; } = ConsignmentStatus.Booked;
    public DateTime? DeliveredAt { get; set; }
    public string? ReceivedBy { get; set; }
    public string? DeliveryNotes { get; set; }

    public string? PodFileName { get; set; }
    public string? PodContentType { get; set; }
    public string? PodStoragePath { get; set; }
    public long? PodFileSizeBytes { get; set; }
    public DateTime? PodUploadedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
