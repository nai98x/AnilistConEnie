using System.Diagnostics;
using AnilistConEnie.Bot.Configuration;
using AnilistConEnie.Bot.Services;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.DependencyInjection;

namespace AnilistConEnie.Bot.Events.Handlers;

public class GuildMemberAddedHandler(IServiceProvider services, BotConfiguration config)
{
    public async Task Handle(DiscordClient client, GuildMemberAddedEventArgs args)
    {
        DiscordBotService discordBotService = services.GetRequiredService<DiscordBotService>();
        if (args.Guild.Id != config.GuildId || discordBotService.Debug) return;

        DiscordRole noVerificadoRole = args.Guild.Roles[config.Roles.NoVinculado];
        await args.Member.GrantRoleAsync(noVerificadoRole);
    }
}
