using System.ComponentModel;
using AnilistConEnie.Application.Extensions;
using AnilistConEnie.Application.Xp;
using AnilistConEnie.Bot.Commands.Framework.Attributes;
using AnilistConEnie.Bot.Commands.Framework.Checks;
using AnilistConEnie.Bot.Commands.Framework.Choices;
using AnilistConEnie.Bot.Configuration;
using AnilistConEnie.Bot.Extensions;
using AnilistConEnie.Bot.Helpers;
using AnilistConEnie.Bot.Services;
using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;

namespace AnilistConEnie.Bot.Commands.Slash;

[RequireStaff]
[TestCommand]
public class RepartirXp(
    BotConfiguration config,
    DiscordBotService discordBotService,
    RepartoXpService repartoService,
    InteractivityExtension interactivity)
{
    private static readonly TimeSpan Espera = TimeSpan.FromMinutes(5);

    /// <summary>Discord admite 5 componentes por modal, y cada usuario ocupa uno (nombre + campo de XP).</summary>
    private const int PorPagina = 5;

    private const int MaxUsuarios = 25;
    private const int MaxDetalle = 60;
    private const string Paso1 = "### Paso 1 · Elige la categoría y los usuarios participantes";
    private const string Paso2 = "Paso 2 · Asignar XP";

    // El prefijo modal- evita el defer automático de ComponentInteractionHandler, que dejaría sin abrir el modal.
    private const string SelectCategoria = "modal-repartirxp-categoria";
    private const string BotonDetalle = "modal-repartirxp-detalle";
    private const string BotonCargar = "modal-repartirxp-cargar";
    private const string BotonAnterior = "modal-repartirxp-anterior";
    private const string BotonSiguiente = "modal-repartirxp-siguiente";
    private const string BotonContinuar = "modal-repartirxp-continuar";
    private const string BotonSalir = "modal-repartirxp-salir";

    [Command("repartir_xp")]
    [Description("Reparte xp entre varios usuarios, para que la apruebe un Kami Sama")]
    public async Task Repartir(
        SlashCommandContext ctx,
        [Parameter("Categoria")] [Description("Por que se reparte la xp")] CategoriaReparto categoriaInicial)
    {
        if (!await ctx.BotInicializadoAsync(discordBotService)) return;

        await ctx.DeferEphemeralAsync();

        if (!ctx.Guild!.Channels.TryGetValue(config.Channels.Colaboradores, out DiscordChannel? canal))
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(ErrorEmbed.De("No está configurado el canal de colaboradores.")));
            return;
        }

        CategoriaReparto categoria = categoriaInicial;
        string detalle = string.Empty;
        List<DiscordMember> elegidos = [];
        Dictionary<ulong, long> cargado = [];
        int pagina = 0;
        string aviso = string.Empty;
        List<AsignacionXp>? reparto = null;

        while (reparto is null)
        {
            pagina = Math.Clamp(pagina, 0, TotalPaginas(elegidos) - 1);

            DiscordMessage panel = await ctx.EditResponseAsync(
                Panel(Titulo(categoria, detalle), aviso, categoria, detalle, elegidos, cargado, pagina));

            InteractivityResult<ComponentInteractionCreatedEventArgs> accion = await interactivity
                .WaitForEventArgsAsync<ComponentInteractionCreatedEventArgs>(x => x.Message.Id == panel.Id && x.User.Id == ctx.User.Id, Espera);

            if (accion.TimedOut)
            {
                await Cerrar(ctx, $"{Titulo(categoria, detalle)}\nSe venció el tiempo, vuelve a invocar el comando.");
                return;
            }

            aviso = string.Empty;
            string id = accion.Result.Id;

            // El modal se abre respondiendo a la interacción; el resto de las acciones solo redibujan el panel.
            if (id is not (BotonCargar or BotonDetalle))
                await accion.Result.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);

            switch (id)
            {
                case BotonSalir:
                    await Cerrar(ctx, $"{Titulo(categoria, detalle)}\nReparto cancelado.");
                    return;

                case SelectCategoria:
                    categoria = Enum.Parse<CategoriaReparto>(accion.Result.Values[0]);
                    continue;

                case RepartoXpService.SelectUsuarios:
                    elegidos = Elegidos(ctx.Guild, accion.Result.Values);
                    pagina = 0;
                    if (elegidos.Count == 0)
                        aviso = "Ninguno de los usuarios elegidos es un miembro válido del servidor.";
                    continue;

                case BotonAnterior:
                    pagina--;
                    continue;

                case BotonSiguiente:
                    pagina++;
                    continue;

                case BotonContinuar:
                    List<AsignacionXp> asignaciones = Asignaciones(elegidos, cargado);
                    if (asignaciones.Count == 0)
                    {
                        aviso = "Carga XP distinta de cero para al menos un usuario.";
                        continue;
                    }
                    reparto = asignaciones;
                    continue;

                case BotonDetalle:
                    detalle = await PedirDetalle(accion.Result.Interaction, detalle) ?? detalle;
                    continue;

                case BotonCargar:
                    aviso = await PedirXp(accion.Result.Interaction, ctx.User, Pagina(elegidos, pagina), cargado);
                    pagina = ProximaPagina(elegidos, cargado, pagina);
                    continue;
            }
        }

        string titulo = Titulo(categoria, detalle);

        if (!await Confirmar(ctx, titulo, reparto))
        {
            await Cerrar(ctx, $"{titulo}\nReparto cancelado.");
            return;
        }

        DiscordMessage publicado = await repartoService.PublicarAsync(canal, $"{titulo}\nRepartido por {ctx.User.Mention}", reparto, ctx.User.Id);

        await Cerrar(ctx, $"{titulo}\nEl reparto quedó [esperando aprobación]({publicado.JumpLink}) en {canal.Mention}.");
    }

    /// <summary>
    /// Abre el modal con una tanda de participantes (un campo por usuario) y guarda lo cargado.
    /// Devuelve el aviso a mostrar en el panel, vacío si sali bien.
    /// </summary>
    private async Task<string> PedirXp(DiscordInteraction interaccion, DiscordUser autor, List<DiscordMember> tanda, Dictionary<ulong, long> cargado)
    {
        if (tanda.Count == 0)
        {
            await interaccion.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);
            return "No hay participantes para cargar.";
        }

        string modalId = $"modal-repartirxp-{interaccion.Id}";
        DiscordModalBuilder modal = new DiscordModalBuilder().WithCustomId(modalId).WithTitle(Paso2);

        foreach (DiscordMember member in tanda)
        {
            string actual = cargado.TryGetValue(member.Id, out long xp) ? xp.ToString() : "0";
            modal.AddTextInput(
                new DiscordTextInputComponent($"{member.Id}", "0", actual, true, DiscordTextInputStyle.Short, 1, 12),
                member.DisplayName);
        }

        await interaccion.CreateResponseAsync(DiscordInteractionResponseType.Modal, modal);

        InteractivityResult<ModalSubmittedEventArgs> respuesta = await interactivity.WaitForModalAsync(modalId, autor, Espera);
        if (respuesta.TimedOut) return "No cargaste la XP a tiempo, no se perdió nada.";

        await respuesta.Result.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);

        List<string> errores = [];
        foreach (DiscordMember member in tanda)
        {
            string valor = RepartoXpService.TextoDelModal(respuesta.Result, $"{member.Id}");
            if (RepartoXp.TryParseXp(valor, out long xp))
                cargado[member.Id] = xp;
            else
                errores.Add($"- `{valor.Trim()}` no es un número válido ({member.DisplayName}).");
        }

        return errores.Count > 0 ? $"No se guardó la XP de todos:\n{string.Join("\n", errores)}" : string.Empty;
    }

    private async Task<string?> PedirDetalle(DiscordInteraction interaccion, string detalle)
    {
        string modalId = $"modal-repartirxp-detalle-{interaccion.Id}";
        DiscordModalBuilder modal = new DiscordModalBuilder()
            .WithCustomId(modalId)
            .WithTitle("Que se reparte")
            .AddTextInput(
                new DiscordTextInputComponent(RepartoXpService.CampoDetalle, "Karaoke, juegos, etc", detalle, true, DiscordTextInputStyle.Short, 1, MaxDetalle),
                "Que se reparte");

        await interaccion.CreateResponseAsync(DiscordInteractionResponseType.Modal, modal);

        InteractivityResult<ModalSubmittedEventArgs> respuesta = await interactivity.WaitForModalAsync(modalId, interaccion.User, Espera);
        if (respuesta.TimedOut) return null;

        await respuesta.Result.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);

        return Limpiar(RepartoXpService.TextoDelModal(respuesta.Result, RepartoXpService.CampoDetalle));
    }

    /// <summary>
    /// Panel de armado del reparto. Vive en el mismo mensaje para que cambiar la categoría o los
    /// participantes no obligue a rehacer lo demás: lo ya cargado se reinyecta en el modal de cada tanda.
    /// </summary>
    private static DiscordWebhookBuilder Panel(
        string titulo,
        string aviso,
        CategoriaReparto categoria,
        string detalle,
        List<DiscordMember> elegidos,
        Dictionary<ulong, long> cargado,
        int pagina)
    {
        bool faltaDetalle = categoria == CategoriaReparto.Otros && detalle.Length == 0;
        int totalPaginas = TotalPaginas(elegidos);
        int listos = elegidos.Count(x => cargado.ContainsKey(x.Id));

        string paso = elegidos.Count == 0 ? Paso1 : $"### {Paso2}";
        string encabezado = aviso.Length > 0 ? $"{titulo}\n{paso}\n{aviso}" : $"{titulo}\n{paso}";

        string lista = elegidos.Count == 0
            ? "-# Todavía no elegiste participantes."
            : string.Join("\n", elegidos.Select(x => cargado.TryGetValue(x.Id, out long xp)
                ? $"✅ {x.Mention} · {RepartoXpService.Signo(xp)} XP"
                : $"⬜ {x.Mention} · sin cargar"));

        string pie = elegidos.Count == 0
            ? "-# Nada se aplica hasta que un Kami Sama lo apruebe."
            : $"-# {listos} de {elegidos.Count} cargados · nada se aplica hasta que un Kami Sama lo apruebe.";

        DiscordSelectComponent selectCategoria = new(SelectCategoria, "Categoría del reparto",
            Enum.GetValues<CategoriaReparto>().Select(x => new DiscordSelectComponentOption(
                ((Enum)x).GetDescription(), x.ToString(), isDefault: x == categoria)));

        DiscordUserSelectComponent selectUsuarios = new(RepartoXpService.SelectUsuarios, "Participantes del reparto", false, 1, MaxUsuarios);
        selectUsuarios.AddDefaultUsers(elegidos.Select(x => x.Id));

        DiscordWebhookBuilder builder = new DiscordWebhookBuilder()
            .EnableV2Components()
            .AddContainerComponent(RepartoXpService.Contenedor(encabezado, lista, pie, DiscordColor.Orange))
            .AddActionRowComponent(selectCategoria)
            .AddActionRowComponent(selectUsuarios);

        if (categoria == CategoriaReparto.Otros)
            builder.AddActionRowComponent(new DiscordButtonComponent(
                faltaDetalle ? DiscordButtonStyle.Primary : DiscordButtonStyle.Secondary,
                BotonDetalle,
                detalle.Length == 0 ? "Escribir qué se reparte" : $"Qué se reparte: {detalle}"));

        List<DiscordButtonComponent> acciones = [];

        if (totalPaginas > 1)
            acciones.Add(new DiscordButtonComponent(DiscordButtonStyle.Secondary, BotonAnterior, "◀", pagina == 0));

        acciones.Add(new DiscordButtonComponent(DiscordButtonStyle.Primary, BotonCargar, EtiquetaCargar(elegidos, pagina), elegidos.Count == 0));

        if (totalPaginas > 1)
            acciones.Add(new DiscordButtonComponent(DiscordButtonStyle.Secondary, BotonSiguiente, "▶", pagina >= totalPaginas - 1));

        acciones.Add(new DiscordButtonComponent(DiscordButtonStyle.Success, BotonContinuar, "Continuar", listos == 0 || faltaDetalle));
        acciones.Add(new DiscordButtonComponent(DiscordButtonStyle.Secondary, BotonSalir, "Cancelar"));

        return builder.AddActionRowComponent(acciones);
    }

    private static string EtiquetaCargar(List<DiscordMember> elegidos, int pagina)
    {
        if (elegidos.Count <= PorPagina) return "Cargar XP";

        int desde = pagina * PorPagina + 1;
        int hasta = Math.Min(desde + PorPagina - 1, elegidos.Count);
        return $"Cargar XP ({desde}-{hasta} de {elegidos.Count})";
    }

    private static int TotalPaginas(List<DiscordMember> elegidos) =>
        Math.Max(1, (int)Math.Ceiling(elegidos.Count / (double)PorPagina));

    private static List<DiscordMember> Pagina(List<DiscordMember> elegidos, int pagina) =>
        elegidos.Skip(pagina * PorPagina).Take(PorPagina).ToList();

    /// <summary>Después de cargar una tanda salta a la primera que todavía tenga usuarios sin XP.</summary>
    private static int ProximaPagina(List<DiscordMember> elegidos, Dictionary<ulong, long> cargado, int pagina)
    {
        for (int i = 0; i < TotalPaginas(elegidos); i++)
        {
            if (Pagina(elegidos, i).Any(x => !cargado.ContainsKey(x.Id)))
                return i;
        }
        return pagina;
    }

    private static List<AsignacionXp> Asignaciones(List<DiscordMember> elegidos, Dictionary<ulong, long> cargado) =>
        elegidos.Where(x => cargado.GetValueOrDefault(x.Id) != 0)
            .Select(x => new AsignacionXp(x.Id, cargado[x.Id]))
            .ToList();

    /// <summary>Con "Otros" el título lo pone el staff; hasta que lo cargue se muestra la opción elegida.</summary>
    private static string Titulo(CategoriaReparto categoria, string detalle) =>
        $"## Reparto de XP · {(categoria == CategoriaReparto.Otros && detalle.Length > 0 ? detalle : ((Enum)categoria).GetDescription())}";

    private static string Limpiar(string detalle)
    {
        string limpio = string.Join(" ", detalle.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return limpio.Length > MaxDetalle ? limpio[..MaxDetalle] : limpio;
    }

    private async Task<bool> Confirmar(SlashCommandContext ctx, string titulo, IReadOnlyList<AsignacionXp> asignaciones)
    {
        DiscordMessage mensaje = await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .EnableV2Components()
            .AddContainerComponent(RepartoXpService.Contenedor(
                $"{titulo}\n### Paso 3 · Revisa el reparto antes de mandarlo a aprobación",
                RepartoXp.Renderizar(asignaciones),
                RepartoXpService.Resumen(asignaciones),
                DiscordColor.Orange))
            .AddActionRowComponent(
                new DiscordButtonComponent(DiscordButtonStyle.Success, "reparto-confirmar", "Confirmar"),
                new DiscordButtonComponent(DiscordButtonStyle.Secondary, "reparto-cancelar", "Cancelar")));

        InteractivityResult<ComponentInteractionCreatedEventArgs> respuesta = await interactivity.WaitForButtonAsync(mensaje, ctx.User, Espera);
        if (respuesta.TimedOut) return false;

        await respuesta.Result.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);
        return respuesta.Result.Id == "reparto-confirmar";
    }

    private static async Task Cerrar(SlashCommandContext ctx, string texto) =>
        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .EnableV2Components()
            .AddContainerComponent(new DiscordContainerComponent([new DiscordTextDisplayComponent(texto)], color: DiscordColor.Orange)));

    private static List<DiscordMember> Elegidos(DiscordGuild guild, IReadOnlyList<string> valores)
    {
        List<DiscordMember> elegidos = [];
        foreach (string valor in valores)
        {
            if (ulong.TryParse(valor, out ulong id)
                && guild.Members.TryGetValue(id, out DiscordMember? member)
                && !member.IsBot
                && elegidos.All(x => x.Id != id))
                elegidos.Add(member);
        }
        return elegidos;
    }
}
