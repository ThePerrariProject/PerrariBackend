namespace Perrari.Application.Common;

public static class PasswordPolicy
{
    public const int MinLength = 12;
    public const int MaxLength = 128;

    public const string IdentityMessage = "La contrasena no puede ser igual al correo o nombre de usuario";

    public static bool MatchesIdentity(string password, string email, string username)
    {
        var at = email.IndexOf('@');
        var localPart = at > 0 ? email[..at] : email;

        return password.Equals(email, StringComparison.OrdinalIgnoreCase)
            || password.Equals(localPart, StringComparison.OrdinalIgnoreCase)
            || password.Equals(username, StringComparison.OrdinalIgnoreCase);
    }
}