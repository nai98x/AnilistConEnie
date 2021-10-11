using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AnilistESP
{
    public class Administrativo : ApplicationCommandModule
    {
        private readonly FuncionesAuxiliares _funciones = new();

        [SlashCommand("yepmodetoggle", "Activa el Yep mode (Staff)")]
        [SlashRequireUserPermissions(Permissions.ManageGuild)]
        public async Task ToggleYepMode(InteractionContext ctx, [Option("Emote", "El emote que quieres utilizar")] string emojiStr)
        {
            if (ctx.Guild.Id == 862408834693070898)
            {
                DiscordEmoji emote = _funciones.ToEmoji(emojiStr);
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
        [SlashRequireUserPermissions(Permissions.ManageGuild)]
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
    }
}
