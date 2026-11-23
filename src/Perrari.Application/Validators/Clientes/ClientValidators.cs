using System.Globalization;
using System.Text.RegularExpressions;
using FluentValidation;
using Perrari.Application.DTOs.Clientes;
using Perrari.Domain.Entities.Clientes;

namespace Perrari.Application.Validators.Clientes;

public static class RfcValidator
{
    public const string GenericGeneralPublic = "XAXX010101000";
    public const string GenericForeigner = "XEXX010101000";

    // Persona Física: 4 letras (incluye Ñ y &), 6 dígitos de fecha, 3 caracteres alfanuméricos homoclave
    private static readonly Regex FisicoRegex = new(@"^[A-ZÑ&]{4}[0-9]{6}[A-Z0-9]{3}$", RegexOptions.Compiled);

    // Persona Moral: 3 letras (incluye Ñ y &), 6 dígitos de fecha, 3 caracteres alfanuméricos homoclave
    private static readonly Regex MoralRegex = new(@"^[A-ZÑ&]{3}[0-9]{6}[A-Z0-9]{3}$", RegexOptions.Compiled);

    public static bool IsValid(string? rfc)
    {
        if (string.IsNullOrWhiteSpace(rfc))
            return false;

        var normalized = Client.NormalizeRfc(rfc);

        if (normalized == GenericGeneralPublic || normalized == GenericForeigner)
            return true;

        if (normalized.Length == 13)
        {
            if (!FisicoRegex.IsMatch(normalized))
                return false;

            return HasValidCalendarDate(normalized.Substring(4, 6));
        }

        if (normalized.Length == 12)
        {
            if (!MoralRegex.IsMatch(normalized))
                return false;

            return HasValidCalendarDate(normalized.Substring(3, 6));
        }

        return false;
    }

    private static bool HasValidCalendarDate(string datePart)
    {
        // El datePart son 6 dígitos: YYMMDD
        // Validamos que mes esté entre 01 y 12 y día entre 01 y 31 y corresponda a fecha real
        if (!int.TryParse(datePart.Substring(2, 2), out var month) || month < 1 || month > 12)
            return false;

        if (!int.TryParse(datePart.Substring(4, 2), out var day) || day < 1 || day > 31)
            return false;

        return DateTime.TryParseExact(
            datePart,
            "yyMMdd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out _);
    }
}

public static class PhoneValidator
{
    public static bool IsValid(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return false;

        var clean = Client.NormalizePhone(phone);
        return clean.Length == 10 && clean.All(char.IsDigit);
    }
}

public class CreateClientDtoValidator : AbstractValidator<CreateClientDto>
{
    public CreateClientDtoValidator()
    {
        RuleFor(x => x.Name).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Ingrese el nombre del cliente.")
            .MaximumLength(255).WithMessage("El nombre no puede superar los 255 caracteres.")
            .Must(name => name == name.Trim()).WithMessage("El nombre no debe contener espacios al inicio ni al final.");

        RuleFor(x => x.Rfc).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Ingrese el RFC.")
            .Must(RfcValidator.IsValid).WithMessage("El formato del RFC no es válido o contiene una fecha inexistente.");

        RuleFor(x => x.Phone).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Ingrese el teléfono.")
            .Must(PhoneValidator.IsValid).WithMessage("El teléfono debe contener 10 dígitos numéricos.");

        RuleFor(x => x.Email).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Ingrese el correo.")
            .MaximumLength(255).WithMessage("El correo no puede superar los 255 caracteres.")
            .EmailAddress().WithMessage("El correo no es válido.");
    }
}

public class UpdateClientDtoValidator : AbstractValidator<UpdateClientDto>
{
    public UpdateClientDtoValidator()
    {
        RuleFor(x => x.Name).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Ingrese el nombre del cliente.")
            .MaximumLength(255).WithMessage("El nombre no puede superar los 255 caracteres.")
            .Must(name => name == name.Trim()).WithMessage("El nombre no debe contener espacios al inicio ni al final.");

        RuleFor(x => x.Rfc).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Ingrese el RFC.")
            .Must(RfcValidator.IsValid).WithMessage("El formato del RFC no es válido o contiene una fecha inexistente.");

        RuleFor(x => x.Phone).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Ingrese el teléfono.")
            .Must(PhoneValidator.IsValid).WithMessage("El teléfono debe contener 10 dígitos numéricos.");

        RuleFor(x => x.Email).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Ingrese el correo.")
            .MaximumLength(255).WithMessage("El correo no puede superar los 255 caracteres.")
            .EmailAddress().WithMessage("El correo no es válido.");
    }
}

public class ClientListQueryDtoValidator : AbstractValidator<ClientListQueryDto>
{
    public ClientListQueryDtoValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("La página debe ser mayor o igual a 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("El tamaño de página debe estar entre 1 y 100.");
    }
}

