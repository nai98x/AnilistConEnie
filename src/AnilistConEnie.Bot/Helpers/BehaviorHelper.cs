using AnilistConEnie.Bot.Configuration;
using AnilistConEnie.Bot.Services;
using AnilistConEnie.Model.Interfaces.Repositories;
using AnilistConEnie.Model.Entities;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace AnilistConEnie.Bot.Helpers;

public class BehaviorHelper(ILogger<BehaviorHelper> logger, BotStateService botStateService, BotConfiguration config, IUsuariosActivosRepository usuariosActivosRepository, IUsuariosDiscordRepository usuariosDiscordRepository)
{
    public async Task ClearInvitesRoleOnStartup(DiscordGuild guild)
    {
        try
        {
            DiscordRole inviteRole = guild.Roles[config.Roles.Invite];
            IEnumerable<KeyValuePair<ulong, DiscordMember>> usersWithInvitesRole = guild.Members.Where(x => x.Value.Roles.Any(y => y.Id == inviteRole.Id));
            foreach (KeyValuePair<ulong, DiscordMember> userPair in usersWithInvitesRole)
            {
                await userPair.Value.RevokeRoleAsync(inviteRole);
            }
        }
        catch (Exception) { /* Ignored */ }
    }
    
    public async Task ManageBoosters(DiscordGuild guild)
    {
        List<ulong> boostersIds = [];
        List<DiscordMember> members = await guild.GetAllMembersAsync().ToListAsync();
        foreach (DiscordMember member in members)
        {
            if (member.PremiumSince != null)
                boostersIds.Add(member.Id);
        }
        
        botStateService.FillBoosters(boostersIds);
    }
    
    public async Task ManageNewUsuarios(DiscordGuild guild)
    {
        DiscordRole noVerificadoRole = guild.Roles[config.Roles.NoVinculado];
        DiscordRole miembroRole = guild.Roles[config.Roles.Miembro];

        List<DiscordMember> newMembers = guild.Members.Values.Where(x => !x.IsBot && !x.Roles.Contains(miembroRole) && !x.Roles.Contains(noVerificadoRole)).ToList();
        foreach (DiscordMember member in newMembers)
        {
            await member.GrantRoleAsync(noVerificadoRole);
        }
    }
    
    public async Task ManageUsuariosActivos(DiscordGuild guild)
    {
        DiscordRole inactivoRole = guild.Roles.First(x => x.Key == config.Roles.Inactivo).Value;

        // 1- Obtener inactivos (+90 dias, desde bd)
        List<UsuarioActivo> usuariosActivos = await usuariosActivosRepository.GetUsuariosActivos(guild.Members.Keys.ToHashSet());
        List<KeyValuePair<ulong, DiscordMember>> usuariosInactivos = guild.Members.Where(x => !usuariosActivos.Any(y => y.UserId == (long)x.Key) && !x.Value.IsBot).ToList();

        // 2- Si estos usuarios no tienen el rol inactivo, agregarselo
        foreach (KeyValuePair<ulong, DiscordMember> user in usuariosInactivos)
        {
            if (!user.Value.Roles.Any(x => x.Id == inactivoRole.Id))
            {
                await user.Value.GrantRoleAsync(inactivoRole);
            }
        }
    }
    
    public async Task ManageXpUserHistory(DiscordGuild guild)
    {
        try
        {
            List<UserXp> rankings = botStateService.GetGuildXp(guild);
            DateTime date = new DateTime(day: DateTime.Now.Day, month: DateTime.Now.Month, year: DateTime.Now.Year, hour: 5, minute: 0, second: 0, kind: DateTimeKind.Utc);
            foreach (UserXp user in rankings)
            {
                await usuariosDiscordRepository.AddDailyXp(date, (ulong)user.UserId, user.Total);
                await botStateService.AddUserXpToChartHistory((ulong)user.UserId, user.Total, date);
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error leyendo el history de experiencia");
        }
    }
    
    public async Task ManageUnlinkedAccounts(DiscordClient client)
    {
        DiscordGuild guild = client.Guilds[config.GuildId];
        DiscordRole noVerificadoRole = guild.Roles[config.Roles.NoVinculado];
        DiscordRole miembroRole = guild.Roles[config.Roles.Miembro];
        List<KeyValuePair<ulong, DiscordMember>> miembrosNoVerificados = guild.Members.Where(x => x.Value.Roles.Contains(noVerificadoRole)).ToList();

        foreach (KeyValuePair<ulong, DiscordMember> member in miembrosNoVerificados)
        {
            try
            {
                if (member.Value.JoinedAt.AddDays(1) >= System.DateTime.Now) continue;
                
                DiscordMember memberNew = await guild.GetMemberAsync(member.Key, true);
                if (memberNew.Roles.Contains(noVerificadoRole) && !memberNew.Roles.Contains(miembroRole))
                    await member.Value.RemoveAsync("Kick automatico - 24hs sin vincular");
            }
            catch (Exception) { /* Ignored */ }
        }
    }
    
    public async Task ManageFundadores(DiscordGuild guild)
    {
        try
        {
            DateTime guildCreation = guild.CreationTimestamp.Date;
            DiscordRole fundadorRole = guild.Roles[config.Roles.Fundador];
            DiscordRole noVerificadoRole = guild.Roles[config.Roles.NoVinculado];

            List<KeyValuePair<ulong, DiscordMember>> miembrosNoFundadores = guild.Members.Where(x => x.Value.JoinedAt.Date == guildCreation && !x.Value.Roles.Contains(fundadorRole) && !x.Value.Roles.Contains(noVerificadoRole) && !x.Value.IsBot).ToList();

            foreach (KeyValuePair<ulong, DiscordMember> member in miembrosNoFundadores)
            {
                try
                {
                    await member.Value.GrantRoleAsync(fundadorRole);
                }
                catch (Exception) { /* Ignored */ }
            }
        }
        catch (Exception) { /* Ignored */ }
    }
}