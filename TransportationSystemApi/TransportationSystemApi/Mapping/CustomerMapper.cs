using TransportationSystemApi.Dtos;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Mapping;

public static class CustomerMapper
{
    public static CustomerListItemDto ToListItemDto(Customer c) => new()
    {
        Id = c.Id,
        CustomerCode = c.CustomerCode,
        Name = c.Name,
        ContactPerson = c.ContactPerson,
        Phone = c.Phone,
        Email = c.Email,
        CreditLimit = c.CreditLimit,
        PaymentTermsDays = c.PaymentTermsDays,
        Status = c.Status
    };

    public static CustomerDetailDto ToDetailDto(Customer c) => new()
    {
        Id = c.Id,
        CustomerCode = c.CustomerCode,
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt,
        Name = c.Name,
        ContactPerson = c.ContactPerson,
        Phone = c.Phone,
        Email = c.Email,
        BillingAddress = c.BillingAddress,
        TaxNumber = c.TaxNumber,
        CreditLimit = c.CreditLimit,
        PaymentTermsDays = c.PaymentTermsDays,
        Status = c.Status,
        Notes = c.Notes
    };

    public static void ApplyUpsert(Customer c, CustomerUpsertDto dto)
    {
        c.Name = dto.Name;
        c.ContactPerson = dto.ContactPerson;
        c.Phone = dto.Phone;
        c.Email = dto.Email;
        c.BillingAddress = dto.BillingAddress;
        c.TaxNumber = dto.TaxNumber;
        c.CreditLimit = dto.CreditLimit;
        c.PaymentTermsDays = dto.PaymentTermsDays;
        c.Status = dto.Status;
        c.Notes = dto.Notes;
    }
}
