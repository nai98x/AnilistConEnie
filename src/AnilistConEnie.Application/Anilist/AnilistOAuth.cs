namespace AnilistConEnie.Application.Anilist;

public static class AnilistOAuth
{
    public static string UrlAutorizacion(int clientId, string redirectUri, string state) =>
        "https://anilist.co/api/v2/oauth/authorize" +
        $"?client_id={clientId}" +
        $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
        "&response_type=code" +
        $"&state={Uri.EscapeDataString(state)}";
}
