using FluentValidation;
using Perrari.Application.Common;
using Perrari.Application.DTOs.Auth;
using Perrari.Application.DTOs.Usuarios;

namespace Perrari.Application.Validators.Users;

public static class PasswordRules
{
    public static IRuleBuilderOptions<T, string> MustBeStrongPassword<T>(this IRuleBuilder<T, string> rule)
        => rule
            .NotEmpty().WithMessage("Ingrese una contrasena")
            .MinimumLength(PasswordPolicy.MinLength).WithMessage("La debe tener al menos 12 caracteres")
            .MaximumLength(PasswordPolicy.MaxLength).WithMessage("La contrasena no debe superar los 128 caracteres")
            .Matches("[A-Z]").WithMessage("La contrasena debe incluir al menos una mayuscula")
            .Matches("[a-z]").WithMessage("La contrasena debe incluir al menos una minuscula")
            .Matches("[0-9]").WithMessage("La contrasena debe incluir al menos un numero")
            .Matches("[^a-zA-Z0-9]").WithMessage("La contraseña debe incluir al menos un símbolo.");
}

public class CreateUserDtoValidator : AbstractValidator<CreateUserDto>
{
    public CreateUserDtoValidator()
    {
        RuleFor(x => x.Username).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Ingrese el nombre de usuario")
            .MaximumLength(255).WithMessage("El nombre de usuario no puede superar los 255 caracteres");

        RuleFor(x => x.Email).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Ingrese el correo")
            .MaximumLength(255).WithMessage("El correo no pueede superar los 255 caracteres")
            .EmailAddress().WithMessage("El correo no es valido");

        RuleFor(x => x.Password).Cascade(CascadeMode.Stop)
            .MustBeStrongPassword()
            .Must((dto, password) => !PasswordPolicy.MatchesIdentity(password, dto.Email ?? "", dto.Username ?? ""))
            .WithMessage(PasswordPolicy.IdentityMessage);

        RuleFor(x => x.RoleIds).Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Seleccione al menos un rol")
            .Must(ids => ids.Count > 0).WithMessage("Seleccione al menos un rol");
    }
}

public class UpdateUserDtoValidator : AbstractValidator<UpdateUserDto>
{
    public UpdateUserDtoValidator()
    {
        RuleFor(x => x.Username).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Ingrese el nombre de usuario.")
            .MaximumLength(255).WithMessage("El nombre de usuario no puede superar los 255 caracteres.");

        RuleFor(x => x.Email).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Ingrese el correo.")
            .MaximumLength(255).WithMessage("El correo no puede superar los 255 caracteres.")
            .EmailAddress().WithMessage("El correo no es válido.");
    }
}

public class SetUserRolesDtoValidator : AbstractValidator<SetUserRolesDto>
{
    public SetUserRolesDtoValidator()
    {
        RuleFor(x => x.RoleIds).Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Seleccione al menos un rol.")
            .Must(ids => ids.Count > 0).WithMessage("Seleccione al menos un rol.");
    }
}

public class UserListQueryDtoValidator : AbstractValidator<UserListQueryDto>
{
    public const int MaxPageSize = 100;

    public UserListQueryDtoValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("La página debe ser 1 o mayor.");
        RuleFor(x => x.PageSize).InclusiveBetween(1, MaxPageSize).WithMessage($"El tamaño de página debe estar entre 1 y {MaxPageSize}.");
        RuleFor(x => x.Search).MaximumLength(100).WithMessage("La búsqueda no puede superar los 100 caracteres.");
    }
}

public class ChangePasswordDtoValidator : AbstractValidator<ChangePasswordDto>
{
    public ChangePasswordDtoValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().WithMessage("Complete los campos requeridos para continuar.")
            .MaximumLength(PasswordPolicy.MaxLength);
        RuleFor(x => x.NewPassword).Cascade(CascadeMode.Stop).MustBeStrongPassword();
    }
}

public class SetRolePermissionsDtoValidator : AbstractValidator<SetRolePermissionsDto>
{
    public SetRolePermissionsDtoValidator()
    {
        RuleFor(x => x.PermissionIds).NotNull().WithMessage("Envíe la lista de permisos.");
    }
}