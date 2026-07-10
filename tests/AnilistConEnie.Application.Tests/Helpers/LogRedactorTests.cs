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
    public void RedactarValor_NombreSensible_DevuelveRedactado(string nombre)
    {
        Assert.Equal("***", LogRedactor.RedactarValor(nombre, "valor-secreto"));
    }

    [Theory]
    [InlineData("usuario")]
    [InlineData("cantidad")]
    [InlineData("mensaje")]
    public void RedactarValor_NombreNoSensible_DevuelveValorOriginal(string nombre)
    {
        Assert.Equal("hola", LogRedactor.RedactarValor(nombre, "hola"));
    }

    [Fact]
    public void RedactarValor_ValorVacioONulo_DevuelveVacio()
    {
        Assert.Equal(string.Empty, LogRedactor.RedactarValor("token", null));
        Assert.Equal(string.Empty, LogRedactor.RedactarValor("token", ""));
    }
}
