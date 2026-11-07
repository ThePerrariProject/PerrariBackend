using OtpNet;
using Perrari.Application.Interfaces.Services;

namespace Perrari.Infrastructure.Security;

/// <summary>TOTP (RFC 6238) compatible con Google Authenticator, Microsoft Authenticator, Authy, etc.</summary>
public class TotpService : ITotpService
{
    private const string Issuer = "Perrari";

    public string GenerateSecret() => Base32Encoding.ToString(KeyGeneration.GenerateRandomKey(20));

    public string BuildOtpAuthUri(string email, string secretBase32)
    {
        var issuer = Uri.EscapeDataString(Issuer);
        var account = Uri.EscapeDataString(email);
        return $"otpauth://totp/{issuer}:{account}?secret={secretBase32}&issuer={issuer}&algorithm=SHA1&digits=6&period=30";
    }

    public bool VerifyCode(string secretBase32, string code, out long step)
    {
        var totp = new Totp(Base32Encoding.ToBytes(secretBase32));
        return totp.VerifyTotp(code, out step, new VerificationWindow(previous: 1, future: 1));
    }
}
