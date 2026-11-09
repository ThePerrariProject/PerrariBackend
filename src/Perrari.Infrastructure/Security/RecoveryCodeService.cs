using System.Security.Cryptography;
using System.Text;
using Perrari.Application.Interfaces.Services;

namespace Perrari.Infrastructure.Security;

public class RecoveryCodeService : IRecoveryCodeService
{
    // Sin 0/O ni 1/I para evitar confusiones al transcribir
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public IReadOnlyList<string> Generate(int count)
    {
        var codes = new List<string>(count);
        for (var i = 0; i < count; i++)
        {
            var c = RandomNumberGenerator.GetString(Alphabet, 10);
            codes.Add($"{c[..5]}-{c[5..]}");
        }
        return codes;
    }

    public string Hash(string code)
    {
        var normalized = code.Replace("-", "").Replace(" ", "").Trim().ToUpperInvariant();
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
    }
}
