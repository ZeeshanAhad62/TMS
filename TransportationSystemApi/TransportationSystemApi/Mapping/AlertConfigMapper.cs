using TransportationSystemApi.Dtos;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Mapping;

public static class AlertConfigMapper
{
    public static AlertConfigDto ToDto(AlertConfig a) => new()
    {
        Id = a.Id,
        EntityType = a.EntityType,
        DocumentType = a.DocumentType,
        ThresholdDays = a.ThresholdDays,
        RecipientEmails = a.RecipientEmails,
        IsActive = a.IsActive,
        CreatedAt = a.CreatedAt,
        UpdatedAt = a.UpdatedAt
    };

    public static void ApplyUpsert(AlertConfig a, AlertConfigUpsertDto dto)
    {
        a.EntityType = dto.EntityType;
        a.DocumentType = string.IsNullOrWhiteSpace(dto.DocumentType) ? null : dto.DocumentType;
        a.ThresholdDays = dto.ThresholdDays;
        a.RecipientEmails = dto.RecipientEmails;
        a.IsActive = dto.IsActive;
    }

    public static AlertLogDto ToLogDto(AlertLog l) => new()
    {
        Id = l.Id,
        EntityType = l.EntityType,
        EntityId = l.EntityId,
        DocumentType = l.DocumentType,
        ExpiryDate = l.ExpiryDate,
        Severity = l.Severity,
        RecipientEmails = l.RecipientEmails,
        SentAt = l.SentAt
    };
}
