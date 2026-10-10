using AnilistConEnie.Application.Helpers;

namespace AnilistConEnie.Application.Tests.Helpers;

public class IntercambioHelperTests
{
    [Fact]
    public void ParsearPedidos_CommaSeparated_ReturnsRequests()
    {
        PedidosIntercambio resultado = IntercambioHelper.ParsearPedidos("Eli 1, Nai 1, Pepe 3, Gaby 2");

        Assert.True(resultado.EsValido);
        Assert.Equal(new Dictionary<string, int> { ["Eli"] = 1, ["Nai"] = 1, ["Pepe"] = 3, ["Gaby"] = 2 }, resultado.Pedidos);
    }

    [Fact]
    public void ParsearPedidos_NewLinesAndExtraSpaces_ReturnsRequests()
    {
        PedidosIntercambio resultado = IntercambioHelper.ParsearPedidos("  Eli   1\r\nNai 2,\n\nPepe 3,");

        Assert.True(resultado.EsValido);
        Assert.Equal(new Dictionary<string, int> { ["Eli"] = 1, ["Nai"] = 2, ["Pepe"] = 3 }, resultado.Pedidos);
    }

    [Fact]
    public void ParsearPedidos_NameWithSpaces_UsesLastTermAsAmount()
    {
        PedidosIntercambio resultado = IntercambioHelper.ParsearPedidos("Juan Perez 2, Ana 1");

        Assert.True(resultado.EsValido);
        Assert.Equal(2, resultado.Pedidos["Juan Perez"]);
    }

    [Theory]
    [InlineData("Eli, Nai 1")]
    [InlineData("Eli dos, Nai 1")]
    [InlineData("Eli 0, Nai 1")]
    [InlineData("Eli -1, Nai 1")]
    public void ParsearPedidos_InvalidAmount_ReportsEntry(string texto)
    {
        PedidosIntercambio resultado = IntercambioHelper.ParsearPedidos(texto);

        Assert.False(resultado.EsValido);
        Assert.Single(resultado.Errores);
        Assert.Contains("Eli", resultado.Errores[0]);
    }

    [Fact]
    public void ParsearPedidos_DuplicateNameIgnoringCase_ReportsDuplicate()
    {
        PedidosIntercambio resultado = IntercambioHelper.ParsearPedidos("Eli 1, Nai 1, eli 2");

        Assert.False(resultado.EsValido);
        Assert.Contains(resultado.Errores, e => e.Contains("más de una vez"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Eli 3")]
    public void ParsearPedidos_FewerThanTwoPeople_IsInvalid(string texto)
    {
        PedidosIntercambio resultado = IntercambioHelper.ParsearPedidos(texto);

        Assert.False(resultado.EsValido);
        Assert.Contains(resultado.Errores, e => e.Contains("al menos 2 personas"));
    }

    [Fact]
    public void ParsearPedidos_SeveralInvalidEntries_ReportsAll()
    {
        PedidosIntercambio resultado = IntercambioHelper.ParsearPedidos("Eli, Nai 0, Pepe 2");

        Assert.Equal(2, resultado.Errores.Count);
    }

    [Fact]
    public void Repartir_ValidRequests_DeliversExactTotal()
    {
        Dictionary<string, int> pedidos = new() { ["Ana"] = 2, ["Beto"] = 1, ["Cami"] = 3 };

        (List<string> reparto, Dictionary<string, int> detalle) = IntercambioHelper.Repartir(pedidos);

        Assert.Equal(6, reparto.Count); // total pedido = 2 + 1 + 3
        Assert.Equal(6, detalle.Values.Sum());
    }

    [Fact]
    public void Repartir_ValidRequests_NobodyAssignedToThemselves()
    {
        Dictionary<string, int> pedidos = new() { ["Ana"] = 2, ["Beto"] = 2, ["Cami"] = 2 };

        (List<string> reparto, _) = IntercambioHelper.Repartir(pedidos);

        foreach (string entrega in reparto)
        {
            string[] partes = entrega.Split(" -> ");
            Assert.NotEqual(partes[0], partes[1]);
        }
    }

    [Fact]
    public void Repartir_ValidRequests_EachReceiverGetsRequestedAmount()
    {
        Dictionary<string, int> pedidos = new() { ["Ana"] = 1, ["Beto"] = 2, ["Cami"] = 3 };

        (List<string> reparto, _) = IntercambioHelper.Repartir(pedidos);

        Dictionary<string, int> recibidas = reparto
            .GroupBy(e => e.Split(" -> ")[1])
            .ToDictionary(g => g.Key, g => g.Count());

        Assert.Equal(1, recibidas["Ana"]);
        Assert.Equal(2, recibidas["Beto"]);
        Assert.Equal(3, recibidas["Cami"]);
    }

    [Fact]
    public void Repartir_MultipleGivers_DistributesEvenly()
    {
        // 3 personas pidiendo 2 cada una: 6 entregas repartidas parejo -> 2 por dador.
        Dictionary<string, int> pedidos = new() { ["Ana"] = 2, ["Beto"] = 2, ["Cami"] = 2 };

        (_, Dictionary<string, int> detalle) = IntercambioHelper.Repartir(pedidos);

        Assert.All(detalle.Values, v => Assert.Equal(2, v));
    }

    [Fact]
    public void Repartir_ManyRandomRequests_KeepsInvariants()
    {
        Random rnd = new(1);

        for (int i = 0; i < 500; i++)
        {
            Dictionary<string, int> pedidos = Enumerable.Range(0, rnd.Next(2, 8))
                .ToDictionary(n => $"P{n}", _ => rnd.Next(1, 6));

            (List<string> reparto, Dictionary<string, int> detalle) = IntercambioHelper.Repartir(pedidos);

            List<string[]> partes = reparto.Select(e => e.Split(" -> ")).ToList();
            Assert.All(partes, p => Assert.NotEqual(p[0], p[1]));

            Dictionary<string, int> recibidas = partes.GroupBy(p => p[1]).ToDictionary(g => g.Key, g => g.Count());
            Assert.All(pedidos, p => Assert.Equal(p.Value, recibidas[p.Key]));

            Dictionary<string, int> dadas = partes.GroupBy(p => p[0]).ToDictionary(g => g.Key, g => g.Count());
            Assert.All(detalle, d => Assert.Equal(d.Value, dadas.GetValueOrDefault(d.Key)));
        }
    }
}
