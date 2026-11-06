namespace Perrari.Application.Interfaces.Services;

public interface ICurrentUserService
{
    Guid? UserId { get; }

    string? IpAddress { get; }

    string? UserAgent { get; }
}