using Perrari.Application.Common.Exceptions;
using Perrari.Application.DTOs.Clientes;
using Perrari.Application.Interfaces.Repositories;
using Perrari.Application.Services;
using Perrari.Application.Validators.Clientes;
using Perrari.Domain.Entities.Clientes;
using Xunit;

namespace Perrari.Application.Tests;

internal sealed class FakeClientRepository : IClientRepository
{
    public List<Client> Store { get; } = [];

    public Task<Client?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(Store.FirstOrDefault(c => c.Id == id));

    public Task<Client?> GetByRfcAsync(string rfc, CancellationToken ct = default)
        => Task.FromResult(Store.FirstOrDefault(c => c.Rfc == rfc));

    public Task<bool> RfcExistsAsync(string rfc, Guid? excludeClientId = null, CancellationToken ct = default)
        => Task.FromResult(Store.Any(c => c.Rfc == rfc && (excludeClientId == null || c.Id != excludeClientId)));

    public Task AddAsync(Client client, CancellationToken ct = default)
    {
        Store.Add(client);
        return Task.CompletedTask;
    }

    public Task<(IReadOnlyList<Client> Items, int TotalCount)> SearchAsync(ClientSearchCriteria criteria, CancellationToken ct = default)
    {
        var query = Store.AsQueryable();

        if (!string.IsNullOrWhiteSpace(criteria.Search))
        {
            var s = criteria.Search.Trim().ToLowerInvariant();
            query = query.Where(c => c.Name.ToLowerInvariant().Contains(s) || c.Rfc.ToLowerInvariant().Contains(s));
        }

        if (criteria.IsActive.HasValue)
            query = query.Where(c => c.IsActive == criteria.IsActive.Value);

        var list = query.OrderBy(c => c.Name).ToList();
        var items = list.Skip((criteria.Page - 1) * criteria.PageSize).Take(criteria.PageSize).ToList();

        return Task.FromResult(((IReadOnlyList<Client>)items, list.Count));
    }
}

public class ClientServiceTests
{
    private static readonly DateTime Now = new(2026, 11, 23, 10, 0, 0, DateTimeKind.Utc);

    private readonly FakeClientRepository _repo = new();
    private readonly FakeUnitOfWork _uow = new();
    private readonly ClientService _service;

    public ClientServiceTests()
    {
        _service = new ClientService(
            _repo,
            _uow,
            TimeProvider.System,
            new CreateClientDtoValidator(),
            new UpdateClientDtoValidator(),
            new ClientListQueryDtoValidator());
    }

    [Fact]
    public async Task Create_ValidDto_SavesClientAndReturnsDetail()
    {
        var dto = new CreateClientDto("Juan Perez", "PEHJ900101ABC", "5512345678", "juan@example.com");

        var result = await _service.CreateAsync(dto);

        Assert.NotNull(result);
        Assert.Equal("Juan Perez", result.Name);
        Assert.Equal("PEHJ900101ABC", result.Rfc);
        Assert.True(result.IsActive);
        Assert.Equal(1, _uow.Saves);
        Assert.Single(_repo.Store);
    }

    [Fact]
    public async Task Create_DuplicateRfc_ThrowsConflictException()
    {
        var existing = Client.Create("Cliente Existente", "PEHJ900101ABC", "5512345678", "existente@example.com", Now);
        _repo.Store.Add(existing);

        var dto = new CreateClientDto("Juan Nuevo", "PEHJ900101ABC", "5599999999", "nuevo@example.com");

        var ex = await Assert.ThrowsAsync<ConflictException>(() => _service.CreateAsync(dto));
        Assert.Equal("Ya existe un cliente con ese RFC.", ex.Message);
    }

    [Fact]
    public async Task Get_NonExistentClient_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Update_DeactivatedClient_ThrowsConflictException()
    {
        var client = Client.Create("Juan Perez", "PEHJ900101ABC", "5512345678", "juan@example.com", Now);
        client.Deactivate(Now);
        _repo.Store.Add(client);

        var dto = new UpdateClientDto("Juan Modificado", "PEHJ900101ABC", "5512345678", "juan@example.com");

        var ex = await Assert.ThrowsAsync<ConflictException>(() => _service.UpdateAsync(client.Id, dto));
        Assert.Equal("No se puede editar un cliente desactivado.", ex.Message);
    }

    [Fact]
    public async Task Update_DuplicateRfcOnAnotherClient_ThrowsConflictException()
    {
        var client1 = Client.Create("Cliente 1", "PEHJ900101ABC", "5511111111", "c1@example.com", Now);
        var client2 = Client.Create("Cliente 2", "GME9502284K8", "5522222222", "c2@example.com", Now);
        _repo.Store.Add(client1);
        _repo.Store.Add(client2);

        // Intentar actualizar client2 con el RFC de client1
        var dto = new UpdateClientDto("Cliente 2", "PEHJ900101ABC", "5522222222", "c2@example.com");

        var ex = await Assert.ThrowsAsync<ConflictException>(() => _service.UpdateAsync(client2.Id, dto));
        Assert.Equal("Ya existe un cliente con ese RFC.", ex.Message);
    }

    [Fact]
    public async Task Deactivate_SetsActiveFalse_AndIsIdempotent()
    {
        var client = Client.Create("Juan Perez", "PEHJ900101ABC", "5512345678", "juan@example.com", Now);
        _repo.Store.Add(client);

        await _service.DeactivateAsync(client.Id);
        Assert.False(client.IsActive);
        Assert.Equal(1, _uow.Saves);

        // Segunda llamada no debe guardar de nuevo
        await _service.DeactivateAsync(client.Id);
        Assert.Equal(1, _uow.Saves);
    }

    [Fact]
    public async Task Reactivate_SetsActiveTrue_AndIsIdempotent()
    {
        var client = Client.Create("Juan Perez", "PEHJ900101ABC", "5512345678", "juan@example.com", Now);
        client.Deactivate(Now);
        _repo.Store.Add(client);

        await _service.ReactivateAsync(client.Id);
        Assert.True(client.IsActive);
        Assert.Equal(1, _uow.Saves);

        // Segunda llamada no debe guardar de nuevo
        await _service.ReactivateAsync(client.Id);
        Assert.Equal(1, _uow.Saves);
    }
}

