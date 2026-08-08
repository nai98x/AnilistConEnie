using AnilistConEnie.Application.Xp;
using AnilistConEnie.Bot.Configuration;
using AnilistConEnie.Bot.Services.State;
using AnilistConEnie.Domain.Entities;
using AnilistConEnie.Domain.Enum;
using AnilistConEnie.Domain.Interfaces.Repositories;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;

namespace AnilistConEnie.Bot.Helpers;

/// <summary>
/// Reparto de XP entre varios usuarios y su circuito de aprobación. El mensaje publicado en el canal de
/// colaboradores es el único estado del reparto pendiente: las asignaciones se releen de ahí con
/// <see cref="RepartoXp.ParseMensaje"/>, así un reinicio del bot no deja repartos huérfanos.
/// </summary>
public class RepartoXpService(
    BotConfiguration config,
    RangoRoles rangoRoles,
    IXpUsuariosRepository xpUsuariosRepository,
    DiscordLogService logService,
    InteractivityExtension interactivity)
{
    public const string PrefijoBoton = "repartirxp-";

    /// <summary>El prefijo <c>modal-</c> evita que <c>ComponentInteractionHandler</c> haga defer del select y nos deje sin poder abrir el modal.</summary>
    public const string SelectUsuarios = "modal-repartirxp-usuarios";

    public const string CampoXp = "xp";

    public const string CampoDetalle = "detalle";

    private const string PiePendiente = "-# ⏳ Pendiente de la aprobación de un Kami Sama";

    public async Task<DiscordMessage> PublicarAsync(DiscordChannel canal, string encabezado, IReadOnlyList<AsignacionXp> asignaciones, ulong autorId)
    {
        DiscordMessageBuilder builder = new DiscordMessageBuilder()
            .EnableV2Components()
            .AddContainerComponent(Contenedor(encabezado, RepartoXp.Renderizar(asignaciones), $"{Resumen(asignaciones)}\n{PiePendiente}", DiscordColor.Orange))
            .AddActionRowComponent(
                new DiscordButtonComponent(DiscordButtonStyle.Success, $"{PrefijoBoton}aprobar", "Aprobar"),
                new DiscordButtonComponent(DiscordButtonStyle.Danger, $"{PrefijoBoton}denegar", "Denegar"),
                new DiscordButtonComponent(DiscordButtonStyle.Secondary, $"{PrefijoBoton}editar-{autorId}", "Editar"),
                new DiscordButtonComponent(DiscordButtonStyle.Secondary, $"{PrefijoBoton}descartar-{autorId}", "Descartar"));

        return await canal.SendMessageAsync(builder);
    }

    public static DiscordContainerComponent Contenedor(string encabezado, string lista, string pie, DiscordColor color) =>
        new([
            new DiscordTextDisplayComponent(encabezado),
            new DiscordSeparatorComponent(divider: true, spacing: DiscordSeparatorSpacing.Small),
            new DiscordTextDisplayComponent(lista),
            new DiscordSeparatorComponent(divider: true, spacing: DiscordSeparatorSpacing.Small),
            new DiscordTextDisplayComponent(pie)
        ], color: color);

    public static string Resumen(IReadOnlyList<AsignacionXp> asignaciones) => $"-# {asignaciones.Count} usuario(s)";

    public static string Signo(long xp) => $"{(xp >= 0 ? "+" : "-")}{Math.Abs(xp)}";

    public async Task ResolverAsync(ComponentInteractionCreatedEventArgs args)
    {
        string[] partes = args.Interaction.Data.CustomId[PrefijoBoton.Length..].Split('-');
        string accion = partes[0];

        if (accion is "editar" or "descartar")
        {
            if (partes.Length < 2 || !ulong.TryParse(partes[1], out ulong autorId)) return;
            if (args.User.Id != autorId)
            {
                await RechazarAsync(args, "Solo quien armó el reparto puede editarlo o descartarlo.");
                return;
            }
        }
        else if (!await EsKamiSamaAsync(args))
        {
            return;
        }

        switch (accion)
        {
            case "aprobar":
                await AprobarAsync(args);
                break;
            case "denegar":
                await DenegarAsync(args);
                break;
            case "editar":
                await EditarAsync(args);
                break;
            case "descartar":
                await DescartarAsync(args);
                break;
        }
    }

    private async Task AprobarAsync(ComponentInteractionCreatedEventArgs args)
    {
        await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);

        IReadOnlyList<AsignacionXp> asignaciones = RepartoXp.ParseMensaje(TextoDelReparto(args.Message));
        if (asignaciones.Count == 0)
        {
            await AvisarAsync(args, "No se pudo leer el reparto de este mensaje.");
            return;
        }

        string lista = await AplicarAsync(args.Guild, asignaciones);
        await CerrarAsync(args, lista, $"{Resumen(asignaciones)}\n-# ✅ Aprobado por {args.User.Mention}", DiscordColor.Green);
    }

    private async Task DenegarAsync(ComponentInteractionCreatedEventArgs args)
    {
        await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);

        IReadOnlyList<AsignacionXp> asignaciones = RepartoXp.ParseMensaje(TextoDelReparto(args.Message));
        string lista = asignaciones.Count > 0 ? RepartoXp.Renderizar(asignaciones) : "-# No se pudo releer el reparto.";

        await CerrarAsync(args, lista, $"{Resumen(asignaciones)}\n-# ❌ Denegado por {args.User.Mention}", DiscordColor.Red);
    }

    private async Task DescartarAsync(ComponentInteractionCreatedEventArgs args)
    {
        await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);

        try
        {
            await args.Message.DeleteAsync();
        }
        catch (Exception ex)
        {
            await logService.LogException(args.Guild, ex, "Repartir XP - descartar");
        }
    }

    private async Task EditarAsync(ComponentInteractionCreatedEventArgs args)
    {
        IReadOnlyList<AsignacionXp> asignaciones = RepartoXp.ParseMensaje(TextoDelReparto(args.Message));
        if (asignaciones.Count == 0)
        {
            await RechazarAsync(args, "No se pudo leer el reparto de este mensaje.");
            return;
        }

        Dictionary<ulong, string> nombres = Nombres(args.Guild, asignaciones.Select(a => a.UserId));

        string modalId = $"modal-repartirxp-{args.Interaction.Id}";
        DiscordModalBuilder modal = new DiscordModalBuilder()
            .WithCustomId(modalId)
            .WithTitle("Editar reparto")
            .AddTextInput(
                new DiscordTextInputComponent(CampoXp, "Nombre = 500", RepartoXp.Prellenar(asignaciones, nombres), true, DiscordTextInputStyle.Paragraph),
                "XP por usuario",
                "Un usuario por línea. En negativo se le quita, en cero queda afuera.");

        await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.Modal, modal);

        InteractivityResult<ModalSubmittedEventArgs> respuesta = await interactivity.WaitForModalAsync(modalId, args.User, TimeSpan.FromMinutes(10));
        if (respuesta.TimedOut) return;

        await respuesta.Result.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);

        RepartoParseado parseado = RepartoXp.Parse(TextoDelModal(respuesta.Result), nombres);
        if (parseado.Errores.Count > 0)
        {
            await respuesta.Result.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder()
                .AsEphemeral()
                .WithContent($"No se guardó nada:\n{string.Join("\n", parseado.Errores.Select(e => $"- {e}"))}"));
            return;
        }

        DiscordMessageBuilder edicion = new DiscordMessageBuilder()
            .EnableV2Components()
            .AddContainerComponent(Contenedor(
                Encabezado(args.Message),
                RepartoXp.Renderizar(parseado.Asignaciones),
                $"{Resumen(parseado.Asignaciones)}\n{PiePendiente}",
                DiscordColor.Orange));

        foreach (DiscordActionRowComponent fila in args.Message.Components!.OfType<DiscordActionRowComponent>())
            edicion.AddActionRowComponent(fila);

        await args.Message.ModifyAsync(edicion);
    }

    /// <summary>Aplica la XP usuario por usuario y devuelve la lista ya anotada con los cambios de rango.</summary>
    private async Task<string> AplicarAsync(DiscordGuild guild, IReadOnlyList<AsignacionXp> asignaciones)
    {
        List<string> lineas = [];

        foreach (AsignacionXp asignacion in asignaciones)
        {
            try
            {
                if (!guild.Members.TryGetValue(asignacion.UserId, out DiscordMember? member))
                {
                    lineas.Add($"<@{asignacion.UserId}> · {Signo(asignacion.Xp)} XP · ⚠️ ya no está en el servidor");
                    continue;
                }

                long magnitud = Math.Abs(asignacion.Xp);
                XpOperation operacion = asignacion.Xp >= 0 ? XpOperation.Add : XpOperation.Remove;

                DiscordRole rangoAntes = rangoRoles.GetRoleByXpActual(guild, member);
                UserXp actual = await xpUsuariosRepository.AddRemove(asignacion.UserId, new UserXpDelta { Total = magnitud, Intercambios = magnitud }, operacion);
                DiscordRole rangoDespues = rangoRoles.GetRoleByXp(guild, actual.Total);
                string cambioRango = string.Empty;

                if (rangoAntes.Id != rangoDespues.Id)
                {
                    await rangoRoles.AplicarRangoAsync(member, rangoAntes, rangoDespues);
                    cambioRango = $" · {rangoAntes.Name} → {rangoDespues.Name}";
                }

                lineas.Add($"<@{asignacion.UserId}> · {Signo(asignacion.Xp)} XP{cambioRango}");
            }
            catch (Exception ex)
            {
                await logService.LogException(guild, ex, "Repartir XP - aplicar");
                lineas.Add($"<@{asignacion.UserId}> · {Signo(asignacion.Xp)} XP · ❌ no se pudo aplicar");
            }
        }

        return string.Join("\n", lineas);
    }

    /// <summary>Reedita el mensaje con el estado final y sin botones, para que no se pueda resolver dos veces.</summary>
    private async Task CerrarAsync(ComponentInteractionCreatedEventArgs args, string lista, string pie, DiscordColor color)
    {
        try
        {
            await args.Message.ModifyAsync(new DiscordMessageBuilder()
                .EnableV2Components()
                .AddContainerComponent(Contenedor(Encabezado(args.Message), lista, pie, color)));
        }
        catch (Exception ex)
        {
            await logService.LogException(args.Guild, ex, "Repartir XP - cerrar mensaje");
        }
    }

    private async Task<bool> EsKamiSamaAsync(ComponentInteractionCreatedEventArgs args)
    {
        if (args.Guild.Members.TryGetValue(args.User.Id, out DiscordMember? clicker) && clicker.Roles.Any(r => r.Id == config.Roles.KamiSama))
            return true;

        await RechazarAsync(args, "Solo un Kami Sama puede aprobar o denegar un reparto.");
        return false;
    }

    private static async Task RechazarAsync(ComponentInteractionCreatedEventArgs args, string motivo) =>
        await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder().AsEphemeral().WithContent(motivo));

    private static async Task AvisarAsync(ComponentInteractionCreatedEventArgs args, string mensaje) =>
        await args.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().AsEphemeral().WithContent(mensaje));

    private static Dictionary<ulong, string> Nombres(DiscordGuild guild, IEnumerable<ulong> ids) =>
        ids.ToDictionary(id => id, id => guild.Members.TryGetValue(id, out DiscordMember? member) ? member.DisplayName : id.ToString());

    private static string Encabezado(DiscordMessage mensaje) => Textos(mensaje).FirstOrDefault() ?? "## Reparto de XP";

    private static string TextoDelReparto(DiscordMessage mensaje) => string.Join("\n", Textos(mensaje).Skip(1));

    private static IEnumerable<string> Textos(DiscordMessage mensaje) =>
        (mensaje.Components ?? [])
            .OfType<DiscordContainerComponent>()
            .SelectMany(container => container.Components)
            .OfType<DiscordTextDisplayComponent>()
            .Select(texto => texto.Content);

    public static string TextoDelModal(ModalSubmittedEventArgs args, string campo = CampoXp) =>
        args.Values.TryGetValue(campo, out IModalSubmission? valor) && valor is TextInputModalSubmission texto
            ? texto.Value
            : string.Empty;
}
