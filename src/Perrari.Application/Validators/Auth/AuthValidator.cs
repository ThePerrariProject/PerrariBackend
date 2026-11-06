using FluentValidation;
using Perrari.Application.DTOs.Auth;

namespace Perrari.Application.Validators.Auth;

internal static class AuthMessages
{
    public const string Required = "Complete los campos requeridos para continuar.";
}

public class LoginDtoValidator : AbstractValidator<LoginDto>
{
    public LoginDtoValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage(AuthMessages.Required).MaximumLength(255);
        RuleFor(x => x.Password).NotEmpty().WithMessage(AuthMessages.Required).MaximumLength(128);
    }
}

public class VerifyTwoFactorDtoValidator : AbstractValidator<VerifyTwoFactorDto>
{
    public VerifyTwoFactorDtoValidator()
    {
        RuleFor(x => x.TwoFactorToken).NotEmpty().WithMessage(AuthMessages.Required);
        // 6 digitos (TOTP) o 11 caracteres (codigo de recuperacion XXXXX-XXXXX)
        RuleFor(x => x.Code).NotEmpty().WithMessage(AuthMessages.Required).MaximumLength(20);
    }
}

public class EnableTwoFactorDtoValidator : AbstractValidator<EnableTwoFactorDto>
{
    public EnableTwoFactorDtoValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Ingrese el código de su aplicación autenticadora.")
            .Matches(@"^\d{6}$").WithMessage("El código debe tener 6 dígitos.");
    }
}

public class DisableTwoFactorDtoValidator : AbstractValidator<DisableTwoFactorDto>
{
    public DisableTwoFactorDtoValidator()
    {
        RuleFor(x => x.Password).NotEmpty().WithMessage(AuthMessages.Required).MaximumLength(128);
        RuleFor(x => x.Code).NotEmpty().WithMessage(AuthMessages.Required).MaximumLength(20);
    }
}

public class RefreshTokenDtoValidator : AbstractValidator<RefreshTokenDto>
{
    public RefreshTokenDtoValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().WithMessage(AuthMessages.Required).MaximumLength(200);
    }
}
