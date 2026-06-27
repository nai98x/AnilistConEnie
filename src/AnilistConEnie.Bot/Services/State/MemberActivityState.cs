using AnilistConEnie.Bot.Configuration;
using AnilistConEnie.Model.Interfaces.Repositories;
using DSharpPlus.Entities;
using System.Collections.Concurrent;

namespace AnilistConEnie.Bot.Services.State;

public class MemberActivityState(IUsuariosRepository usuariosRepository, BotConfiguration config)
{
    private readonly ConcurrentDictionary<long, bool> _dailyActiveUsers = new();

    public async Task AddDailyActiveUser(ulong id, DiscordGuild guild)
    {
        if (!_dailyActiveUsers.TryAdd((long)id, true)) return;

        DiscordRole inactivoRole = guild.Roles[config.Roles.Inactivo];
        await usuariosRepository.SetLastActivity(id, FechaActividad());

        if (guild.Members.TryGetValue(id, out DiscordMember? member) && member.Roles.Any(x => x.Id == inactivoRole.Id))
            await member.RevokeRoleAsync(inactivoRole);
    }

    private static DateTime FechaActividad() =>
        new(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 5, 0, 0, DateTimeKind.Utc);
}
