using TransportationSystemApi.Dtos;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Mapping;

public static class ConsignmentMapper
{
    public static ConsignmentListItemDto ToListItemDto(Consignment c) => new()
    {
        Id = c.Id,
        ConsignmentCode = c.ConsignmentCode,
        LrNumber = c.LrNumber,
        TripId = c.TripId,
        TripCode = c.Trip?.TripCode ?? string.Empty,
        BookingDate = c.BookingDate,
        ConsignorName = c.ConsignorName,
        ConsigneeName = c.ConsigneeName,
        WeightKg = c.WeightKg,
        FreightAmount = c.FreightAmount,
        FreightTerms = c.FreightTerms,
        Status = c.Status,
        DeliveredAt = c.DeliveredAt,
        HasPod = !string.IsNullOrEmpty(c.PodStoragePath)
    };

    public static ConsignmentDetailDto ToDetailDto(Consignment c) => new()
    {
        Id = c.Id,
        ConsignmentCode = c.ConsignmentCode,
        TripId = c.TripId,
        TripCode = c.Trip?.TripCode ?? string.Empty,
        LrNumber = c.LrNumber,
        BookingDate = c.BookingDate,
        ConsignorName = c.ConsignorName,
        ConsignorAddress = c.ConsignorAddress,
        ConsignorPhone = c.ConsignorPhone,
        ConsigneeName = c.ConsigneeName,
        ConsigneeAddress = c.ConsigneeAddress,
        ConsigneePhone = c.ConsigneePhone,
        GoodsDescription = c.GoodsDescription,
        Packages = c.Packages,
        WeightKg = c.WeightKg,
        DeclaredValue = c.DeclaredValue,
        FreightAmount = c.FreightAmount,
        FreightTerms = c.FreightTerms,
        Status = c.Status,
        DeliveredAt = c.DeliveredAt,
        ReceivedBy = c.ReceivedBy,
        DeliveryNotes = c.DeliveryNotes,
        HasPod = !string.IsNullOrEmpty(c.PodStoragePath),
        PodFileName = c.PodFileName,
        PodFileSizeBytes = c.PodFileSizeBytes,
        PodUploadedAt = c.PodUploadedAt,
        PodDownloadUrl = string.IsNullOrEmpty(c.PodStoragePath) ? null : $"/api/consignments/{c.Id}/pod/download",
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt
    };

    public static void ApplyUpsert(Consignment c, ConsignmentUpsertDto dto)
    {
        c.TripId = dto.TripId;
        c.LrNumber = string.IsNullOrWhiteSpace(dto.LrNumber) ? null : dto.LrNumber.Trim();
        c.BookingDate = dto.BookingDate;

        c.Status = dto.Status;
        // Delivery metadata only makes sense while Status == Delivered; a plain
        // PUT that moves the status elsewhere clears it (use POST .../deliver to
        // set it with a receiver + timestamp).
        if (dto.Status != ConsignmentStatus.Delivered)
        {
            c.DeliveredAt = null;
            c.ReceivedBy = null;
            c.DeliveryNotes = null;
        }

        c.ConsignorName = dto.ConsignorName.Trim();
        c.ConsignorAddress = dto.ConsignorAddress;
        c.ConsignorPhone = dto.ConsignorPhone;
        c.ConsigneeName = dto.ConsigneeName.Trim();
        c.ConsigneeAddress = dto.ConsigneeAddress;
        c.ConsigneePhone = dto.ConsigneePhone;
        c.GoodsDescription = dto.GoodsDescription;
        c.Packages = dto.Packages;
        c.WeightKg = dto.WeightKg;
        c.DeclaredValue = dto.DeclaredValue;
        c.FreightAmount = dto.FreightAmount;
        c.FreightTerms = dto.FreightTerms;
    }
}
