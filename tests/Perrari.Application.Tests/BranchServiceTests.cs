using Perrari.Application.Common.Exceptions;
using Perrari.Application.DTOs.Inventarios;
using Perrari.Application.Services;
using Perrari.Application.Validators.Inventarios;
using Xunit;

namespace Perrari.Application.Tests;

public class BranchServiceTests
{
    private readonly FakeBranchRepository _branches = new();
    private readonly BranchService _service;

    public BranchServiceTests()
        => _service = new BranchService(_branches, new FakeUnitOfWork(), new MutableTimeProvider(new DateTime(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc)), new BranchInputDtoValidator());

    [Fact]
    public async Task Create_and_update_work_and_names_are_unique_ignoring_case()
    {
        var a = await _service.CreateAsync(new BranchInputDto("Maranello", "Via Abetone 1", null));
        var b = await _service.CreateAsync(new BranchInputDto("Modena", null, "555-0100"));

        await Assert.ThrowsAsync<ConflictException>(() => _service.CreateAsync(new BranchInputDto("MARANELLO", null, null)));
        await Assert.ThrowsAsync<ConflictException>(() => _service.UpdateAsync(b.Id, new BranchInputDto("maranello", null, null)));

        // Conservar el propio nombre al editar es valido
        var updated = await _service.UpdateAsync(a.Id, new BranchInputDto("Maranello", "Nueva dirección", null));
        Assert.Equal("Nueva dirección", updated.Address);
    }

    [Fact]
    public async Task Missing_name_is_a_validation_error_and_unknown_id_is_404()
    {
        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => _service.CreateAsync(new BranchInputDto("", null, null)));
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetAsync(Guid.NewGuid()));
        await Assert.ThrowsAsync<NotFoundException>(() => _service.UpdateAsync(Guid.NewGuid(), new BranchInputDto("X", null, null)));
    }
}
