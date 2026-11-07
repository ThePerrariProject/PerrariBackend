using Perrari.Application.Interfaces.Services;

namespace Perrari.Infrastructure.Security;

public class PasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    // Se calcula una sola vez; sirve para igualar tiempos cuando el correo no existe
    private static readonly string FakeHash = BCrypt.Net.BCrypt.HashPassword("perrari-fake-hash", WorkFactor);

    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);

    public void SimulateVerification(string password) => BCrypt.Net.BCrypt.Verify(password, FakeHash);
}
