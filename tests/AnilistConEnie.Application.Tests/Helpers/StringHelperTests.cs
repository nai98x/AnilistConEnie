using AnilistConEnie.Application.Helpers;

namespace AnilistConEnie.Application.Tests.Helpers;

public class StringHelperTests
{
    [Fact]
    public void CreateString_GivenLength_ReturnsAllowedChars()
    {
        const string permitidos = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz0123456789";

        string result = StringHelper.CreateString(50);

        Assert.Equal(50, result.Length);
        Assert.All(result, c => Assert.Contains(c, permitidos));
        Assert.DoesNotContain('l', result); // la 'l' minúscula está excluida del alfabeto
    }

    [Fact]
    public void CreateString_ZeroLength_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, StringHelper.CreateString(0));
    }

    [Fact]
    public void TextAfter_Match_ReturnsRemainder()
    {
        Assert.Equal("mundo", "hola=mundo".TextAfter("="));
        Assert.Equal("/user/Nai", "https://anilist.co/user/Nai".TextAfter("anilist.co"));
    }

    [Fact]
    public void NormalizarDescription_ShortText_Unchanged()
    {
        string s = new('a', 100);
        Assert.Equal(s, StringHelper.NormalizarDescription(s));
    }

    [Fact]
    public void NormalizarDescription_LongText_TruncatesAtLastBracket()
    {
        // Texto largo con un corchete después del límite de recorte.
        string s = new string('a', 2040) + "[link]" + new string('b', 100);

        string result = StringHelper.NormalizarDescription(s);

        Assert.True(result.Length <= 2048);
        Assert.EndsWith("...", result);
        Assert.DoesNotContain("[", result);
    }

    [Fact]
    public void NormalizarDescription_LongTextNoBracket_TruncatesByLength()
    {
        string s = new('a', 3000);

        string result = StringHelper.NormalizarDescription(s);

        Assert.EndsWith(" ...", result);
        Assert.Equal(2048, result.Length); // 2044 'a' + " ..."
    }

    [Fact]
    public void NormalizarField_LongText_TruncatesTo1024()
    {
        string s = new('x', 2000);

        string result = StringHelper.NormalizarField(s);

        Assert.EndsWith(" ...", result);
        Assert.Equal(1024, result.Length);
    }

    [Fact]
    public void NormalizarBoton_LongText_TruncatesTo80WithEllipsis()
    {
        string s = new('z', 100);

        string result = StringHelper.NormalizarBoton(s);

        Assert.Equal(80, result.Length); // 76 + " ..."
        Assert.EndsWith(" ...", result);
    }

    [Fact]
    public void NormalizarBoton_TextUpTo80_Unchanged()
    {
        string s = new('z', 80);
        Assert.Equal(s, StringHelper.NormalizarBoton(s));
    }

    [Theory]
    [InlineData("hola<br>mundo", "holamundo")]
    [InlineData("a<BR>b", "ab")]
    [InlineData("<i>x</i>", "*x*")]
    [InlineData("<b>x</b>", "**x**")]
    [InlineData("~!spoiler!~", "||spoiler||")]
    [InlineData("__negrita__", "**negrita**")]
    public void LimpiarTexto_AnilistTags_TranslatesToMarkdown(string entrada, string esperado)
    {
        Assert.Equal(esperado, StringHelper.LimpiarTexto(entrada));
    }

    [Fact]
    public void LimpiarTexto_NullOrEmpty_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, StringHelper.LimpiarTexto(null!));
        Assert.Equal(string.Empty, StringHelper.LimpiarTexto(""));
    }
}
