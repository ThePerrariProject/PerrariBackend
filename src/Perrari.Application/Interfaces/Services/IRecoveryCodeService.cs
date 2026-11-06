namespace Perrari.Application.Interfaces.Services;

public interface IRecoveryCodeService
{
    IReadOnlyList<string> Generate(int count);

    string Hash(string code);
}
