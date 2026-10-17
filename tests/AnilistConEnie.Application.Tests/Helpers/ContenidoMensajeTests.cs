using AnilistConEnie.Application.Helpers;

namespace AnilistConEnie.Application.Tests.Helpers;

public class ContenidoMensajeTests
{
    [Theory]
    [InlineData("<:pepe:123456789>")]
    [InlineData("<a:pepe:123456789>")]
    [InlineData("<@198212314892075009>")]
    public void CuentaComoActividad_SingleAngleBracketToken_ReturnsFalse(string contenido) =>
        Assert.False(ContenidoMensaje.CuentaComoActividad(contenido));

    [Theory]
    [InlineData("hola")]
    [InlineData("<:pepe:123456789> jajaja")]
    [InlineData("<:pepe:123456789> <:pepe:123456789>")]
    [InlineData("😂")]
    [InlineData("")]
    public void CuentaComoActividad_OtherContent_ReturnsTrue(string contenido) =>
        Assert.True(ContenidoMensaje.CuentaComoActividad(contenido));
}
