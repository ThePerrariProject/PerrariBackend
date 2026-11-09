namespace Perrari.Application.Interfaces.Services;

public interface ITotpService
{
    string GenerateSecret();

    string BuildOtpAuthUri(string email, string secretBase32);

    bool VerifyCode(string secretBase32, string code, out long step);
}
