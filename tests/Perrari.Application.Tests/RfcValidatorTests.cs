using System.Globalization;
using Perrari.Application.Validators.Clientes;
using Xunit;

namespace Perrari.Application.Tests;

public class RfcValidatorTests
{
    [Theory]
    [InlineData("PEHJ900101ABC")] // Persona física estándar
    [InlineData("ÑEHA850615XYZ")] // Persona física con Ñ
    [InlineData("A&HA921130123")] // Persona física con &
    [InlineData("GME9502284K8")] // Persona física con fecha 28 Feb en año no bisiesto
    public void Valid_PersonaFisica_ReturnsTrue(string rfc)
    {
        Assert.True(RfcValidator.IsValid(rfc));
    }

    [Theory]
    [InlineData("ABC100520XYZ")] // Persona moral estándar (12 caracteres)
    [InlineData("ÑBC100520XYZ")] // Persona moral con Ñ
    [InlineData("A&C100520XYZ")] // Persona moral con &
    public void Valid_PersonaMoral_ReturnsTrue(string rfc)
    {
        Assert.True(RfcValidator.IsValid(rfc));
    }

    [Theory]
    [InlineData("XAXX010101000")] // RFC genérico nacional
    [InlineData("XEXX010101000")] // RFC genérico extranjero
    [InlineData("xaxx010101000")] // Minúsculas (se normaliza)
    public void Valid_GenericRfc_ReturnsTrue(string rfc)
    {
        Assert.True(RfcValidator.IsValid(rfc));
    }

    [Theory]
    [InlineData("ABC")] // Demasiado corto
    [InlineData("PEHJ900101ABCD")] // 14 caracteres (demasiado largo)
    [InlineData("1234567890123")] // Solo números
    [InlineData("PEHJ900101AB#")] // Caracter inválido #
    [InlineData("PEHJ900101AB ")] // Caracter de espacio interno inválido
    public void Invalid_Format_ReturnsFalse(string rfc)
    {
        Assert.False(RfcValidator.IsValid(rfc));
    }

    [Theory]
    [InlineData("PEHJ901301ABC")] // Mes 13 inválido
    [InlineData("PEHJ900001ABC")] // Mes 00 inválido
    [InlineData("PEHJ900132ABC")] // Día 32 inválido
    [InlineData("PEHJ900100ABC")] // Día 00 inválido
    [InlineData("PEHJ010229ABC")] // 29 Feb en 2001 (no fue bisiesto)
    [InlineData("PEHJ900431ABC")] // 31 Abril (abril solo tiene 30 días)
    public void Invalid_CalendarDate_ReturnsFalse(string rfc)
    {
        Assert.False(RfcValidator.IsValid(rfc));
    }
}

