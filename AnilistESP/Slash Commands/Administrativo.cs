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
        [SlashCommand("yepmode", "Activa o desactiva el Yep mode (Staff)")]
        [SlashRequireUserPermissions(Permissions.ManageGuild)]
        public async Task ToggleYepMode(InteractionContext ctx)
        {
            if (ctx.Guild.Id == 862408834693070898)
            {
                ServiciosSingleton service = ServiciosSingleton.GetServiciosSingleton();
                service.CambiarYepMode();
                if (service.YepMode)
                {
                    bool ok = DiscordEmoji.TryFromName(ctx.Client, ":yep:", out DiscordEmoji yep);
                    var builder = new DiscordInteractionResponseBuilder();
                    var embed = new DiscordEmbedBuilder
                    {
                        Title = "Yepmode activado",
                        Description = "yep"
                    };
                    if (ok)
                    {
                        embed.WithImageUrl($"{yep.Url}");
                    }
                    builder.AddEmbed(embed);
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, builder);
                }
                else
                {
                    bool ok = DiscordEmoji.TryFromName(ctx.Client, ":notyep:", out DiscordEmoji notyep);
                    var builder = new DiscordInteractionResponseBuilder();
                    var embed = new DiscordEmbedBuilder
                    {
                        Title = "Yepmode desactivado",
                        Description = "notyep"
                    };
                    if (ok)
                    {
                        embed.WithImageUrl($"{notyep.Url}");
                    }
                    builder.AddEmbed(embed);
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, builder);
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
