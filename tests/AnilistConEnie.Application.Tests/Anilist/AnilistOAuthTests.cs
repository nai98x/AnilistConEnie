using AnilistConEnie.Application.Anilist;

namespace AnilistConEnie.Application.Tests.Anilist;

public class AnilistOAuthTests
{
    [Fact]
    public void UrlAutorizacion_ValidParameters_ReturnsAuthorizationCodeUrl()
    {
        string url = AnilistOAuth.UrlAutorizacion(1234, "https://bot.tail1a2b3c.ts.net/anilist/callback", "abc-123_XYZ");

        Assert.Equal(
            "https://anilist.co/api/v2/oauth/authorize?client_id=1234" +
            "&redirect_uri=https%3A%2F%2Fbot.tail1a2b3c.ts.net%2Fanilist%2Fcallback" +
            "&response_type=code&state=abc-123_XYZ",
            url);
    }

    [Fact]
    public void UrlAutorizacion_StateWithReservedCharacters_EscapesState()
    {
        string url = AnilistOAuth.UrlAutorizacion(1, "https://x.ts.net/cb", "a+b/c=&d");

        Assert.EndsWith("&state=a%2Bb%2Fc%3D%26d", url);
    }
}
