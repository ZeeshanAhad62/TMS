using TransportationSystemApi.Dtos;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Mapping;

public static class VendorMapper
{
    public static VendorListItemDto ToListItemDto(Vendor v) => new()
    {
        Id = v.Id,
        VendorCode = v.VendorCode,
        Name = v.Name,
        VendorType = v.VendorType,
        ContactPerson = v.ContactPerson,
        Phone = v.Phone,
        PaymentTermsDays = v.PaymentTermsDays,
        IsActive = v.IsActive,
        OpenPurchaseOrderCount = v.PurchaseOrders.Count(p =>
            p.Status != PurchaseOrderStatus.Received && p.Status != PurchaseOrderStatus.Cancelled)
    };

    public static VendorDetailDto ToDetailDto(Vendor v) => new()
    {
        Id = v.Id,
        VendorCode = v.VendorCode,
        Name = v.Name,
        VendorType = v.VendorType,
        ContactPerson = v.ContactPerson,
        Phone = v.Phone,
        Email = v.Email,
        Address = v.Address,
        TaxNumber = v.TaxNumber,
        PaymentTermsDays = v.PaymentTermsDays,
        IsActive = v.IsActive,
        Notes = v.Notes,
        CreatedAt = v.CreatedAt,
        UpdatedAt = v.UpdatedAt,
        PurchaseOrders = v.PurchaseOrders
            .OrderByDescending(p => p.OrderDate).ThenByDescending(p => p.Id)
            .Select(PurchaseOrderMapper.ToListItemDto)
            .ToList()
    };

    public static void ApplyUpsert(Vendor v, VendorUpsertDto dto)
    {
        v.Name = dto.Name.Trim();
        v.VendorType = dto.VendorType;
        v.ContactPerson = dto.ContactPerson;
        v.Phone = dto.Phone;
        v.Email = dto.Email;
        v.Address = dto.Address;
        v.TaxNumber = dto.TaxNumber;
        v.PaymentTermsDays = dto.PaymentTermsDays;
        v.IsActive = dto.IsActive;
        v.Notes = dto.Notes;
    }
}
