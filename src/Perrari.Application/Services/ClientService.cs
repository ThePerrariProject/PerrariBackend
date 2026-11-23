using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Perrari.Application.Common;
using Perrari.Application.Common.Exceptions;
using Perrari.Application.DTOs.Clientes;
using Perrari.Application.Interfaces.Repositories;
using Perrari.Application.Interfaces.Services;
using Perrari.Domain.Entities.Clientes;

namespace Perrari.Application.Services;

public class ClientService(
    IClientRepository clients,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IValidator<CreateClientDto> createValidator,
    IValidator<UpdateClientDto> updateValidator,
    IValidator<ClientListQueryDto> listValidator) : IClientService
{
    private const string MsgNotFound = "Cliente no encontrado.";
    private const string MsgRfcConflict = "Ya existe un cliente con ese RFC.";
    private const string MsgDeactivatedConflict = "No se puede editar un cliente desactivado.";

    private DateTime Now => timeProvider.GetUtcNow().UtcDateTime;

    #region Consultas

    public async Task<PagedResult<ClientListItemDto>> ListAsync(ClientListQueryDto query, CancellationToken ct = default)
    {
        await listValidator.ValidateAndThrowAsync(query, ct);

        var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
        var (items, total) = await clients.SearchAsync(
            new ClientSearchCriteria(search, query.IsActive, query.Page, query.PageSize), ct);

        var rows = items
            .Select(c => new ClientListItemDto(
                c.Id,
                c.Name,
                c.Rfc,
                c.Phone,
                c.Email,
                c.IsActive,
                c.CreatedAt))
            .ToList();

        return new PagedResult<ClientListItemDto>(rows, query.Page, query.PageSize, total);
    }

    public async Task<ClientDetailDto> GetAsync(Guid id, CancellationToken ct = default)
        => ToDetail(await GetClientEntityAsync(id, ct));

    #endregion

    #region Crear y Editar

    public async Task<ClientDetailDto> CreateAsync(CreateClientDto dto, CancellationToken ct = default)
    {
        await createValidator.ValidateAndThrowAsync(dto, ct);

        var now = Now;
        var normalizedRfc = Client.NormalizeRfc(dto.Rfc);

        if (await clients.RfcExistsAsync(normalizedRfc, null, ct))
            throw new ConflictException(MsgRfcConflict);

        var client = Client.Create(dto.Name, normalizedRfc, dto.Phone, dto.Email, now);

        await clients.AddAsync(client, ct);

        try
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Atrapa colisiones en caso de altas concurrentes simultáneas que violen el índice único
            throw new ConflictException(MsgRfcConflict);
        }

        return ToDetail(client);
    }

    public async Task<ClientDetailDto> UpdateAsync(Guid id, UpdateClientDto dto, CancellationToken ct = default)
    {
        await updateValidator.ValidateAndThrowAsync(dto, ct);

        var now = Now;
        var client = await GetClientEntityAsync(id, ct);

        if (!client.IsActive)
            throw new ConflictException(MsgDeactivatedConflict);

        var normalizedRfc = Client.NormalizeRfc(dto.Rfc);

        if (normalizedRfc != client.Rfc && await clients.RfcExistsAsync(normalizedRfc, client.Id, ct))
            throw new ConflictException(MsgRfcConflict);

        client.Update(dto.Name, normalizedRfc, dto.Phone, dto.Email, now);

        try
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            throw new ConflictException(MsgRfcConflict);
        }

        return ToDetail(client);
    }

    #endregion

    #region Estado

    public async Task DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        var client = await GetClientEntityAsync(id, ct);

        if (!client.IsActive)
            return; // Idempotente

        client.Deactivate(Now);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task ReactivateAsync(Guid id, CancellationToken ct = default)
    {
        var client = await GetClientEntityAsync(id, ct);

        if (client.IsActive)
            return; // Idempotente

        client.Reactivate(Now);
        await unitOfWork.SaveChangesAsync(ct);
    }

    #endregion

    #region Helpers Privados

    private async Task<Client> GetClientEntityAsync(Guid id, CancellationToken ct)
    {
        var client = await clients.GetByIdAsync(id, ct);
        if (client is null)
            throw new NotFoundException(MsgNotFound);

        return client;
    }

    private static ClientDetailDto ToDetail(Client c)
        => new(c.Id, c.Name, c.Rfc, c.Phone, c.Email, c.IsActive, c.CreatedAt, c.UpdatedAt);

    #endregion
}

