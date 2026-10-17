using AnilistConEnie.Application.Helpers;

namespace AnilistConEnie.Application.Tests.Helpers;

public class LogRedactorTests
{
    [Theory]
    [InlineData("token")]
    [InlineData("AniListToken")]
    [InlineData("password")]
    [InlineData("Contraseña")]
    [InlineData("contrasena")]
    [InlineData("clave")]
    [InlineData("apiKey")]
    [InlineData("ClientSecret")]
    public void RedactarValor_SensitiveName_ReturnsRedacted(string nombre)
    {
        Assert.Equal("***", LogRedactor.RedactarValor(nombre, "valor-secreto"));
    }

    [Theory]
    [InlineData("usuario")]
    [InlineData("cantidad")]
    [InlineData("mensaje")]
    public void RedactarValor_NonSensitiveName_ReturnsOriginalValue(string nombre)
    {
        Assert.Equal("hola", LogRedactor.RedactarValor(nombre, "hola"));
    }

    [Fact]
    public void RedactarValor_EmptyOrNullValue_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, LogRedactor.RedactarValor("token", null));
        Assert.Equal(string.Empty, LogRedactor.RedactarValor("token", ""));
    }
}
