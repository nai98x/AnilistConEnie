using AnilistConEnie.Bot.Configuration;
using AnilistConEnie.Bot.Services;
using AnilistConEnie.Bot.Services.State;
using AnilistConEnie.Model.Enum;
using AnilistConEnie.Model.Interfaces.Repositories;
using AnilistConEnie.Model.Entities;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace AnilistConEnie.Bot.Helpers;

public class BehaviorHelper(ILogger<BehaviorHelper> logger, XpState xpState, InviteLinkState inviteLinkState, PermanentUsernameState permanentUsernameState, BotConfiguration config, DiscordHelper discordHelper, DiscordBotService discordBotService, IUsuariosActivosRepository usuariosActivosRepository, IUsuariosDiscordRepository usuariosDiscordRepository, IChallengesRepository challengesRepository)
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

    public async Task ManageInvitesRole(DiscordGuild guild)
    {
        try
        {
            IReadOnlyDictionary<ulong, DateTime> linkRoleUsersCache = inviteLinkState.GetLinkRoleUsers();
            DiscordRole role = guild.Roles[config.Roles.Invite];

            IEnumerable<KeyValuePair<ulong, DiscordMember>> usersWithInvitesRole = guild.Members.Where(x => x.Value.Roles.Any(y => y.Id == role.Id));
            foreach (KeyValuePair<ulong, DiscordMember> userPair in usersWithInvitesRole)
            {
                if (linkRoleUsersCache.TryGetValue(userPair.Key, out DateTime expiration) && DateTime.Now > expiration)
                {
                    await userPair.Value.RevokeRoleAsync(role);
                    inviteLinkState.RemoveLinkRoleUser(userPair.Key);
                }
            }
        }
        catch (Exception) { /* Ignored */ }
    }

    public async Task ManagePermanentUsernames(DiscordGuild guild)
    {
        IReadOnlyDictionary<ulong, string> usernames = permanentUsernameState.GetPermanentUsernames();
        foreach (KeyValuePair<ulong, string> username in usernames)
        {
            try
            {
                DiscordMember member = await guild.GetMemberAsync(username.Key);
                if (member.DisplayName != username.Value)
                {
                    await member.ModifyAsync(x => x.Nickname = username.Value);
                }
            }
            catch (Exception)
            {
                // Ignored
            }
        }
    }

    public async Task ManageMemberXp(DiscordGuild guild)
    {
        const int min = 10;
        const int max = 20;
        const int minBoost = 1;
        const int maxBoost = 3;

        (bool habilitado, ulong usuarioId) debug = xpState.GetDebugXp();
        DiscordChannel playroom = guild.Channels[config.Channels.Playroom];

        try
        {
            Random rnd = new();

            List<ulong> membersToAddXp = xpState.GetMembersToObtainXp();

            foreach (ulong userId in membersToAddXp)
            {
                if (!guild.Members.TryGetValue(userId, out DiscordMember? member) || member.IsBot)
                    continue;

                int xp = rnd.Next(min, max + 1);
                UserXp memberXp = xpState.GetUserXp(userId);

                if (member.PremiumSince != null)
                {
                    int xpBoost = rnd.Next(minBoost, maxBoost + 1);

                    await usuariosDiscordRepository.AddOrRemoveUserXp(userId, 0, 0, xpBoost, 0, 0, 0, 0);
                    long boosterXp = memberXp.Booster += xpBoost;
                    xpState.UpdateUserXp(userId, boosterXp, TipoXp.Booster);

                    xp += xpBoost;
                }

                DiscordRole roleBefore = discordHelper.GetRoleByXpActual(guild, member);
                long totalXp = memberXp.Total += xp;
                DiscordRole roleAfter = discordHelper.GetRoleByXp(guild, totalXp);
                xpState.UpdateUserXp(userId, totalXp, TipoXp.Total);
                await usuariosDiscordRepository.AddOrRemoveUserXp(userId, 0, xp, 0, 0, 0, 0, 0);

                if (debug.habilitado && member.Id == debug.usuarioId)
                {
                    _ = playroom.SendMessageAsync($"Usuario: {member.DisplayName} | Cambio de XP: {xp} | XP Total: {totalXp} | Rango antes: {roleBefore.Mention} | Rango después: {roleAfter.Mention}");
                }

                if (roleBefore != roleAfter || member.Roles.All(x => x.Id != roleAfter.Id))
                {
                    await SubirRango(guild, member, roleBefore, roleAfter);
                }
            }
        }
        catch (Exception ex)
        {
            await discordHelper.GrabarLogGeneralError(guild, $"Error al dar XP\n{ex.Message}: {ex.StackTrace}");
        }

        xpState.ResetMembersToObtainXp();
    }

    private async Task SubirRango(DiscordGuild guild, DiscordMember member, DiscordRole oldRango, DiscordRole newRango)
    {
        DiscordChannel channel = guild.Channels[config.Channels.General];
        DiscordEmoji emote = guild.Emojis[config.Emotes.UmaPoints.Get(discordBotService.Debug)];
        bool yaTieneNuevoRango = false;

        try
        {
            yaTieneNuevoRango = member.Roles.Any(x => x.Id == newRango.Id);

            if (oldRango.Id != config.Roles.Miembro) await member.RevokeRoleAsync(oldRango); // Miembro no se quita nunca
            await member.GrantRoleAsync(newRango);
        }
        catch (Exception ex)
        {
            await discordHelper.GrabarLogGeneralError(guild, $"ERROR subiendo de rango {member.Mention} al rango {newRango.Mention}\n\n{ex.Message}:{Formatter.BlockCode(ex.StackTrace)}");
        }

        if (!yaTieneNuevoRango)
        {
            DiscordMessageBuilder builder = new();

            builder.WithContent(member.Mention);
            builder.AddEmbed(new DiscordEmbedBuilder()
                .WithTitle($"¡Felicitaciones {member.DisplayName}!")
                .WithDescription($"Tu spam en el server dio frutos y subiste al rango {newRango.Mention} {emote}")
                .WithImageUrl("https://media.discordapp.net/attachments/862410361595625522/864343455798919188/Omake_Gif_Anime.gif?ex=66d7d012&is=66d67e92&hm=02039c7a1566df59f6486bd013e38b8f1205e21b9c9b474f24727aad96a68d53&=")
                .WithThumbnail("https://media.discordapp.net/attachments/879612956848062514/879628082921762826/imagen_2021-07-15_150953_1.png"));
            builder.AddMention(new UserMention(member.Id));

            await channel.SendMessageAsync(builder);
        }
        else
        {
            await discordHelper.GrabarLogGeneralError(guild, $"El usuario {member.Mention} ya tenia el rango {newRango.Mention}\nSe omite mensaje de subir rango en general");
        }
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
        
        xpState.FillBoosters(boostersIds);
    }

    public async Task ManageBirthdayRole(DiscordGuild guild)
    {
        try
        {
            DiscordChannel canal = guild.Channels[config.Channels.General];
            DiscordRole role = guild.Roles[config.Roles.Cumple];

            List<UserCumple> birthdays = await usuariosDiscordRepository.GetBirthdaysAhora();

            foreach (UserCumple birthday in birthdays)
            {
                try
                {
                    DiscordMember userBday = guild.Members[(ulong)birthday.Id];
                    if (userBday.Roles.All(x => x.Id != role.Id) && discordHelper.RangoAPartirDe(guild, userBday, RangoEnum.Casual, true))
                    {
                        await userBday.GrantRoleAsync(role);

                        await canal.SendMessageAsync(
                            new DiscordEmbedBuilder()
                                .WithTitle($"¡Feliz cumpleaños {userBday.DisplayName}!")
                                .WithDescription($"Todos mandenle saluditos a {userBday.Mention}")
                                .WithImageUrl("https://media.discordapp.net/attachments/867856756901937202/1055623590235607070/3434c4b692a5176c13079980e94dd6df.gif")
                                .WithColor(DiscordColor.Blurple)
                                .WithThumbnail(userBday.AvatarUrl)
                        );
                    }
                }
                catch (Exception) { /* Ignored */ }
            }

            IEnumerable<KeyValuePair<ulong, DiscordMember>> usersWithBirthdayRole = guild.Members.Where(x => x.Value.Roles.Any(y => y.Id == role.Id));
            foreach (KeyValuePair<ulong, DiscordMember> userPair in usersWithBirthdayRole)
            {
                if (birthdays.All(x => (ulong)x.Id != userPair.Key))
                {
                    await userPair.Value.RevokeRoleAsync(role);
                }
            }
        }
        catch (Exception) { /* Ignored */ }
    }

    public async Task ManageAniversaries(DiscordGuild guild)
    {
        try
        {
            List<DateTime> aniversariesDates = [];
            int yearsGuildExists = DateTime.Now.Year - guild.CreationTimestamp.Year;

            for (int i = 1; i <= yearsGuildExists; i++)
            {
                aniversariesDates.Add(DateTime.Now.AddYears(-i).Date);
            }

            List<KeyValuePair<ulong, DiscordMember>> membersAniversaries = guild.Members
                .Where(x => aniversariesDates.Any(aniversaryDate =>
                    x.Value.JoinedAt.Date == aniversaryDate &&
                    x.Value.JoinedAt.Hour == DateTime.Now.Hour))
                .ToList();

            DiscordChannel canal = guild.Channels[config.Channels.General];

            foreach (KeyValuePair<ulong, DiscordMember> memberPair in membersAniversaries)
            {
                try
                {
                    DiscordMember member = memberPair.Value;

                    if (!discordHelper.RangoAPartirDe(guild, member, RangoEnum.Kouhai, true))
                        continue;

                    DateTimeOffset joinedAt = member.JoinedAt;
                    int yearsInServer = DateTime.Now.Year - joinedAt.Year;

                    if (joinedAt.Month > DateTime.Now.Month ||
                        (joinedAt.Month == DateTime.Now.Month && joinedAt.Day > DateTime.Now.Day))
                    {
                        yearsInServer--;
                    }

                    await canal.SendMessageAsync(new DiscordEmbedBuilder()
                        .WithTitle("¡Aniversario en el servidor!")
                        .WithDescription($"{member.Mention} cumple **{yearsInServer} año(s)** en el servidor hoy.\n\nEntró el {Formatter.Timestamp(joinedAt, TimestampFormat.LongDateTime)}")
                        .WithColor(DiscordColor.Gold)
                        .WithThumbnail(member.AvatarUrl)
                        .WithImageUrl("https://i.pinimg.com/originals/58/ee/7c/58ee7cf98c50002d1af055a407ae4d62.gif"));
                }
                catch (Exception ex)
                {
                    await discordHelper.GrabarLogGeneralError(guild, $"Error al procesar aniversario de {memberPair.Value.DisplayName}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            await discordHelper.GrabarLogGeneralError(guild, $"Error en ManageAniversaries: {ex.Message}\n\n{Formatter.BlockCode(ex.StackTrace)}");
        }
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
    
    public async Task ManageChallenges()
    {
        List<Challenge> challenges = await challengesRepository.GetLista();
        IEnumerable<Challenge> challengesObsoletos = challenges.Where(x => x.Vencimiento != null && x.Disponible && DateTime.Today.AddDays(1) > x.Vencimiento);

        foreach (Challenge challenge in challengesObsoletos)
        {
            await challengesRepository.Set(challenge.Nombre, challenge.Link, false, challenge.Vencimiento);
        }
    }

    public async Task ManageXpUserHistory(DiscordGuild guild)
    {
        try
        {
            List<UserXp> rankings = xpState.GetGuildXp(guild);
            DateTime date = new DateTime(day: DateTime.Now.Day, month: DateTime.Now.Month, year: DateTime.Now.Year, hour: 5, minute: 0, second: 0, kind: DateTimeKind.Utc);
            foreach (UserXp user in rankings)
            {
                await usuariosDiscordRepository.AddDailyXp(date, (ulong)user.UserId, user.Total);
                await xpState.AddUserXpToChartHistory((ulong)user.UserId, user.Total, date);
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