namespace Perrari.Application.Interfaces.Services;

public interface IRefreshTokenGenerator
{
    TimeSpan Lifetime { get; }

    (string Token, string Hash) Generate();

    string Hash(string token);
}
