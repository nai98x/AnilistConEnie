using AnilistConEnie.Application.Anilist;

namespace AnilistConEnie.Application.Tests.Anilist;

public class AnilistProfileUrlTests
{
    [Theory]
    [InlineData("https://anilist.co/user/Nai", "Nai")]
    [InlineData("https://anilist.co/user/Nai/", "Nai")]
    [InlineData("  https://anilist.co/user/Nai  ", "Nai")]
    [InlineData("Nai", "Nai")]
    [InlineData("Nombre Con Espacios", "Nombre Con Espacios")]
    public void ExtractUserName_obtiene_el_nombre_de_la_url_o_lo_devuelve_tal_cual(string input, string esperado)
    {
        Assert.Equal(esperado, AnilistProfileUrl.ExtractUserName(input));
    }

    [Theory]
    [InlineData("https://anilist.co/user/12345", 12345)]
    [InlineData("https://anilist.co/user/12345/", 12345)]
    [InlineData("67890", 67890)]
    public void TryGetUserId_extrae_el_id_numerico(string url, int esperado)
    {
        Assert.True(AnilistProfileUrl.TryGetUserId(url, out int userId));
        Assert.Equal(esperado, userId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("https://anilist.co/user/Nombre")]
    public void TryGetUserId_falla_si_no_hay_id_numerico(string? url)
    {
        Assert.False(AnilistProfileUrl.TryGetUserId(url, out int userId));
        Assert.Equal(0, userId);
    }
}
