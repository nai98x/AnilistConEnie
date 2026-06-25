using AnilistConEnie.Application.Helpers;

namespace AnilistConEnie.Application.Tests.Helpers;

public class IntercambioHelperTests
{
    [Fact]
    public void RepartirGPT_entrega_exactamente_lo_pedido_en_total()
    {
        Dictionary<string, int> pedidos = new() { ["Ana"] = 2, ["Beto"] = 1, ["Cami"] = 3 };

        (List<string> reparto, Dictionary<string, int> detalle) = IntercambioHelper.RepartirGPT(pedidos);

        Assert.Equal(6, reparto.Count); // total pedido = 2 + 1 + 3
        Assert.Equal(6, detalle.Values.Sum());
    }

    [Fact]
    public void RepartirGPT_nadie_se_entrega_a_si_mismo()
    {
        Dictionary<string, int> pedidos = new() { ["Ana"] = 2, ["Beto"] = 2, ["Cami"] = 2 };

        (List<string> reparto, _) = IntercambioHelper.RepartirGPT(pedidos);

        foreach (string entrega in reparto)
        {
            string[] partes = entrega.Split(" -> ");
            Assert.NotEqual(partes[0], partes[1]);
        }
    }

    [Fact]
    public void RepartirGPT_cada_receptor_recibe_lo_que_pidio()
    {
        Dictionary<string, int> pedidos = new() { ["Ana"] = 1, ["Beto"] = 2, ["Cami"] = 3 };

        (List<string> reparto, _) = IntercambioHelper.RepartirGPT(pedidos);

        Dictionary<string, int> recibidas = reparto
            .GroupBy(e => e.Split(" -> ")[1])
            .ToDictionary(g => g.Key, g => g.Count());

        Assert.Equal(1, recibidas["Ana"]);
        Assert.Equal(2, recibidas["Beto"]);
        Assert.Equal(3, recibidas["Cami"]);
    }

    [Fact]
    public void RepartirGPT_reparte_de_forma_pareja_entre_los_dadores()
    {
        // 3 personas pidiendo 2 cada una: 6 entregas repartidas parejo -> 2 por dador.
        Dictionary<string, int> pedidos = new() { ["Ana"] = 2, ["Beto"] = 2, ["Cami"] = 2 };

        (_, Dictionary<string, int> detalle) = IntercambioHelper.RepartirGPT(pedidos);

        Assert.All(detalle.Values, v => Assert.Equal(2, v));
    }

    [Fact]
    public void RepartirClasico_termina_y_produce_entregas()
    {
        // Algoritmo aleatorio (Random.Shared): solo verificamos que termina y reparte algo coherente.
        Dictionary<string, int> pedidos = new() { ["Ana"] = 1, ["Beto"] = 1, ["Cami"] = 1, ["Dani"] = 1 };

        List<string> reparto = IntercambioHelper.RepartirClasico(pedidos);

        Assert.NotEmpty(reparto);
        Assert.All(reparto, e => Assert.Contains(" -> ", e));
        // El resultado viene ordenado alfabéticamente.
        Assert.Equal(reparto.OrderBy(x => x), reparto);
    }
}
