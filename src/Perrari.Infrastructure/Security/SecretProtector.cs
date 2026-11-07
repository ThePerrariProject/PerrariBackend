using Microsoft.AspNetCore.DataProtection;
using Perrari.Application.Interfaces.Services;

namespace Perrari.Infrastructure.Security;

public class SecretProtector(IDataProtectionProvider provider) : ISecretProtector
{
    private readonly IDataProtector _protector = provider.CreateProtector("Perrari.Auth.TotpSecret.v1");

    public string Protect(string plainText) => _protector.Protect(plainText);

    public string Unprotect(string protectedText) => _protector.Unprotect(protectedText);
}
