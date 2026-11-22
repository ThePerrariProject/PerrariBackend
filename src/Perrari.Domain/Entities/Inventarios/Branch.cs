using Perrari.Domain.Exceptions;
using Perrari.Domain.Common;

namespace Perrari.Domain.Entities.Inventarios;

public class Branch : BaseEntity, IAuditable
{
    private Branch() { }

    public string Name { get; private set; } = default!;
    public string? Address { get; private set; }
    public string? Phone { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public static Branch Create(string name, string? address, string? phone, DateTime now)
    {
        var branch = new Branch { CreatedAt = now };
        branch.Update(name, address, phone);
        return branch;
    }

    public void Update(string name, string? address, string? phone)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("El nombre de la sucursal es obligatorio");

        Name = name.Trim();
        Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim();
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
    }
}