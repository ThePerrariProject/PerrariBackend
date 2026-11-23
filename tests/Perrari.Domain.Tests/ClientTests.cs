using Perrari.Domain.Entities.Clientes;
using Perrari.Domain.Exceptions;
using Xunit;

namespace Perrari.Domain.Tests;

public class ClientTests
{
    private static readonly DateTime Now = new(2026, 11, 23, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Later = Now.AddHours(2);

    [Fact]
    public void Create_NormalizesData_AndSetsActive()
    {
        var client = Client.Create(
            "  Juan Perez Hernandez  ",
            "  pehj900101abc  ",
            "55-1234-5678",
            "  JUAN@EXAMPLE.COM  ",
            Now);

        Assert.Equal("Juan Perez Hernandez", client.Name);
        Assert.Equal("PEHJ900101ABC", client.Rfc);
        Assert.Equal("5512345678", client.Phone);
        Assert.Equal("juan@example.com", client.Email);
        Assert.True(client.IsActive);
        Assert.Equal(Now, client.CreatedAt);
        Assert.Equal(Now, client.UpdatedAt);
    }

    [Fact]
    public void Update_UpdatesFieldsAndTouchesUpdatedAt()
    {
        var client = Client.Create("Juan Perez", "PEHJ900101ABC", "5512345678", "juan@example.com", Now);

        client.Update("Juan P. Hernandez", "PEHJ900101XYZ", "5587654321", "nuevo@example.com", Later);

        Assert.Equal("Juan P. Hernandez", client.Name);
        Assert.Equal("PEHJ900101XYZ", client.Rfc);
        Assert.Equal("5587654321", client.Phone);
        Assert.Equal("nuevo@example.com", client.Email);
        Assert.Equal(Later, client.UpdatedAt);
    }

    [Fact]
    public void Update_WhenDeactivated_ThrowsDomainException()
    {
        var client = Client.Create("Juan Perez", "PEHJ900101ABC", "5512345678", "juan@example.com", Now);
        client.Deactivate(Later);

        var ex = Assert.Throws<DomainException>(() =>
            client.Update("Juan Nuevo", "PEHJ900101ABC", "5512345678", "juan@example.com", Later.AddHours(1)));

        Assert.Equal("No se puede editar un cliente desactivado.", ex.Message);
    }

    [Fact]
    public void Deactivate_SetsIsActiveFalse_AndIsIdempotent()
    {
        var client = Client.Create("Juan Perez", "PEHJ900101ABC", "5512345678", "juan@example.com", Now);

        client.Deactivate(Later);
        Assert.False(client.IsActive);
        Assert.Equal(Later, client.UpdatedAt);

        // Segunda llamada no debe modificar el UpdatedAt (idempotente)
        client.Deactivate(Later.AddHours(1));
        Assert.False(client.IsActive);
        Assert.Equal(Later, client.UpdatedAt);
    }

    [Fact]
    public void Reactivate_SetsIsActiveTrue_AndIsIdempotent()
    {
        var client = Client.Create("Juan Perez", "PEHJ900101ABC", "5512345678", "juan@example.com", Now);
        client.Deactivate(Later);

        var reactivateTime = Later.AddHours(1);
        client.Reactivate(reactivateTime);
        Assert.True(client.IsActive);
        Assert.Equal(reactivateTime, client.UpdatedAt);

        // Segunda llamada no debe modificar el UpdatedAt (idempotente)
        client.Reactivate(reactivateTime.AddHours(1));
        Assert.True(client.IsActive);
        Assert.Equal(reactivateTime, client.UpdatedAt);
    }

    [Theory]
    [InlineData("", "PEHJ900101ABC", "5512345678", "juan@example.com")] // Nombre vacio
    [InlineData("Juan", "", "5512345678", "juan@example.com")] // RFC vacio
    [InlineData("Juan", "PEHJ900101ABC", "", "juan@example.com")] // Telefono vacio
    [InlineData("Juan", "PEHJ900101ABC", "5512345678", "")] // Correo vacio
    public void Create_InvalidData_ThrowsDomainException(string name, string rfc, string phone, string email)
    {
        Assert.Throws<DomainException>(() => Client.Create(name, rfc, phone, email, Now));
    }
}

