using AnilistConEnie.Application.Xp;

namespace AnilistConEnie.Application.Tests.Xp;

public class RepartoXpTests
{
    private static readonly Dictionary<ulong, string> Nombres = new()
    {
        [1] = "Nai",
        [2] = "Eli",
        [3] = "Gabý"
    };

    [Fact]
    public void Prellenar_SacaLosSeparadoresDelNombre()
    {
        Dictionary<ulong, string> raro = new() { [1] = "Na=i:  x" };

        string texto = RepartoXp.Prellenar([new AsignacionXp(1, 0)], raro);

        Assert.Equal("Nai x = 0", texto);
    }

    [Theory]
    [InlineData("500", 500)]
    [InlineData("+500", 500)]
    [InlineData("-200", -200)]
    [InlineData(" 1.500 ", 1500)]
    [InlineData("2 000", 2000)]
    [InlineData("0", 0)]
    public void TryParseXp_AceptaLosFormatosDelModal(string valor, long esperado)
    {
        Assert.True(RepartoXp.TryParseXp(valor, out long xp));
        Assert.Equal(esperado, xp);
    }

    [Theory]
    [InlineData("mucha")]
    [InlineData("")]
    [InlineData("1,5x")]
    public void TryParseXp_RechazaLoQueNoEsNumero(string valor)
    {
        Assert.False(RepartoXp.TryParseXp(valor, out _));
    }

    [Fact]
    public void Parse_AceptaPositivosNegativosYSignoExplicito()
    {
        RepartoParseado resultado = RepartoXp.Parse("Nai = 500\nEli = -200\nGaby = +1500", Nombres);

        Assert.Empty(resultado.Errores);
        Assert.Equal(
            [new AsignacionXp(1, 500), new AsignacionXp(2, -200), new AsignacionXp(3, 1500)],
            resultado.Asignaciones);
    }

    [Fact]
    public void Parse_IgnoraLasLineasEnCeroYLasVacias()
    {
        RepartoParseado resultado = RepartoXp.Parse("Nai = 500\n\nEli = 0\n   \n", Nombres);

        Assert.Empty(resultado.Errores);
        Assert.Equal([new AsignacionXp(1, 500)], resultado.Asignaciones);
    }

    [Fact]
    public void Parse_AceptaDosPuntosYSeparadoresDeMiles()
    {
        RepartoParseado resultado = RepartoXp.Parse("Nai: 1.500\nEli : -2 000", Nombres);

        Assert.Empty(resultado.Errores);
        Assert.Equal([new AsignacionXp(1, 1500), new AsignacionXp(2, -2000)], resultado.Asignaciones);
    }

    [Fact]
    public void Parse_IgnoraMayusculasYAcentos()
    {
        RepartoParseado resultado = RepartoXp.Parse("gaby = 100", Nombres);

        Assert.Empty(resultado.Errores);
        Assert.Equal([new AsignacionXp(3, 100)], resultado.Asignaciones);
    }

    [Fact]
    public void Parse_ReportaNombreDesconocido()
    {
        RepartoParseado resultado = RepartoXp.Parse("Nai = 500\nPepe = 300", Nombres);

        Assert.Equal([new AsignacionXp(1, 500)], resultado.Asignaciones);
        Assert.Single(resultado.Errores);
        Assert.Contains("Pepe", resultado.Errores[0]);
    }

    [Fact]
    public void Parse_ReportaNumeroInvalido()
    {
        RepartoParseado resultado = RepartoXp.Parse("Nai = mucha", Nombres);

        Assert.Empty(resultado.Asignaciones);
        Assert.Single(resultado.Errores);
        Assert.Contains("mucha", resultado.Errores[0]);
    }

    [Fact]
    public void Parse_ReportaLineaSinSeparador()
    {
        RepartoParseado resultado = RepartoXp.Parse("Nai 500", Nombres);

        Assert.Empty(resultado.Asignaciones);
        Assert.Single(resultado.Errores);
        Assert.Contains("No se entiende", resultado.Errores[0]);
    }

    [Fact]
    public void Parse_ReportaUsuarioRepetido()
    {
        RepartoParseado resultado = RepartoXp.Parse("Nai = 500\nNai = 300", Nombres);

        Assert.Equal([new AsignacionXp(1, 500)], resultado.Asignaciones);
        Assert.Single(resultado.Errores);
        Assert.Contains("más de una vez", resultado.Errores[0]);
    }

    [Fact]
    public void Parse_ReportaNombreAmbiguo()
    {
        Dictionary<ulong, string> homonimos = new() { [1] = "Nai", [2] = "nai" };

        RepartoParseado resultado = RepartoXp.Parse("Nai = 500", homonimos);

        Assert.Empty(resultado.Asignaciones);
        Assert.Single(resultado.Errores);
        Assert.Contains("más de un usuario", resultado.Errores[0]);
    }

    [Fact]
    public void Parse_SinNingunaAsignacionAvisa()
    {
        RepartoParseado resultado = RepartoXp.Parse("Nai = 0\nEli = 0", Nombres);

        Assert.Empty(resultado.Asignaciones);
        Assert.Single(resultado.Errores);
        Assert.Contains("ningún usuario", resultado.Errores[0]);
    }

    [Fact]
    public void Renderizar_MuestraMencionYSigno()
    {
        string texto = RepartoXp.Renderizar([new AsignacionXp(1, 500), new AsignacionXp(2, -200)]);

        Assert.Equal("<@1> · +500 XP\n<@2> · -200 XP", texto);
    }

    [Fact]
    public void ParseMensaje_RecuperaLoQueRenderizo()
    {
        AsignacionXp[] original = [new AsignacionXp(1, 500), new AsignacionXp(2, -200), new AsignacionXp(3, 1500)];

        IReadOnlyList<AsignacionXp> recuperado = RepartoXp.ParseMensaje(
            $"## Repartir XP · Pelis\nRepartido por <@99>\n{RepartoXp.Renderizar(original)}\n-# Pendiente");

        Assert.Equal(original, recuperado);
    }

    [Fact]
    public void PrellenarDesdeAsignaciones_ArmaElTextoParaEditar()
    {
        string texto = RepartoXp.Prellenar([new AsignacionXp(1, 500), new AsignacionXp(2, -200)], Nombres);

        Assert.Equal("Nai = 500\nEli = -200", texto);
    }
}
