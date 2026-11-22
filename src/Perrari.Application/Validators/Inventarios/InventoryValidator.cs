using FluentValidation;
using Perrari.Application.DTOs.Inventarios;
using Perrari.Domain.Entities.Inventarios;

namespace Perrari.Application.Validators.Inventarios;

public class BranchInputDtoValidator : AbstractValidator<BranchInputDto>
{
    public BranchInputDtoValidator()
    {
        RuleFor(x => x.Name).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Ingrese el nombre de la sucursal.")
            .MaximumLength(150).WithMessage("El nombre no puede superar los 150 caracteres.");

        RuleFor(x => x.Address)
            .MaximumLength(500).WithMessage("La dirección no puede superar los 500 caracteres.");

        RuleFor(x => x.Phone)
            .MaximumLength(50).WithMessage("El teléfono no puede superar los 50 caracteres.");
    }
}

/// <summary>Reglas compartidas entre alta y edicion de vehiculos.</summary>
public static class VehicleRules
{
    public static IRuleBuilderOptions<T, string> MustBeText<T>(this IRuleBuilderInitial<T, string> rule, string label, int max)
        => rule.Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage($"Ingrese {label}.")
            .MaximumLength(max).WithMessage($"{char.ToUpper(label[0])}{label[1..]} no puede superar los {max} caracteres.");

    public static IRuleBuilderOptions<T, int> MustBeValidYear<T>(this IRuleBuilder<T, int> rule)
        => rule.InclusiveBetween(Vehicle.MinYear, DateTime.UtcNow.Year + 1)
            .WithMessage($"El año debe estar entre {Vehicle.MinYear} y {DateTime.UtcNow.Year + 1}.");

    public static IRuleBuilderOptions<T, decimal> MustBeMoney<T>(this IRuleBuilderInitial<T, decimal> rule, string label)
        => rule.Cascade(CascadeMode.Stop)
            .GreaterThan(0).WithMessage($"{label} debe ser mayor a cero.")
            .PrecisionScale(12, 2, false).WithMessage($"{label} admite hasta 10 enteros y 2 decimales.");
}

public class CreateVehicleDtoValidator : AbstractValidator<CreateVehicleDto>
{
    public CreateVehicleDtoValidator()
    {
        RuleFor(x => x.Vin).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Ingrese el VIN.")
            .Must(v => Vehicle.IsValidVin(Vehicle.NormalizeVin(v)))
            .WithMessage("El VIN debe tener 17 caracteres (letras y números, sin I, O ni Q).");

        RuleFor(x => x.Brand).MustBeText("la marca", 100);
        RuleFor(x => x.Model).MustBeText("el modelo", 100);
        RuleFor(x => x.Year).MustBeValidYear();
        RuleFor(x => x.Color).MaximumLength(50).WithMessage("El color no puede superar los 50 caracteres.");
        RuleFor(x => x.PurchasePrice).MustBeMoney("El precio de compra");
        RuleFor(x => x.SalePrice).MustBeMoney("El precio de venta");

        // Opcional: si viene null los validadores lo omiten
        RuleFor(x => x.RentDailyRate).Cascade(CascadeMode.Stop)
            .GreaterThan(0).WithMessage("La tarifa de renta debe ser mayor a cero.")
            .PrecisionScale(10, 2, false).WithMessage("La tarifa de renta admite hasta 8 enteros y 2 decimales.");

        RuleFor(x => x.BranchId).NotEmpty().WithMessage("Seleccione la sucursal.");
    }
}

public class UpdateVehicleDtoValidator : AbstractValidator<UpdateVehicleDto>
{
    public UpdateVehicleDtoValidator()
    {
        RuleFor(x => x.Brand).MustBeText("la marca", 100);
        RuleFor(x => x.Model).MustBeText("el modelo", 100);
        RuleFor(x => x.Year).MustBeValidYear();
        RuleFor(x => x.Color).MaximumLength(50).WithMessage("El color no puede superar los 50 caracteres.");
        RuleFor(x => x.SalePrice).MustBeMoney("El precio de venta");

        // Opcional: si viene null los validadores lo omiten
        RuleFor(x => x.RentDailyRate).Cascade(CascadeMode.Stop)
            .GreaterThan(0).WithMessage("La tarifa de renta debe ser mayor a cero.")
            .PrecisionScale(10, 2, false).WithMessage("La tarifa de renta admite hasta 8 enteros y 2 decimales.");
    }
}

public class ChangeVehicleStatusDtoValidator : AbstractValidator<ChangeVehicleStatusDto>
{
    public ChangeVehicleStatusDtoValidator()
    {
        RuleFor(x => x.Status).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Seleccione el estado.")
            .Must(s => VehicleStatuses.Manual.Contains(s))
            .WithMessage("El estado no es válido. Use AVAILABLE, IN_MAINTENANCE o DECOMMISSIONED.");
    }
}

public class TransferVehicleDtoValidator : AbstractValidator<TransferVehicleDto>
{
    public TransferVehicleDtoValidator()
    {
        RuleFor(x => x.DestinationBranchId).NotEmpty().WithMessage("Seleccione la sucursal de destino.");
    }
}

public class VehicleListQueryDtoValidator : AbstractValidator<VehicleListQueryDto>
{
    public VehicleListQueryDtoValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("La página debe ser 1 o mayor.");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage("El tamaño de página debe estar entre 1 y 100.");
        RuleFor(x => x.Search).MaximumLength(100).WithMessage("La búsqueda no puede superar los 100 caracteres.");
        RuleFor(x => x.Status).Must(s => s is null || VehicleStatuses.All.Contains(s))
            .WithMessage("El estado del filtro no es válido.");
    }
}

public class MovementListQueryDtoValidator : AbstractValidator<MovementListQueryDto>
{
    public MovementListQueryDtoValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("La página debe ser 1 o mayor.");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage("El tamaño de página debe estar entre 1 y 100.");
    }
}
