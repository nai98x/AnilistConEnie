using AnilistConEnie.Bot.Configuration;
using AnilistConEnie.Bot.Helpers;
using AnilistConEnie.Bot.Services;
using AnilistConEnie.Model.Interfaces.Repositories;
using AnilistConEnie.Model.Entities;
using AnilistConEnie.Model.Entities.Anilist;
using AnilistConEnie.Model.Enum;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace AnilistConEnie.Bot.Events.Handlers;

public class ComponentInteractionHandler(DiscordBotService discordBotService, BotConfiguration config, RangoRoles rangoRoles, DiscordLogService logService, AnilistService anilistService, IAnilistApprovalRepository anilistApprovalRepository, IHttpClientFactory httpClientFactory)
{
    public async Task Handle(DiscordClient client, ComponentInteractionCreatedEventArgs args)
    {
        if (args.Guild?.Id != config.GuildId) return;

        if (args.Interaction.Data.CustomId.StartsWith("modal-anilistprofileset") && !discordBotService.Debug)
        {
            await anilistService.VincularAniList(args.Interaction, client);
        }
        else if (args.Interaction.Data.CustomId.StartsWith("colores"))
        {
            await args.Interaction.DeferAsync(true);

            bool ok = false;
            Dictionary<ulong, RangoEnum> coloresPorRango = config.Roles.ColoresRango.ToDictionary(c => c.RoleId, c => Enum.Parse<RangoEnum>(c.Rango));
            if (!args.Guild.Members.TryGetValue(args.User.Id, out DiscordMember? member)
                || !ulong.TryParse(args.Interaction.Data.Values.FirstOrDefault(), out ulong chosenColorId)
                || !coloresPorRango.ContainsKey(chosenColorId)
                || !args.Guild.Roles.TryGetValue(chosenColorId, out DiscordRole? newColor))
            {
                await args.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().AddEmbed(ErrorEmbed.De("El color elegido no es válido")));
                return;
            }
            KeyValuePair<ulong, RangoEnum> chosenColor = coloresPorRango.FirstOrDefault(x => x.Key == chosenColorId);

            if (rangoRoles.RangoAPartirDe(args.Guild, member, chosenColor.Value, false))
            {
                // do the actual color change (sacar el viejo si existe (pueden existir más de 1, usar where) y poner el nuevo)
                IEnumerable<DiscordRole> oldColors = member.Roles.Where(x => coloresPorRango.ContainsKey(x.Id) && x.Id != chosenColorId);

                foreach (DiscordRole oldColor in oldColors)
                {
                    try
                    {
                        await member.RevokeRoleAsync(oldColor);
                    }
                    catch(Exception ex) 
                    {
                        await logService.GrabarLogGeneralError(args.Guild, $"No se pudo quitar el color antiguo {oldColor.Mention} a {member.Mention}: {ex.Message} {Formatter.BlockCode(ex.StackTrace ?? string.Empty)}");
                    }
                }

                try
                {
                    await member.GrantRoleAsync(newColor);
                    ok = true;
                }
                catch (Exception ex)
                {
                    await logService.GrabarLogGeneralError(args.Guild, $"No se pudo asignar el color {newColor.Mention} a {member.Mention}: {ex.Message} {Formatter.BlockCode(ex.StackTrace ?? string.Empty)}");
                }

                if (ok)
                {
                    await args.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder()
                        .WithTitle("Color Modificado")
                        .WithDescription($"Color Asignado correctamente")
                        .WithColor(DiscordColor.Green)));
                }
                else
                {
                    await args.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder()
                        .WithTitle("Color No modificado")
                        .WithDescription($"El color no se pudo asignar por un error interno")
                        .WithColor(DiscordColor.Red)));
                } 
            }
            else
            {
                await args.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().AddEmbed(ErrorEmbed.De($"No tienes el rango necesario para elegir este color. Para poder utilizarlo debes subir a {Enum.GetName(chosenColor.Value)}")));
            }
        }
        else if (args.Interaction.Data.CustomId.StartsWith("sync-"))
        {
            if (!await EsStaffAsync(args)) return;

            string[] parts = args.Id.Split('-');
            if (parts.Length < 3 || !bool.TryParse(parts[1], out bool aprobado) || !long.TryParse(parts[2], out long discordId)) return;

            await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);

            UserApprovalAnilist? userApproval = await anilistApprovalRepository.Obtener(discordId);
            if (userApproval is null)
            {
                await args.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().AsEphemeral().AddEmbed(ErrorEmbed.De("No se encontró la solicitud de vinculación o la misma ya fue revisada")));
            }
            else
            {
                DiscordMember? member = args.Guild.Members.Values.FirstOrDefault(x => x.Id == (ulong)discordId);
                if (member is null)
                {
                    await args.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().AsEphemeral().AddEmbed(ErrorEmbed.De("No se encontró al usuario en el servidor")));
                    return;
                }

                DiscordEmbed original = args.Message.Embeds[0];
                DiscordEmbedBuilder resuelto = new DiscordEmbedBuilder(original);

                if (aprobado)
                {
                    DiscordUser user = await client.GetUserAsync((ulong)discordId);
                    AnilistUser anilistUser = new()
                    {
                        Id = userApproval.IdAnilist,
                        Name = userApproval.Name,
                        SiteUrl = userApproval.SiteUrl,
                        AvatarMedium = userApproval.Avatar,
                        BannerImage = userApproval.Banner
                    };
                    await anilistService.TerminarVinculacion(client, user, member, args.Guild, anilistUser);

                    resuelto
                        .WithColor(DiscordColor.Green)
                        .WithTitle("Vinculacion de AniList aprobada")
                        .WithDescription($"{original.Description}\n\n{Formatter.Bold("Aprobada")} por {args.User.Mention}, el usuario fue vinculado correctamente.");
                }
                else
                {
                    await args.Guild.RemoveMemberAsync((ulong)discordId, "Solicitud de vinculación rechazada por el staff");

                    resuelto
                        .WithColor(DiscordColor.Red)
                        .WithTitle("Vinculacion de AniList rechazada")
                        .WithDescription($"{original.Description}\n\n{Formatter.Bold("Rechazada")} por {args.User.Mention}, el usuario fue expulsado del servidor.");
                }

                await anilistApprovalRepository.Delete(discordId);
                try
                {
                    await args.Message.ModifyAsync(new DiscordMessageBuilder().AddEmbed(resuelto.Build()));
                }
                catch (Exception ex) { await logService.LogException(args.Guild, ex, "Resolución de vinculación - editar mensaje"); }
            }

        }
        else if (args.Interaction.Data.CustomId.StartsWith("yoink-"))
        {
            if (!await EsStaffAsync(args)) return;

            string[] partes = args.Id.Split('-');
            if (partes.Length < 4 || !ulong.TryParse(partes[1], out ulong emoteId)) return;
            bool animado = partes[2] == "1";
            string nombreEmote = partes[3];

            await args.Interaction.DeferAsync(true);

            DiscordMessageBuilder edicion = new DiscordMessageBuilder().EnableV2Components();
            foreach (DiscordComponent componente in args.Message!.Components!)
            {
                if (componente is DiscordContainerComponent container)
                {
                    edicion.AddContainerComponent(container);
                }
                else if (componente is DiscordActionRowComponent fila)
                {
                    edicion.AddActionRowComponent(fila.Components.OfType<DiscordButtonComponent>().Select(boton =>
                        boton.CustomId == args.Id
                            ? new DiscordButtonComponent(boton.Style, boton.CustomId, boton.Label, true, boton.Emoji)
                            : boton));
                }
            }
            await args.Message.ModifyAsync(edicion);

            try
            {
                string extension = animado ? "gif" : "png";
                HttpClient http = httpClientFactory.CreateClient();
                http.MaxResponseContentBufferSize = 8 * 1024 * 1024;
                byte[] bytes = await http.GetByteArrayAsync($"https://cdn.discordapp.com/emojis/{emoteId}.{extension}");
                await using MemoryStream imagen = new(bytes);
                DiscordGuildEmoji nuevo = await args.Guild.CreateEmojiAsync(nombreEmote, imagen);

                await args.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().AsEphemeral().AddEmbed(new DiscordEmbedBuilder
                {
                    Title = "Emote robado",
                    Description = $"Se agregó <{(nuevo.IsAnimated ? "a" : "")}:{nuevo.Name}:{nuevo.Id}> correctamente.",
                    Color = DiscordColor.Green
                }));
            }
            catch (Exception ex)
            {
                await logService.LogException(args.Guild, ex, "Yoink - subir emote");
                await args.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().AsEphemeral().AddEmbed(new DiscordEmbedBuilder
                {
                    Title = "Error al yoinkear",
                    Description = "No se pudo robar el emote.",
                    Color = DiscordColor.Red
                }));
            }
        }
        else if (!args.Interaction.Data.CustomId.StartsWith("modal-"))
        {
            if (args.Interaction.Data.ComponentType is not DiscordComponentType.Button)
                await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);
        }
    }

    private async Task<bool> EsStaffAsync(ComponentInteractionCreatedEventArgs args)
    {
        if (args.Guild.Members.TryGetValue(args.User.Id, out DiscordMember? clicker) && clicker.Roles.Any(r => r.Id == config.Roles.KamiSama))
            return true;

        await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder().AsEphemeral().WithContent("Solo los administradores pueden usar este comando."));
        return false;
    }
}
