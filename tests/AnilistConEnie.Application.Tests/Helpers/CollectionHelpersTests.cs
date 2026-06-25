using AnilistConEnie.Application.Helpers;

namespace AnilistConEnie.Application.Tests.Helpers;

public class CollectionHelpersTests
{
    [Fact]
    public void Shuffle_conserva_los_mismos_elementos()
    {
        List<int> list = [.. Enumerable.Range(1, 100)];

        list.Shuffle();

        Assert.Equal([.. Enumerable.Range(1, 100)], list.OrderBy(x => x));
        Assert.Equal(100, list.Count);
    }

    [Fact]
    public void Shuffle_lista_de_un_elemento_no_cambia()
    {
        List<string> list = ["solo"];
        list.Shuffle();
        Assert.Equal(["solo"], list);
    }

    [Fact]
    public void ToMemoryStream_envuelve_los_bytes_con_posicion_en_cero()
    {
        byte[] datos = [1, 2, 3, 4, 5];

        using MemoryStream stream = datos.ToMemoryStream();

        Assert.Equal(0, stream.Position);
        Assert.Equal(datos, stream.ToArray());
    }

    [Fact]
    public void Shuffle_diccionario_conserva_los_pares()
    {
        Dictionary<string, int> dict = new() { ["a"] = 1, ["b"] = 2, ["c"] = 3 };

        Dictionary<string, int> shuffled = dict.Shuffle();

        Assert.Equal(3, shuffled.Count);
        Assert.Equal(1, shuffled["a"]);
        Assert.Equal(2, shuffled["b"]);
        Assert.Equal(3, shuffled["c"]);
    }
}
