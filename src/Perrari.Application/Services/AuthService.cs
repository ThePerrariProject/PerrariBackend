using FluentValidation;
using FluentValidation.Results;
using Perrari.Application.Common.Exceptions;
using Perrari.Application.DTOs.Auth;
using Perrari.Application.Interfaces.Repositories;
using Perrari.Application.Interfaces.Services;
using Perrari.Domain.Entities.Security;

namespace Perrari.Application.Services;

public class AuthService(
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    IAuthEventRepository authEvents,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwt,
    IRefreshTokenGenerator refreshGenerator,
    ITotpService totp,
    ISecretProtector protector,
    IRecoveryCodeService recoveryCodes,
    ICurrentUserService currentUser,
    TimeProvider timeProvider,
    IValidator<LoginDto> loginValidator,
    IValidator<VerifyTwoFactorDto> verifyValidator,
    IValidator<EnableTwoFactorDto> enableValidator,
    IValidator<DisableTwoFactorDto> disableValidator,
    IValidator<RefreshTokenDto> refreshValidator) : IAuthService
{
    // Mensajes tomados de la documentacion de contenido (pantalla de Login)
    private const string MsgInvalidCredentials = "Correo o contraseña incorrectos. Verifique sus datos.";
    private const string MsgVerificationExpired = "La verificación expiró. Inicie sesión nuevamente.";
    private const string MsgWrongCode = "Código de verificación incorrecto.";
    private const string MsgAccountLocked = "Cuenta bloqueada temporalmente por demasiados intentos fallidos. Intente más tarde.";
    private const string MsgSessionExpired = "Su sesión ha expirado. Ingrese nuevamente.";
    private const int RecoveryCodesCount = 8;

    private enum CodeCheck { Invalid, Totp, RecoveryCode }

    private DateTime Now => timeProvider.GetUtcNow().UtcDateTime;

    #region Paso 1. Login correo / pass

    public async Task<LoginResponseDto> LoginAsync(LoginDto dto, CancellationToken ct = default)
    {
        await loginValidator.ValidateAndThrowAsync(dto, ct);

        var now = Now;
        var user = await users.GetByEmailAsync(dto.Email.Trim().ToLowerInvariant(), ct);

        if (user is null)
        {
            passwordHasher.SimulateVerification(dto.Password); // mismo tiempo de respuesta que un usuario real
            await RecordAsync(null, AuthEventTypes.LoginFailed, AuthEventStatuses.Failure, now, ct);
            await unitOfWork.SaveChangesAsync(ct);
            throw new UnauthorizedException(MsgInvalidCredentials);
        }

        if (user.IsLocked(now))
        {
            await RecordAsync(user.Id, AuthEventTypes.LoginBlocked, AuthEventStatuses.Failure, now, ct);
            await unitOfWork.SaveChangesAsync(ct);
            throw new AccountLockedException(MsgAccountLocked);
        }

        if (!passwordHasher.Verify(dto.Password, user.PasswordHash))
        {
            await RegisterFailureAsync(user, AuthEventTypes.LoginFailed, now, ct);
            throw new UnauthorizedException(MsgInvalidCredentials);
        }

        // Con 2FA activo todavia no hay sesion: se entrega un token temporal para el paso 2.
        // El contador de intentos NO se reinicia aqui (ver User.RegisterSuccessfulLogin).
        if (user.TwoFactorEnabled)
        {
            await RecordAsync(user.Id, AuthEventTypes.TwoFactorRequired, AuthEventStatuses.Success, now, ct);
            await unitOfWork.SaveChangesAsync(ct);
            return new LoginResponseDto(true, jwt.GenerateTwoFactorToken(user), null);
        }

        var session = await IssueSessionAsync(user, now, AuthEventTypes.LoginSuccess, resetLoginCounters: true, ct);
        return new LoginResponseDto(false, null, session);
    }

    #endregion
    #region Paso 2. TOTP

    public async Task<AuthSessionDto> VerifyTwoFactorAsync(VerifyTwoFactorDto dto, CancellationToken ct = default)
    {
        await verifyValidator.ValidateAndThrowAsync(dto, ct);

        var now = Now;
        var userId = await jwt.ValidateTwoFactorTokenAsync(dto.TwoFactorToken)
                     ?? throw new UnauthorizedException(MsgVerificationExpired);

        var user = await users.GetByIdAsync(userId, ct);
        if (user is null || !user.TwoFactorEnabled)
            throw new UnauthorizedException(MsgVerificationExpired);

        if (user.IsLocked(now))
        {
            await RecordAsync(user.Id, AuthEventTypes.LoginBlocked, AuthEventStatuses.Failure, now, ct);
            await unitOfWork.SaveChangesAsync(ct);
            throw new AccountLockedException(MsgAccountLocked);
        }

        var check = CheckCode(user, dto.Code, now);
        if (check == CodeCheck.Invalid)
        {
            await RegisterFailureAsync(user, AuthEventTypes.TwoFactorFailed, now, ct);
            throw new UnauthorizedException(MsgWrongCode);
        }

        if (check == CodeCheck.RecoveryCode)
            await RecordAsync(user.Id, AuthEventTypes.RecoveryCodeUsed, AuthEventStatuses.Success, now, ct);

        // Esto guarda tambien el paso TOTP usado o el codigo de recuperacion consumido
        return await IssueSessionAsync(user, now, AuthEventTypes.LoginSuccess, resetLoginCounters: true, ct);
    }

    #endregion
    #region Renovar / Cerrar sesion

    public async Task<AuthSessionDto> RefreshAsync(RefreshTokenDto dto, CancellationToken ct = default)
    {
        await refreshValidator.ValidateAndThrowAsync(dto, ct);

        var now = Now;
        var stored = await refreshTokens.GetByHashAsync(refreshGenerator.Hash(dto.RefreshToken), ct)
                     ?? throw new UnauthorizedException(MsgSessionExpired);

        if (stored.RevokedAt.HasValue)
        {
            // Un token ya rotado o cerrado volvio a presentarse: posible robo, se cierran todas las sesiones del usuario
            await refreshTokens.RevokeAllActiveAsync(stored.UserId, now, ct);
            await RecordAsync(stored.UserId, AuthEventTypes.TokenReuseDetected, AuthEventStatuses.Failure, now, ct);
            await unitOfWork.SaveChangesAsync(ct);
            throw new UnauthorizedException(MsgSessionExpired);
        }

        if (stored.ExpiresAt <= now)
            throw new UnauthorizedException(MsgSessionExpired);

        var user = await users.GetByIdAsync(stored.UserId, ct)
                   ?? throw new UnauthorizedException(MsgSessionExpired);

        if (user.IsLocked(now))
            throw new AccountLockedException(MsgAccountLocked);

        stored.Revoke(now);
        return await IssueSessionAsync(user, now, AuthEventTypes.TokenRefreshed, resetLoginCounters: false, ct);
    }

    public async Task LogoutAsync(RefreshTokenDto dto, CancellationToken ct = default)
    {
        await refreshValidator.ValidateAndThrowAsync(dto, ct);

        var now = Now;
        var stored = await refreshTokens.GetByHashAsync(refreshGenerator.Hash(dto.RefreshToken), ct);
        if (stored is null)
            return;

        stored.Revoke(now);
        await RecordAsync(stored.UserId, AuthEventTypes.Logout, AuthEventStatuses.Success, now, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }

    #endregion
    #region Perfil

    public async Task<UserDto> GetProfileAsync(CancellationToken ct = default)
        => ToDto(await GetCurrentUserAsync(ct));

    #endregion

    #region Setup 2FA

    public async Task<SetupTwoFactorResponseDto> SetupTwoFactorAsync(CancellationToken ct = default)
    {
        var user = await GetCurrentUserAsync(ct);

        if (user.TwoFactorEnabled)
            throw new ConflictException("La autenticación de dos factores ya está activa.");

        var secret = totp.GenerateSecret();
        user.BeginTotpSetup(protector.Protect(secret), Now);
        await unitOfWork.SaveChangesAsync(ct);

        return new SetupTwoFactorResponseDto(secret, totp.BuildOtpAuthUri(user.Email, secret));
    }

    public async Task<RecoveryCodesResponseDto> EnableTwoFactorAsync(EnableTwoFactorDto dto, CancellationToken ct = default)
    {
        await enableValidator.ValidateAndThrowAsync(dto, ct);

        var now = Now;
        var user = await GetCurrentUserAsync(ct);

        if (user.TwoFactorEnabled)
            throw new ConflictException("La autenticación de dos factores ya está activa.");

        var encrypted = user.Totp?.SecretEncrypted;
        if (string.IsNullOrEmpty(encrypted))
            throw new ConflictException("Primero debe iniciar la configuración de dos factores.");

        if (!totp.VerifyCode(protector.Unprotect(encrypted), dto.Code.Trim(), out var step))
            throw FieldError(nameof(dto.Code), MsgWrongCode); // 400, no 401: el usuario SI esta autenticado

        var codes = recoveryCodes.Generate(RecoveryCodesCount);
        user.ConfirmTotp(step, codes.Select(recoveryCodes.Hash), now);

        await RecordAsync(user.Id, AuthEventTypes.TwoFactorEnabled, AuthEventStatuses.Success, now, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return new RecoveryCodesResponseDto(codes);
    }

    public async Task DisableTwoFactorAsync(DisableTwoFactorDto dto, CancellationToken ct = default)
    {
        await disableValidator.ValidateAndThrowAsync(dto, ct);

        var now = Now;
        var user = await GetCurrentUserAsync(ct);

        if (!user.TwoFactorEnabled)
            throw new ConflictException("La autenticación de dos factores no está activa.");
        if (user.IsLocked(now))
            throw new AccountLockedException(MsgAccountLocked);

        if (!passwordHasher.Verify(dto.Password, user.PasswordHash))
        {
            await RegisterFailureAsync(user, AuthEventTypes.LoginFailed, now, ct);
            throw FieldError(nameof(dto.Password), "Contraseña incorrecta.");
        }

        if (CheckCode(user, dto.Code, now) == CodeCheck.Invalid)
        {
            await RegisterFailureAsync(user, AuthEventTypes.TwoFactorFailed, now, ct);
            throw FieldError(nameof(dto.Code), MsgWrongCode);
        }

        user.DisableTwoFactor(now);
        await RecordAsync(user.Id, AuthEventTypes.TwoFactorDisabled, AuthEventStatuses.Success, now, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }

    #endregion
    #region Metodos privados

    private async Task<AuthSessionDto> IssueSessionAsync(
        User user, DateTime now, string eventType, bool resetLoginCounters, CancellationToken ct)
    {
        if (resetLoginCounters)
            user.RegisterSuccessfulLogin(now);

        var (accessToken, accessExpiresAt) = jwt.GenerateAccessToken(user);

        var (refreshToken, refreshHash) = refreshGenerator.Generate();
        var refreshExpiresAt = now.Add(refreshGenerator.Lifetime);
        await refreshTokens.AddAsync(RefreshToken.Create(user.Id, refreshHash, refreshExpiresAt, now), ct);

        await RecordAsync(user.Id, eventType, AuthEventStatuses.Success, now, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return new AuthSessionDto(accessToken, accessExpiresAt, refreshToken, refreshExpiresAt, ToDto(user));
    }

    /// <summary>Suma un intento fallido, registra el evento (y LOCKED si este intento bloqueo la cuenta) y guarda. El llamador lanza la excepcion.</summary>
    private async Task RegisterFailureAsync(User user, string eventType, DateTime now, CancellationToken ct)
    {
        var wasLocked = user.IsLocked(now);
        user.RegisterFailedLogin(now);

        await RecordAsync(user.Id, eventType, AuthEventStatuses.Failure, now, ct);
        if (!wasLocked && user.IsLocked(now))
            await RecordAsync(user.Id, AuthEventTypes.Locked, AuthEventStatuses.Failure, now, ct);

        await unitOfWork.SaveChangesAsync(ct);
    }

    /// <summary>Acepta un codigo TOTP de 6 digitos o un codigo de recuperacion. Modifica el usuario (paso usado / codigo consumido); el llamador guarda.</summary>
    private CodeCheck CheckCode(User user, string code, DateTime now)
    {
        var clean = code.Trim();

        if (clean.Length == 6 && clean.All(char.IsAsciiDigit))
        {
            var encrypted = user.Totp?.SecretEncrypted;
            if (string.IsNullOrEmpty(encrypted))
                return CodeCheck.Invalid;

            if (!totp.VerifyCode(protector.Unprotect(encrypted), clean, out var step))
                return CodeCheck.Invalid;

            if (user.IsTotpStepUsed(step)) // un codigo TOTP no se puede reutilizar
                return CodeCheck.Invalid;

            user.RegisterTotpStep(step);
            return CodeCheck.Totp;
        }

        return user.ConsumeRecoveryCode(recoveryCodes.Hash(clean), now)
            ? CodeCheck.RecoveryCode
            : CodeCheck.Invalid;
    }

    private Task RecordAsync(Guid? userId, string eventType, string status, DateTime now, CancellationToken ct)
        => authEvents.AddAsync(
            AuthEvent.Create(userId, eventType, currentUser.IpAddress ?? "unknown", currentUser.UserAgent, status, now),
            ct);

    private async Task<User> GetCurrentUserAsync(CancellationToken ct)
    {
        var id = currentUser.UserId ?? throw new UnauthorizedException(MsgSessionExpired);
        return await users.GetByIdAsync(id, ct) ?? throw new UnauthorizedException(MsgSessionExpired);
    }

    private static ValidationException FieldError(string field, string message)
        => new(new[] { new ValidationFailure(field, message) });

    private static UserDto ToDto(User user)
        => new(user.Id, user.Username, user.Email, user.GetRoleNames(), user.TwoFactorEnabled);

    #endregion
}
