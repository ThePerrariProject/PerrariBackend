using Perrari.Domain.Common;
using Perrari.Domain.Exceptions;

namespace Perrari.Domain.Entities.Clientes;

public class Client : BaseEntity, IAuditable
{
    private Client() { }

    public string Name { get; private set; } = default!;
    public string Rfc { get; private set; } = default!;
    public string Phone { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public static Client Create(string name, string rfc, string phone, string email, DateTime now)
    {
        var normalizedName = NormalizeName(name);
        var normalizedRfc = NormalizeRfc(rfc);
        var normalizedPhone = NormalizePhone(phone);
        var normalizedEmail = NormalizeEmail(email);

        Validate(normalizedName, normalizedRfc, normalizedPhone, normalizedEmail);

        return new Client
        {
            Name = normalizedName,
            Rfc = normalizedRfc,
            Phone = normalizedPhone,
            Email = normalizedEmail,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Update(string name, string rfc, string phone, string email, DateTime now)
    {
        if (!IsActive)
            throw new DomainException("No se puede editar un cliente desactivado.");

        var normalizedName = NormalizeName(name);
        var normalizedRfc = NormalizeRfc(rfc);
        var normalizedPhone = NormalizePhone(phone);
        var normalizedEmail = NormalizeEmail(email);

        Validate(normalizedName, normalizedRfc, normalizedPhone, normalizedEmail);

        Name = normalizedName;
        Rfc = normalizedRfc;
        Phone = normalizedPhone;
        Email = normalizedEmail;
        UpdatedAt = now;
    }

    public void Deactivate(DateTime now)
    {
        if (!IsActive) return;
        IsActive = false;
        UpdatedAt = now;
    }

    public void Reactivate(DateTime now)
    {
        if (IsActive) return;
        IsActive = true;
        UpdatedAt = now;
    }

    public static string NormalizeName(string? name) => (name ?? "").Trim();
    public static string NormalizeRfc(string? rfc) => (rfc ?? "").Trim().ToUpperInvariant();
    public static string NormalizePhone(string? phone) => (phone ?? "").Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Trim();
    public static string NormalizeEmail(string? email) => (email ?? "").Trim().ToLowerInvariant();

    private static void Validate(string name, string rfc, string phone, string email)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("El nombre del cliente es obligatorio.");
        if (name.Length > 255)
            throw new DomainException("El nombre no puede superar los 255 caracteres.");

        if (string.IsNullOrWhiteSpace(rfc))
            throw new DomainException("El RFC del cliente es obligatorio.");

        if (string.IsNullOrWhiteSpace(phone))
            throw new DomainException("El teléfono es obligatorio.");

        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("El correo es obligatorio.");
        if (email.Length > 255)
            throw new DomainException("El correo no puede superar los 255 caracteres.");
    }
}

