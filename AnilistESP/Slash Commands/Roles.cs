using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using System;
using System.Threading.Tasks;

namespace AnilistESP
{
    public class Roles : ApplicationCommandModule
    {
        [SlashCommand("birthdayroleadd", "Le agrega a un usuario el rol de cumpleañero (Staff)")]
        [SlashRequireUserPermissions(Permissions.ManageRoles)]
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
                            IsEphemeral = false,
                        }.AddEmbed(new DiscordEmbedBuilder
                        {
                            Title = "Rol otorgado",
                            Description = $"Se le ha asignado el rol {rol.Mention} a {user.Mention}"
                        }));
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
        [SlashRequireUserPermissions(Permissions.ManageRoles)]
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
    }
}
