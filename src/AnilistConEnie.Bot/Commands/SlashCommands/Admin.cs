using System.ComponentModel;
using System.Text.RegularExpressions;
using AnilistConEnie.Application.Extensions;
using AnilistConEnie.Application.Helpers;
using AnilistConEnie.Bot.Commands.Enums;
using AnilistConEnie.Bot.Commands.SlashCommands.Attributes;
using AnilistConEnie.Bot.Configuration;
using AnilistConEnie.Bot.Helpers;
using AnilistConEnie.Bot.Services;
using AnilistConEnie.Bot.Services.State;
using AnilistConEnie.Model.Entities;
using AnilistConEnie.Model.Entities.Anilist;
using AnilistConEnie.Model.Enum;
using AnilistConEnie.Model.Interfaces;
using AnilistConEnie.Model.Interfaces.Repositories;
using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace AnilistConEnie.Bot.Commands.SlashCommands;

[Command("admin")]
[Description("Comandos de los admins del servidor")]
[TestCommand]
public class Admin(
    BotConfiguration config,
    DiscordBotService discordBotService,
    DiscordLogService logService,
    EmoteModeState emoteModeState,
    InviteLinkState inviteLinkState,
    AnilistUsersState anilistUsersState,
    XpState xpState,
    IAnilistClient anilistClient,
    IUsuariosActivosRepository usuariosActivosRepository,
    IUsuariosDiscordRepository usuariosDiscordRepository)
{
    private const string PddSuggestionForm = "https://forms.gle/o3V3pgu2hfYRBVtu7";
    private const string PddThumbnail = "https://images-ext-1.discordapp.net/external/2FcgVgNDv60ja04L_9-_3DsxW2wqOd8Byj5rjr8EEvE/%3Fv%3D1/https/cdn.discordapp.com/emojis/846813702434586694.png?format=webp&quality=lossless";
    private const string PddFooterIcon = "https://media.discordapp.net/attachments/858877547336957986/865309515367841812/imagen_2021-07-15_150953_1.png";

    [Command("kickearnoverificados")]
    [Description("Expulsa del servidor a los miembros no verificados")]
    public async Task KickNoVerificados(SlashCommandContext ctx)
    {
        await ctx.DeferResponseAsync(true);
        if (!await EnsureAdminAsync(ctx)) return;

        DiscordRole noVerificadoRole = ctx.Guild!.Roles[config.Roles.NoVinculado];
        int kickeados = 0;

        List<DiscordMember> miembrosNoVerificados = ctx.Guild.Members.Values.Where(x => x.Roles.Contains(noVerificadoRole)).ToList();
        foreach (DiscordMember memberToKick in miembrosNoVerificados)
        {
            await memberToKick.RemoveAsync("AUTO KICK POR NO VINCULAR ANILIST");
            kickeados++;
        }

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Miembros expulsados por no verificarse: {kickeados}"));
    }

    [Command("permitirinvite")]
    [Description("Permite que el miembro pase links de invitacion")]
    public async Task PermitirInvite(
        SlashCommandContext ctx,
        [Parameter("Usuario")] [Description("Miembro para habilitar invites")] DiscordUser user)
    {
        await ctx.DeferResponseAsync();
        if (!await EnsureAdminAsync(ctx)) return;

        DiscordMember member = await ctx.Guild!.GetMemberAsync(user.Id);
        DiscordRole role = ctx.Guild.Roles[config.Roles.Invite];

        if (member.Roles.All(y => y.Id != role.Id))
        {
            await member.GrantRoleAsync(role);
            inviteLinkState.AddLinkRoleUser(member.Id, DateTime.Now.AddMinutes(5));

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
            {
                Title = "Invitaciones permitidas",
                Description = $"{member.Mention}, puedes mandar invitaciones a otros servidores por los próximos 5 minutos.",
                Color = DiscordColor.Green
            }).AddMention(new UserMention(member.Id)));
        }
        else
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
            {
                Title = "Error",
                Description = $"{member.Mention} ya tiene permiso para mandar invitaciones a otros servidores.",
                Color = DiscordColor.Red
            }));
        }
    }

    [Command("emotemodeenable")]
    [Description("Activa el emote mode (Staff)")]
    public async Task EmoteModeEnable(
        SlashCommandContext ctx,
        [Parameter("Emote")] [Description("El emote que quieres utilizar")] string emojiStr)
    {
        await ctx.DeferResponseAsync();
        if (!await EnsureAdminAsync(ctx)) return;

        DiscordEmoji? emote = DiscordEmojiHelper.ToEmoji(ctx.Client, emojiStr);
        if (emote is null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
            {
                Title = "Error",
                Description = "Debes pasar un emoji",
                Color = DiscordColor.Red
            }));
            return;
        }

        string name = emote.Name + "mode";
        if (!emoteModeState.YepMode)
        {
            emoteModeState.ActivarYepMode(emote);

            DiscordEmbedBuilder embed = new() { Title = $"{name} activado" };
            if (emote.Id != 0) embed.WithImageUrl(emote.Url);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
        }
        else
        {
            emoteModeState.ActivarYepMode(emote);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
            {
                Title = "Emote cambiado",
                Description = $"El {name} ahora tiene asignado otro emote"
            }));
        }
    }

    [Command("emotemodedisable")]
    [Description("Desactiva el emote mode (Staff)")]
    public async Task EmoteModeDisable(SlashCommandContext ctx)
    {
        await ctx.DeferResponseAsync();
        if (!await EnsureAdminAsync(ctx)) return;

        if (emoteModeState is { YepMode: true, Emote: not null })
        {
            string name = emoteModeState.Emote.Name + "mode";
            emoteModeState.DesactivarYepMode();

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder { Title = $"{name} desactivado" }));
        }
        else
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
            {
                Title = "Error",
                Description = "El emote mode no estaba activado"
            }));
        }
    }

    [Command("desvinculados")]
    [Description("Muestra los perfiles que no tienen cuenta de AniList vinculada")]
    public async Task Desvinculados(SlashCommandContext ctx)
    {
        await ctx.DeferResponseAsync();
        if (!await EnsureAdminAsync(ctx)) return;

        List<UsuarioAnilist> vinculadosFirebase = anilistUsersState.Usuarios;

        List<DiscordMember> vinculados = ctx.Guild!.Members.Values
            .Where(member => vinculadosFirebase.Any(x => (ulong)x.UserId == member.Id))
            .ToList();

        List<DiscordMember> noVinculados = ctx.Guild.Members.Values.Except(vinculados).ToList();
        List<DiscordMember> noVinculadosNoBot = noVinculados.Where(x => !x.IsBot).ToList();

        string desc = "**Usuarios sin AniList vinculado:**\n" +
                      string.Join("\n", noVinculadosNoBot.Select(member => $"{member.DisplayName} (<@{member.Id}>)")) +
                      $"\n\nTotal: {noVinculadosNoBot.Count}";

        DiscordEmbedBuilder embed = new()
        {
            Color = DiscordColor.Red,
            Title = "Usuarios sin cuenta vinculada de AniList"
        };

        InteractivityExtension interactivity = ctx.ServiceProvider.GetRequiredService<InteractivityExtension>();
        IEnumerable<Page> pages = InteractivityExtension.GeneratePagesInEmbed(desc, SplitType.Line, embed);
        await interactivity.SendPaginatedResponseAsync(ctx.Interaction, ephemeral: false, ctx.User, pages);
    }

    [Command("xp")]
    [Description("Administra la experiencia de los usuarios")]
    public async Task Xp(
        SlashCommandContext ctx,
        [Parameter("Usuarios")] [Description("Los usuarios a agregar o quitar xp")] string usuarios,
        [Parameter("Accion")] [Description("Agregar o quitar xp")] AccionXp accion,
        [Parameter("Xp")] [Description("Xp a agregar o quitar")] double xp,
        [Parameter("Tipo")] [Description("Categoria a la que pertenece la xp")] TipoXp categoria,
        [Parameter("DarXp")] [Description("Indica si agrega o no xp")] bool darXp = true)
    {
        await ctx.DeferResponseAsync(true);
        if (!await EnsureAdminAsync(ctx)) return;

        DiscordChannel channel = ctx.Guild!.Channels[config.Channels.ConfigBots];

        List<DiscordMember> members = [];
        foreach (Match m in Regex.Matches(usuarios, @"<@!?(\d+)>"))
        {
            if (ulong.TryParse(m.Groups[1].Value, out ulong id)
                && ctx.Guild.Members.TryGetValue(id, out DiscordMember? member)
                && members.All(x => x.Id != id))
                members.Add(member);
        }

        if (members.Count == 0)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                .WithTitle("Error")
                .WithDescription("No se encontró ningún usuario en el parámetro. Menciona a los usuarios.")
                .WithColor(DiscordColor.Red)));
            return;
        }

        if (categoria is TipoXp.Total or TipoXp.Booster)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                .WithTitle("Experiencia no agregada")
                .WithColor(DiscordColor.Red)
                .WithDescription("No se puede agregar xp de esta categoria, elegir otra.")));
            return;
        }

        string accionStr = ((Enum)accion).GetDescription();
        string desc1 = $"{string.Join("\n", members.Select(x => $"- {x.Mention}"))}\n";
        string desc2 = categoria switch
        {
            TipoXp.Eventos => $"### [{accionStr}] {xp} Xp\nPor haber participado en un evento o actividad.",
            TipoXp.Intercambios => $"### [{accionStr}] {xp} Xp\nPor haber participado en un intercambio.",
            TipoXp.Challenges => $"### [{accionStr}] {xp} Xp\nPor haber hecho un challenge.\n\n## Esta acción se hace automáticamente cuando se completa el challenge.",
            _ => $"### [{accionStr}] {xp} Xp\nPor haber algo que no es ni un evento, ni un intercambio, ni un challenge."
        };
        string desc = desc1 + desc2;
        int action = accion == AccionXp.Agregar ? 0 : 1;
        XpOperation operation = accion == AccionXp.Agregar ? XpOperation.Add : XpOperation.Remove;
        string accionTitulo = darXp ? $"{accionStr} Xp?" : $"Agregar xp a categoria {((Enum)categoria).GetDescription().ToLower()} sin dar xp?";

        bool confirmed = await DiscordInteractivity.GetSiNoInteractivity(ctx, accionTitulo, desc);

        if (confirmed)
        {
            foreach (DiscordMember miembro in members)
            {
                try
                {
                    UserXp memberXp = xpState.GetUserXp(miembro.Id);
                    long totalXp = action == 0 ? memberXp.Total + (long)xp : memberXp.Total - (long)xp;
                    long categoryXp = 0;

                    int eventos = 0, intercambios = 0, challenges = 0, otros = 0;
                    switch (categoria)
                    {
                        case TipoXp.Eventos:
                            eventos = (int)xp;
                            categoryXp = action == 0 ? memberXp.Eventos + (long)xp : memberXp.Eventos - (long)xp;
                            break;
                        case TipoXp.Intercambios:
                            intercambios = (int)xp;
                            categoryXp = action == 0 ? memberXp.Intercambios + (long)xp : memberXp.Intercambios - (long)xp;
                            break;
                        case TipoXp.Challenges:
                            challenges = (int)xp;
                            categoryXp = action == 0 ? memberXp.Challenges + (long)xp : memberXp.Challenges - (long)xp;
                            break;
                        default:
                            otros = (int)xp;
                            categoryXp = action == 0 ? memberXp.Otros + (long)xp : memberXp.Otros - (long)xp;
                            break;
                    }

                    xpState.UpdateUserXp(miembro.Id, categoryXp, categoria);

                    if (darXp)
                    {
                        xpState.UpdateUserXp(miembro.Id, totalXp, TipoXp.Total);
                        await usuariosDiscordRepository.AddOrRemoveUserXp(miembro.Id, new UserXpDelta { Total = (int)xp, Challenges = challenges, Eventos = eventos, Intercambios = intercambios, Otros = otros }, operation);

                        await channel.SendMessageAsync(new DiscordEmbedBuilder()
                            .WithTitle("Experiencia modificada")
                            .WithColor(DiscordColor.Green)
                            .WithDescription($"## - {miembro.DisplayName} ({miembro.Mention})\n{desc2}")
                            .WithThumbnail(miembro.GuildAvatarUrl ?? miembro.AvatarUrl));
                    }
                    else
                    {
                        await usuariosDiscordRepository.AddOrRemoveUserXp(miembro.Id, new UserXpDelta { Challenges = challenges, Eventos = eventos, Intercambios = intercambios, Otros = otros }, operation);

                        await channel.SendMessageAsync(new DiscordEmbedBuilder()
                            .WithTitle("Experiencia modificada sin dar xp")
                            .WithColor(DiscordColor.Green)
                            .WithDescription(desc)
                            .WithThumbnail(miembro.GuildAvatarUrl ?? miembro.AvatarUrl));
                    }
                }
                catch (Exception ex)
                {
                    await logService.GrabarLogGeneralError(ctx.Guild, $"Error agregando xp\n{ex.Message}\n{Formatter.BlockCode(ex.StackTrace ?? string.Empty)}");

                    await channel.SendMessageAsync(new DiscordEmbedBuilder()
                        .WithTitle("Experiencia no agregada")
                        .WithColor(DiscordColor.Red)
                        .WithDescription($"Ocurrió un error modificando la experiencia de {miembro.DisplayName}."));
                }
            }
        }

        await ctx.DeleteResponseAsync();
    }

    [Command("repartir_intercambio")]
    [Description("Reparte los intercambios")]
    public async Task RepartirIntercambio(
        SlashCommandContext ctx,
        [Parameter("Version")] [Description("Version del algoritmo")] VersionReparto version)
    {
        if (ctx.Member is null || ctx.Member.Roles.All(r => r.Id != config.Roles.KamiSama))
        {
            await ctx.DeferResponseAsync(true);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(SinPermisoEmbed()));
            return;
        }

        const string placeholder = "Eli 1, Nai 1, Pepe 3, Gaby 2, etc";
        string modalId = $"modal-{ctx.Interaction.Id}";
        string customId = $"{ctx.User.Id}";

        DiscordModalBuilder modal = new DiscordModalBuilder()
            .WithCustomId(modalId)
            .WithTitle("Organizar intercambio")
            .AddTextInput(new DiscordTextInputComponent(customId, placeholder, null, true, DiscordTextInputStyle.Paragraph), "Opciones");

        await ctx.Interaction.CreateResponseAsync(DiscordInteractionResponseType.Modal, modal);

        InteractivityExtension interactivity = ctx.ServiceProvider.GetRequiredService<InteractivityExtension>();
        InteractivityResult<ModalSubmittedEventArgs> modalInteraction = await interactivity.WaitForModalAsync(modalId, ctx.User, TimeSpan.FromMinutes(5));

        if (modalInteraction.TimedOut) return;

        await modalInteraction.Result.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);

        string value = modalInteraction.Result.Values[customId] is TextInputModalSubmission texto ? texto.Value : string.Empty;
        string[] values = value.Split(',');

        Dictionary<string, int> pelisPorRecibir = [];
        foreach (string kvp in values)
        {
            string[] r = kvp.Trim().Split(' ');
            pelisPorRecibir[r[0]] = int.Parse(r[1]);
        }

        pelisPorRecibir = pelisPorRecibir.Shuffle();

        DiscordChannel canal = ctx.Guild!.Channels[config.Channels.Playroom];

        if (version == VersionReparto.GPT)
        {
            (List<string> reparto, Dictionary<string, int> detalle) = IntercambioHelper.RepartirGPT(pelisPorRecibir);

            DiscordMessageBuilder builder = new DiscordMessageBuilder()
                .AddEmbed(new DiscordEmbedBuilder()
                    .WithTitle("Input")
                    .WithDescription(value)
                    .WithColor(DiscordColor.Yellow))
                .AddEmbed(new DiscordEmbedBuilder()
                    .WithTitle($"Reparto para intercambio {version}")
                    .WithDescription(string.Join("\n", reparto))
                    .WithColor(DiscordColor.Green))
                .AddEmbed(new DiscordEmbedBuilder()
                    .WithTitle("Detalle de usuarios que reparten")
                    .WithDescription(string.Join("\n", detalle.Select(x => $"{x.Key} - {x.Value}")))
                    .WithColor(DiscordColor.MidnightBlue));

            await canal.SendMessageAsync(builder);
        }
        else
        {
            List<string> resultClasico = IntercambioHelper.RepartirClasico(pelisPorRecibir);

            await canal.SendMessageAsync(new DiscordEmbedBuilder()
                .WithTitle($"Reparto para intercambio {version}")
                .WithDescription($"{value}\n\n" + string.Join("\n", resultClasico))
                .WithColor(DiscordColor.Green));
        }
    }

    [Command("pdd")]
    [Description("Postea la pregunta del dia")]
    public async Task Pdd(
        SlashCommandContext ctx,
        [Parameter("Titulo")] [Description("Titulo de la pregunta del dia")] string titulo,
        [Parameter("Imagen")] [Description("Link del gif a mostrar")] string imagen,
        [Parameter("Source")] [Description("De que es el gif a mostrar")] string source)
    {
        await ctx.DeferResponseAsync(true);
        if (!await EnsureAdminAsync(ctx)) return;

        DiscordChannel channel = ctx.Guild!.Channels[config.Channels.Pdd];
        DiscordEmoji emote = await DiscordEmojiHelper.GetApplicationEmojiAsync(ctx.Client, config.Emotes.GatoVanguard.Get(discordBotService.Debug));
        DiscordRole rol = ctx.Guild.Roles[config.Roles.Pdd];

        DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
            .WithThumbnail(PddThumbnail)
            .WithTitle(titulo)
            .WithDescription($"{Formatter.Italic($"---- gif: {source}")}\n¿Que pregunta quieres que se haga? {Formatter.MaskedUrl("Click acá", new Uri(PddSuggestionForm))}")
            .WithImageUrl(imagen)
            .WithFooter("Sugerir preguntas disminuye tu porcentaje de boludito", PddFooterIcon)
            .WithColor(DiscordColor.Blue);

        bool ok = await DiscordInteractivity.GetSiNoInteractivity(ctx, "Confirmar PDD", "Confirma la creación de esta pregunta del día", embed.Build());

        if (!ok)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                .WithTitle("PDD no creada")
                .WithDescription("Se canceló la creación de la pregunta del día")
                .WithColor(DiscordColor.Red)));
            return;
        }

        DiscordMessage msg = await channel.SendMessageAsync(new DiscordMessageBuilder()
            .WithContent($"Pregunta del dia {emote} {rol.Mention}")
            .AddEmbed(embed)
            .WithAllowedMention(new RoleMention(rol.Id)));

        string hoyString = DateTime.Today.ToString("yy MM dd");

        await channel.CreateThreadAsync(msg, hoyString, DiscordAutoArchiveDuration.Week);

        var threads = await ctx.Guild.ListActiveThreadsAsync();
        foreach (DiscordThreadChannel thread in threads.Threads)
        {
            if (thread.ParentId == config.Channels.Pdd && thread.Name.Trim() != hoyString && thread.ThreadMetadata?.IsArchived == false)
            {
                await thread.ModifyAsync(x =>
                {
                    x.IsArchived = true;
                    x.Locked = true;
                });
            }
        }

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
            .WithTitle("PDD Creada con éxito")
            .WithDescription("Se creó la pdd y se archivaron los hilos de las pdd anteriores")
            .WithColor(DiscordColor.Green)));
    }

    [Command("kickinactivos")]
    [Description("Kick de miembros inactivos")]
    public async Task KickInactivos(SlashCommandContext ctx)
    {
        await ctx.DeferResponseAsync();
        if (!await EnsureAdminAsync(ctx)) return;

        HashSet<ulong> memberIds = ctx.Guild!.Members.Keys.ToHashSet();
        List<UsuarioActivo> inactivos = await usuariosActivosRepository.GetUsuariosInactivos(memberIds, 12);
        List<ulong> kickeables = [];

        foreach (DiscordMember member in ctx.Guild.Members.Values)
        {
            if (xpState.GetUserXp(member.Id).Total < 1000 && inactivos.Any(x => x.UserId == (long)member.Id))
                kickeables.Add(member.Id);
        }

        string membersStr = string.Join("\n", kickeables.Select(x => ctx.Guild.Members[x].DisplayName));
        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
            .WithTitle("Miembros inactivos hace mas de un año con menos de 1000 de xp")
            .WithDescription(membersStr)));
    }

    [Command("banearanilist")]
    [Description("Banea un perfil de AniList")]
    public async Task BanearAnilist(
        SlashCommandContext ctx,
        [Parameter("Username")] [Description("Nombre de usuario de AniList a banear")] string userName)
    {
        await ctx.DeferResponseAsync();
        if (!await EnsureAdminAsync(ctx)) return;
        if (!await EnsureCanalPermitidoAsync(ctx)) return;

        AnilistUser? perfil = await anilistClient.SearchUserAsync(userName);
        if (perfil is null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
            {
                Title = "Error",
                Description = "No se encontró el perfil de AniList con ese nombre de usuario.",
                Color = DiscordColor.Red
            }));
            return;
        }

        await anilistUsersState.AddAnilistUserBaneado(perfil.Id);

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
        {
            Title = "Perfil baneado",
            Description = $"El perfil de AniList con el nombre de usuario `{userName}` ha sido baneado correctamente.",
            Color = DiscordColor.Green
        }));
    }

    [Command("desbanearanilist")]
    [Description("Desbanea un perfil de AniList")]
    public async Task DesbanearAnilist(
        SlashCommandContext ctx,
        [Parameter("Username")] [Description("Nombre de usuario de AniList a desbanear")] string userName)
    {
        await ctx.DeferResponseAsync();
        if (!await EnsureAdminAsync(ctx)) return;
        if (!await EnsureCanalPermitidoAsync(ctx)) return;

        AnilistUser? perfil = await anilistClient.SearchUserAsync(userName);
        if (perfil is null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
            {
                Title = "Error",
                Description = "No se encontró el perfil de AniList con ese nombre de usuario.",
                Color = DiscordColor.Red
            }));
            return;
        }

        await anilistUsersState.RemoveAnilistUserBaneado(perfil.Id);

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
        {
            Title = "Perfil desbaneado",
            Description = $"El perfil de AniList con el nombre de usuario `{userName}` ha sido desbaneado correctamente.",
            Color = DiscordColor.Green
        }));
    }

    [Command("baneadosanilist")]
    [Description("Lista de perfiles baneados de AniList")]
    public async Task BaneadosAnilist(SlashCommandContext ctx)
    {
        await ctx.DeferResponseAsync();
        if (!await EnsureAdminAsync(ctx)) return;
        if (!await EnsureCanalPermitidoAsync(ctx)) return;

        List<int> listaBaneados = anilistUsersState.GetAnilistUsersBaneados();
        string listaBaneadosStr = string.Join("\n", listaBaneados.Select(x => $"- https://anilist.co/user/{x}"));

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
        {
            Title = "Perfiles baneados",
            Description = listaBaneadosStr,
            Color = DiscordColor.Green
        }));
    }

    [Command("rolreaccion")]
    [Description("Agrega un rol a los que hayas reaccionado a un mensaje con un emoji")]
    public async Task RolReaccion(
        SlashCommandContext ctx,
        [Parameter("Rol")] [Description("Rol a asignar")] DiscordRole rol,
        [Parameter("Emoji")] [Description("Emoji de reaccion para obtener el rol")] string emoji,
        [Parameter("Channel")] [Description("Canal donde fue puesto el mensaje")] DiscordChannel canalMensaje,
        [Parameter("IdMensaje")] [Description("Id del mensaje de anuncio")] string mensajeId)
    {
        await ctx.DeferResponseAsync();
        if (!await EnsureAdminAsync(ctx)) return;

        try
        {
            if (!ulong.TryParse(mensajeId, out ulong mensajeIdUlong))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("⚠️ Message ID is not valid!"));
                return;
            }

            DiscordMessage msgAnuncio = await canalMensaje.GetMessageAsync(mensajeIdUlong);

            MatchCollection matches = Regex.Matches(emoji, @"<a?:(.+?):(\d+)>");
            if (matches.Count is not 1)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("⚠️ Referenced emoji not found!"));
                return;
            }

            DiscordEmoji emote = DiscordEmoji.FromGuildEmote(ctx.Client, ulong.Parse(matches[0].Groups[2].Value));

            List<DiscordMember> rolAgregado = [];
            await foreach (DiscordUser reaccionante in msgAnuncio.GetReactionsAsync(emote))
            {
                if (!ctx.Guild!.Members.TryGetValue(reaccionante.Id, out DiscordMember? member)) continue;
                if (member.Roles.All(y => y.Id != rol.Id))
                {
                    try
                    {
                        await member.GrantRoleAsync(rol);
                        rolAgregado.Add(member);
                    }
                    catch (Exception) { /* Ignored */ }
                }
            }

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder()
                .WithTitle("Roles agregados")
                .WithColor(DiscordColor.Green)
                .WithDescription($"Se agrego el rol {rol.Mention} a los siguientes usuarios\n\n{string.Join("\n", rolAgregado.Select(x => $"{x.DisplayName} ({x.Mention})"))}")));
        }
        catch (Exception ex)
        {
            await logService.GrabarLogGeneralError(ctx.Guild!, $"{ex.Message}\n\n{Formatter.BlockCode(ex.StackTrace ?? string.Empty)}");
        }
    }

    private async Task<bool> EnsureAdminAsync(SlashCommandContext ctx)
    {
        if (ctx.Member is not null && ctx.Member.Roles.Any(r => r.Id == config.Roles.KamiSama))
            return true;

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(SinPermisoEmbed()));
        return false;
    }

    private async Task<bool> EnsureCanalPermitidoAsync(SlashCommandContext ctx)
    {
        if (ctx.Channel.Id == config.Channels.Moderacion
            || ctx.Channel.Id == config.Channels.ConfigBots
            || ctx.Channel.Id == config.Channels.Playroom)
            return true;

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
        {
            Title = "Error",
            Description = "Este comando solo se puede usar en el canal de administración o en el canal de config bots.",
            Color = DiscordColor.Red
        }));
        return false;
    }

    private static DiscordEmbedBuilder SinPermisoEmbed() => new()
    {
        Title = "Sin permiso",
        Description = "Solo los administradores pueden usar este comando.",
        Color = DiscordColor.Red
    };
}
