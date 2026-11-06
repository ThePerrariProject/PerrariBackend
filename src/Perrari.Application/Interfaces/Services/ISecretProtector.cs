namespace Perrari.Application.Interfaces.Services;

public interface ISecretProtector
{
    string Protect(string plainText);

    string Unprotect(string protectedText);
}
