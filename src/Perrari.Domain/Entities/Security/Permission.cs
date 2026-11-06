using Perrari.Domain.Common;
using Perrari.Domain.Exceptions;

namespace Perrari.Domain.Entities.Security;

public class Permission : BaseEntity
{
    private Permission() { }

    public string Module { get; private set; } = default!;
    public string Action { get; private set; } = default!;
    public string? Description { get; private set; }

    public string Code => $"{Module}:{Action}";

    public static Permission Create(string module, string action, string? description)
    {
        if (string.IsNullOrWhiteSpace(module) || string.IsNullOrWhiteSpace(action))
            throw new DomainException("El modulo y la accion del permiso son obligatorios");

        return new Permission
        {
            Module = module.Trim().ToUpperInvariant(),
            Action = action.Trim().ToUpperInvariant(),
            Description = description
        };
    }
}