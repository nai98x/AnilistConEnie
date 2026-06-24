using System.Diagnostics;
using AnilistConEnie.Bot.Configuration;
using AnilistConEnie.Bot.Services;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace AnilistConEnie.Bot.Events.Handlers;

public class GuildMemberAddedHandler(DiscordBotService discordBotService, BotConfiguration config)
{
    public async Task Handle(DiscordClient client, GuildMemberAddedEventArgs args)
    {
        if (args.Guild.Id != config.GuildId || discordBotService.Debug) return;

        DiscordRole noVerificadoRole = args.Guild.Roles[config.Roles.NoVinculado];
        await args.Member.GrantRoleAsync(noVerificadoRole);
    }
}
