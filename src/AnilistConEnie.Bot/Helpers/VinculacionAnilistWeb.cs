using System.Net;
using AnilistConEnie.Application.Anilist;
using AnilistConEnie.Bot.Configuration;
using AnilistConEnie.Bot.Observabilidad;
using AnilistConEnie.Bot.Services.State;
using AnilistConEnie.Domain.Exceptions;
using AnilistConEnie.Domain.Interfaces;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.AspNetCore.Http;

namespace AnilistConEnie.Bot.Helpers;

/// <summary>
/// Vinculación de AniList por OAuth (Authorization Code): el miembro recibe un enlace personal de
/// autorización y AniList lo redirige al callback del bot, expuesto con Tailscale Funnel.
/// </summary>
public class VinculacionAnilistWeb(DiscordClient client, VinculacionAnilistState vinculacionState, VinculacionAnilistSettings settings, AnilistService anilistService, IAnilistOAuthClient oauthClient, DiscordLogService logService)
{
    public const string IdBoton = "anilist-vincular";

    public async Task IniciarAsync(DiscordInteraction interaction)
    {
        if (await anilistService.ResponderSiAprobacionPendienteAsync(interaction)) return;

        TimeSpan validez = TimeSpan.FromMinutes(settings.ValidezEnlaceMinutos);
        string state = vinculacionState.Registrar(interaction, validez);
        string url = AnilistOAuth.UrlAutorizacion(settings.ClientId, settings.RedirectUri.OriginalString, state);

        await interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
            .AsEphemeral()
            .AddEmbed(new DiscordEmbedBuilder
            {
                Title = "Tu enlace está listo",
                Description =
                    $"Vence {Formatter.Timestamp(DateTimeOffset.UtcNow + validez, TimestampFormat.RelativeTime)}.\n\n" +
                    $"Una vez vinculada tu cuenta, puedes revocar el acceso del bot desde AniList en {Formatter.MaskedUrl("Settings → Apps", new Uri("https://anilist.co/settings/apps"))}.",
                Color = DiscordEmojiHelper.GetColor()
            })
            .AddActionRowComponent(new DiscordLinkButtonComponent(url, "Autorizar en AniList")));
    }

    public async Task<IResult> CallbackAsync(string? code, string? state)
    {
        IResult respuesta = Pagina(HttpStatusCode.InternalServerError, "Error", "Ocurrió un error inesperado. Vuelve a Discord e intenta de nuevo.");
        await Telemetria.TrazarEventoAsync("AnilistOAuthCallback", async () => respuesta = await ProcesarCallbackAsync(code, state));
        return respuesta;
    }

    private async Task<IResult> ProcesarCallbackAsync(string? code, string? state)
    {
        VinculacionPendiente? pendiente = string.IsNullOrEmpty(state) ? null : vinculacionState.Consumir(state);
        if (pendiente is null)
            return Pagina(HttpStatusCode.BadRequest, "Enlace vencido",
                "Este enlace ya se usó o venció. Vuelve a Discord y haz click en Vincular AniList otra vez.");

        DiscordInteraction interaccion = pendiente.Interaccion;
        DiscordGuild guild = interaccion.Guild!;

        if (string.IsNullOrEmpty(code))
        {
            await ActualizarMensajeAsync(interaccion, ErrorEmbed.De("Autorización cancelada",
                "No se autorizó el acceso en AniList. Haz click en **Vincular AniList** para intentarlo de nuevo."));
            return Pagina(HttpStatusCode.BadRequest, "Autorización cancelada",
                "No se autorizó el acceso en AniList. Vuelve a Discord para intentarlo de nuevo.");
        }

        ResultadoVinculacion resultado;
        try
        {
            string accessToken = await oauthClient.CanjearCodigoAsync(code);
            resultado = await anilistService.ProcesarVinculacionAsync(client, guild, interaccion.User, accessToken);
        }
        catch (Exception ex)
        {
            await logService.LogException(guild, ex, $"Vinculación de AniList por enlace de {interaccion.User.Id}");
            resultado = ResultadoVinculacion.ConError(ex is AnilistApiException
                ? "No se pudo validar la autorización con AniList. Intenta de nuevo en unos minutos."
                : "Ocurrió un error inesperado. Intenta de nuevo en unos minutos.");
        }

        if (EmbedResultado(resultado) is { } embed)
            await ActualizarMensajeAsync(interaccion, embed);

        return resultado.Estado switch
        {
            EstadoVinculacion.Vinculada => Pagina(HttpStatusCode.OK, "¡Cuenta vinculada!",
                "Tu cuenta de AniList quedó vinculada. Ya puedes cerrar esta página y volver a Discord."),
            EstadoVinculacion.PendienteAprobacion => Pagina(HttpStatusCode.OK, "Pendiente de aprobación",
                "El staff tiene que aprobar la vinculación de tu cuenta. Ya puedes cerrar esta página y volver a Discord."),
            EstadoVinculacion.Error => Pagina(HttpStatusCode.BadGateway, "No se pudo vincular",
                $"{resultado.Error?.TrimEnd('.')}. Vuelve a Discord para intentarlo de nuevo."),
            _ => Pagina(HttpStatusCode.Forbidden, "No se pudo vincular", "No se pudo completar la vinculación.")
        };
    }

    private static DiscordEmbedBuilder? EmbedResultado(ResultadoVinculacion resultado) => resultado.Estado switch
    {
        EstadoVinculacion.Vinculada => new DiscordEmbedBuilder
        {
            Title = "Cuenta vinculada",
            Description = "Tu cuenta de AniList quedó vinculada. ¡Bienvenido a Añilist!",
            Color = DiscordColor.Green
        },
        EstadoVinculacion.PendienteAprobacion => new DiscordEmbedBuilder
        {
            Title = "Atencion",
            Description = "La cuenta debe ser vinculada manualmente por el staff.",
            Color = DiscordColor.Yellow
        },
        EstadoVinculacion.Error => ErrorEmbed.De(resultado.Error ?? "Error desconocido"),
        _ => null
    };

    private async Task ActualizarMensajeAsync(DiscordInteraction interaccion, DiscordEmbed embed)
    {
        try
        {
            await interaccion.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
        }
        catch (Exception ex)
        {
            await logService.LogException(interaccion.Guild!, ex, $"Vinculación de AniList por enlace - actualizar mensaje de {interaccion.User.Id}");
        }
    }

    private static IResult Pagina(HttpStatusCode status, string titulo, string mensaje) =>
        Results.Content($$"""
            <!doctype html>
            <html lang="es">
            <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <meta name="robots" content="noindex">
            <title>Añilist · {{WebUtility.HtmlEncode(titulo)}}</title>
            <style>
            :root { color-scheme: light dark; }
            body { margin: 0; min-height: 100vh; display: grid; place-items: center; padding: 0 16px; font-family: system-ui, sans-serif; background: Canvas; color: CanvasText; }
            main { max-width: 28rem; text-align: center; }
            h1 { font-size: 1.5rem; }
            p { line-height: 1.5; }
            </style>
            </head>
            <body>
            <main>
            <h1>{{WebUtility.HtmlEncode(titulo)}}</h1>
            <p>{{WebUtility.HtmlEncode(mensaje)}}</p>
            </main>
            </body>
            </html>
            """, "text/html; charset=utf-8", statusCode: (int)status);
}
