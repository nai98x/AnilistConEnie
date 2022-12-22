using AnilistESP;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnilistConEnie.Commands
{
    [SlashCommandPermissions(Permissions.ManageGuild)]
    public class Administrativo : ApplicationCommandModule
    {
        [SlashCommand("yepmodetoggle", "Activa el Yep mode (Staff)")]
        public async Task ToggleYepMode(InteractionContext ctx, [Option("Emote", "El emote que quieres utilizar")] string emojiStr)
        {
            if (ctx.Guild.Id == 862408834693070898)
            {
                DiscordEmoji emote = Funciones.ToEmoji(emojiStr);
                string name = emote.Name + "mode";
                if (emote != null)
                {
                    ServiciosSingleton service = ServiciosSingleton.GetServiciosSingleton();
                    if (!service.YepMode)
                    {
                        service.ActivarYepmpde(emote);
                        var builder = new DiscordInteractionResponseBuilder();
                        var embed = new DiscordEmbedBuilder
                        {
                            Title = $"{name} activado"
                        };
                        if (emote.Id != 0)
                        {
                            embed.WithImageUrl($"{emote.Url}");
                        }
                        builder.AddEmbed(embed);
                        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, builder);
                    }
                    else
                    {
                        service.ActivarYepmpde(emote);
                        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(new DiscordEmbedBuilder
                        {
                            Title = "Emote cambiado",
                            Description = $"El {name} ahora tiene asignado otro emote"
                        }));
                    }
                }
                else
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                    {
                        IsEphemeral = true,
                    }.AddEmbed(new DiscordEmbedBuilder
                    {
                        Title = "Error",
                        Description = "Debes pasar un emoji"
                    }));
                }
            }
            else
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                {
                    IsEphemeral = true,
                }.AddEmbed(new DiscordEmbedBuilder
                {
                    Title = "Error",
                    Description = "Comando no habilitado para este servidor"
                }));
            }
        }

        [SlashCommand("yepmodedisable", "Desactiva el Yep mode (Staff)")]
        public async Task DisableYepMode(InteractionContext ctx)
        {
            if (ctx.Guild.Id == 862408834693070898)
            {
                ServiciosSingleton service = ServiciosSingleton.GetServiciosSingleton();
                string name = service.Emote.Name + "mode";
                if (service.YepMode)
                {
                    service.DesactivarYepMode();
                    var builder = new DiscordInteractionResponseBuilder();
                    var embed = new DiscordEmbedBuilder
                    {
                        Title = $"{name} desactivado",
                    };
                    builder.AddEmbed(embed);
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, builder);
                }
                else
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(new DiscordEmbedBuilder
                    {
                        Title = "Error",
                        Description = $"El {name} no estaba activado"
                    }));
                }
            }
            else
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                {
                    IsEphemeral = true,
                }.AddEmbed(new DiscordEmbedBuilder
                {
                    Title = "Error",
                    Description = "Comando no habilitado para este servidor"
                }));
            }
        }

        [SlashCommand("birthdayroleadd", "Le agrega a un usuario el rol de cumpleañero (Staff)")]
        [SlashCommandPermissions(Permissions.ManageRoles)]
        [SlashRequireBotPermissions(Permissions.ManageRoles)]
        public async Task SetRolBirthday(InteractionContext ctx, [Option("Usuario", "Usuario del servidor al que quieres darle el rol")] DiscordUser user)
        {
            if (ctx.Guild.Id == 862408834693070898)
            {
                var context = Funciones.GetContext(ctx);
                DiscordMember miembro = (DiscordMember)user;
                ulong idRol = 869257331484004363;
                var rol = ctx.Guild.GetRole(idRol);
                if (rol != null)
                {
                    try
                    {
                        await miembro.GrantRoleAsync(rol);
                        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                        {
                            IsEphemeral = true,
                        }.AddEmbed(new DiscordEmbedBuilder
                        {
                            Title = "Rol otorgado",
                            Description = $"Se le ha asignado el rol {rol.Mention} a {user.Mention}"
                        }));

                        string desc = $"Todos mandenle saluditos a {miembro.Mention}";
                        DiscordChannel general = ctx.Guild.Channels[862408834693070901];
                        UsuariosDiscord usuariosService = new();
                        var usuarios = await usuariosService.GetBirthdaysHoy((long)ctx.Guild.Id);
                        var usr = usuarios.FirstOrDefault(usuarios => usuarios.Id == (long)miembro.Id);
                        if (usr != null)
                        {
                            desc += $" que cumple Cumple **{DateTime.Now.Year - usr.Birthday.Year} años";
                        }

                        await general.SendMessageAsync(
                            new DiscordEmbedBuilder()
                                .WithTitle($"¡Feliz cumpleaños {miembro.DisplayName}!")
                                .WithDescription(desc)
                                .WithImageUrl(@"https://media.discordapp.net/attachments/867856756901937202/1055623590235607070/3434c4b692a5176c13079980e94dd6df.gif")
                                .WithColor(DiscordColor.Blurple)
                                .WithThumbnail(miembro.AvatarUrl)
                        );
                    }
                    catch (Exception ex)
                    {
                        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                        {
                            IsEphemeral = true,
                        }.AddEmbed(new DiscordEmbedBuilder
                        {
                            Title = "Rol no otorgado",
                            Description = $"Error asignando rol `{rol.Name}`!"
                        }));
                        await Funciones.GrabarLogError(context, $"Error asignando rol `{rol.Name} (id: {rol.Id})`: {ex.Message}\n```{ex.StackTrace}```");
                    }
                }
            }
            else
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                {
                    IsEphemeral = true,
                }.AddEmbed(new DiscordEmbedBuilder
                {
                    Title = "Error",
                    Description = "Comando no habilitado para este servidor"
                }));
            }
        }

        [SlashCommand("birthdayroleremove", "Le quita a un usuario el rol de cumpleañero (Staff)")]
        [SlashCommandPermissions(Permissions.ManageRoles)]
        [SlashRequireBotPermissions(Permissions.ManageRoles)]
        public async Task RemoveRolBirthday(InteractionContext ctx, [Option("Usuario", "Usuario del servidor al que quieres quitarle el rol")] DiscordUser user)
        {
            if (ctx.Guild.Id == 862408834693070898)
            {
                var context = Funciones.GetContext(ctx);
                DiscordMember miembro = (DiscordMember)user;
                ulong idRol = 869257331484004363;
                var rol = ctx.Guild.GetRole(idRol);
                if (rol != null)
                {
                    try
                    {
                        await miembro.RevokeRoleAsync(rol);
                        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                        {
                            IsEphemeral = false,
                        }.AddEmbed(new DiscordEmbedBuilder
                        {
                            Title = "Rol quitado",
                            Description = $"Se le ha removido el rol {rol.Mention} a {user.Mention}"
                        }));
                    }
                    catch (Exception ex)
                    {
                        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                        {
                            IsEphemeral = true,
                        }.AddEmbed(new DiscordEmbedBuilder
                        {
                            Title = "Rol no quitado",
                            Description = $"Error removiendo rol `{rol.Name}`!"
                        }));
                        await Funciones.GrabarLogError(context, $"Error removiendo rol `{rol.Name} (id: {rol.Id})`: {ex.Message}\n```{ex.StackTrace}```");
                    }
                }
            }
            else
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                {
                    IsEphemeral = true,
                }.AddEmbed(new DiscordEmbedBuilder
                {
                    Title = "Error",
                    Description = "Comando no habilitado para este servidor"
                }));
            }
        }

        [SlashCommand("desvinculados", "Muestra los perfiles que no tienen cuenta de AniList vinculada")]
        public async Task Desvinculados(InteractionContext ctx)
        {
            await ctx.DeferAsync();

            if (ctx.Guild.Id == 862408834693070898)
            {
                UsuariosAnilist usuariosAnilist = new();

                var vinculadosFirebase = await usuariosAnilist.GetListaUsuarios();

                var vinculados = new List<DiscordMember>();
                ctx.Guild.Members.ToList().ForEach(member =>
                {
                    if (vinculadosFirebase.Any(x => (ulong) x.UserId == member.Key))
                    {
                        vinculados.Add(member.Value);
                    }
                });

                var usuarios = ctx.Guild.Members.Values.ToList();
                var noVinculados = usuarios.Except(vinculados).ToList();
                var botRole = ctx.Guild.Roles[862411811226910730];
                var miembroRole = ctx.Guild.Roles[862452184029069332];

                var noVinculadosNoBot = noVinculados.Where(x => !x.Roles.Contains(botRole)).ToList();

                var desc = "**Usuarios sin AniList vinculado:**\n" + string.Join("\n", noVinculadosNoBot.Select(member => $"{member.Username}#{member.Discriminator} (<@{member.Id}>)")) + $"\n\nTotal: {noVinculadosNoBot.Count}";
                var embed = new DiscordEmbedBuilder
                {
                    Footer = Funciones.GetFooter(ctx),
                    Color = DiscordColor.Red,
                    Title = "Usuarios sin cuenta vinculada de AniList"
                };
                var interactivity = ctx.Client.GetInteractivity();
                var pages = interactivity.GeneratePagesInEmbed(desc, DSharpPlus.Interactivity.Enums.SplitType.Line, embed);
                await interactivity.SendPaginatedResponseAsync(ctx.Interaction, false, ctx.User, pages, asEditResponse: true);
            }
        }
    }
}
