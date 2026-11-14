using AnilistConEnie.Bot.Configuration;
using AnilistConEnie.Bot.Helpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace AnilistConEnie.Bot.Extensions;

public static class VinculacionAnilistEndpoints
{
    public static WebApplication MapVinculacionAnilist(this WebApplication app)
    {
        string ruta = app.Services.GetRequiredService<VinculacionAnilistSettings>().RedirectUri.AbsolutePath;

        app.MapGet(ruta, (string? code, string? state, VinculacionAnilistWeb vinculacion) => vinculacion.CallbackAsync(code, state));

        return app;
    }
}
