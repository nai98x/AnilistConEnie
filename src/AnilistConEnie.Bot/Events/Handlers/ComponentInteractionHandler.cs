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

public class ComponentInteractionHandler(DiscordBotService discordBotService, BotConfiguration config, RangoRoles rangoRoles, DiscordLogService logService, AnilistService anilistService, IUsuariosAnilistRepository usuariosAnilistRepository)
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
            DiscordMember member = args.Guild.Members[args.User.Id];
            ulong chosenColorId = ulong.Parse(args.Interaction.Data.Values.First());
            Dictionary<ulong, RangoEnum> coloresPorRango = config.Roles.ColoresRango.ToDictionary(c => c.RoleId, c => Enum.Parse<RangoEnum>(c.Rango));
            KeyValuePair<ulong, RangoEnum> chosenColor = coloresPorRango.FirstOrDefault(x => x.Key == chosenColorId);
            DiscordRole newColor = args.Guild.Roles[chosenColorId];

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
                await args.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder()
                    .WithTitle("Error")
                    .WithDescription($"No tienes el rango necesario para elegir este color. Para poder utilizarlo debes subir a {Enum.GetName(chosenColor.Value)}")
                    .WithColor(DiscordColor.Red)));
            }
        }
        else if (args.Interaction.Data.CustomId.StartsWith("sync-"))
        {
            await args.Interaction.DeferAsync(false);

            string[] parts = args.Id.Split('-');
            bool aprobado = bool.Parse(parts[1]);
            long discordId = long.Parse(parts[2]);
            
            UserApprovalAnilist? userApproval = await usuariosAnilistRepository.GetUsuarioApproval(discordId);
            if (userApproval is null)
            {
                await args.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder()
                    .WithTitle("Error")
                    .WithDescription("No se encontró la solicitud de vinculación o la misma ya fue revisada")
                    .WithColor(DiscordColor.Red)
                ));
            }
            else
            {
                DiscordMember? member = args.Guild.Members.Values.FirstOrDefault(x => x.Id == (ulong)discordId);
                if (member is null)
                {
                    await args.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder()
                        .WithTitle("Error")
                        .WithDescription("No se encontró al usuario en el servidor")
                        .WithColor(DiscordColor.Red)
                    ));
                    return;
                }

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

                    await args.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder()
                        .WithTitle("Solicitud Aprobada")
                        .WithDescription($"El usuario {member.Mention} | Nombre: {Formatter.InlineCode(member.DisplayName)} | Id: {Formatter.InlineCode(member.Id.ToString())} ({Formatter.MaskedUrl("Link de su AniList", new Uri(userApproval.SiteUrl))}) ha sido vinculado correctamente por {args.User.Mention}")
                        .WithColor(DiscordColor.Green)
                    ));
                }
                else
                {
                    await args.Guild.RemoveMemberAsync((ulong)discordId, "Solicitud de vinculación rechazada por el staff");
                    await args.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder()
                        .WithTitle("Solicitud Rechazada")
                        .WithDescription($"El usuario {member.Mention} | Nombre: {Formatter.InlineCode(member.DisplayName)} | Id: {Formatter.InlineCode(member.Id.ToString())} ({Formatter.MaskedUrl("Link de su AniList", new Uri(userApproval.SiteUrl))}) ha sido expulsado del servidor por {args.User.Mention}")
                        .WithColor(DiscordColor.Red)
                    ));
                }

                await usuariosAnilistRepository.EliminarUsuarioApproval(discordId);
                try
                {
                    await args.Message.DeleteAsync();
                }
                catch (Exception ex) { await logService.LogException(args.Guild, ex, "Rechazo de vinculación - borrar mensaje"); }
            }

        }
        else if (!args.Interaction.Data.CustomId.StartsWith("modal-"))
        {
            if (args.Interaction.Data.ComponentType is not DiscordComponentType.Button)
                await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);
        }
    }
}
