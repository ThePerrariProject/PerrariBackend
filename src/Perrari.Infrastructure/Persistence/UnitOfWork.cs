using Microsoft.EntityFrameworkCore;
using Npgsql;
using Perrari.Application.Common.Exceptions;
using Perrari.Application.Interfaces.Repositories;

namespace Perrari.Infrastructure.Persistence;

public class UnitOfWork(PerrariDbContext db) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            return await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Otra persona modifico la misma fila entre que se leyo y se guardo (token updated_at)
            throw new ConflictException("El registro fue modificado por otra persona. Recargue e intente de nuevo.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Respaldo para carreras: dos altas con el mismo valor unico (VIN, correo) a la vez
            throw new ConflictException("Ya existe un registro con esos datos.");
        }
    }
}
