using AnilistConEnie.Application.Helpers;
using AnilistConEnie.Bot.Configuration;
using AnilistConEnie.Model.Entities;
using AnilistConEnie.Model.Enum;
using AnilistConEnie.Model.Interfaces.Repositories;
using DSharpPlus.Entities;
using System.Collections.Concurrent;

namespace AnilistConEnie.Bot.Services;

public class BotStateService(
    IUsuariosActivosRepository usuariosActivosRepository,
    IUsuariosDiscordRepository usuariosDiscordRepository,
    IUsuariosAnilistRepository usuariosAnilistRepository,
    BotConfiguration config)
{
    private List<UsuarioAnilist> _usuarios = [];
    private List<ulong> _boosters = [];
    private List<int> _anilistBaneados = [];
    private (bool, ulong) _debugXp = (false, 0);

    private readonly ConcurrentDictionary<long, bool> _dailyActiveUsers = new();
    private readonly ConcurrentDictionary<ulong, bool> _usersXp = new();
    private readonly ConcurrentDictionary<ulong, bool> _boluditos = new();
    private readonly ConcurrentDictionary<ulong, List<string>> _highlightedWords = new();
    private readonly ConcurrentDictionary<string, Trigger> _triggers = new();
    private readonly ConcurrentDictionary<ulong, DateTime> _linkRoleUsers = new();
    private readonly ConcurrentDictionary<ulong, List<BasicMessage>> _lastMessagesUsers = new();
    private readonly ConcurrentDictionary<ulong, DateTime> _teiouNicknameCooldown = new();
    private readonly ConcurrentDictionary<ulong, UserXp> _generalXp = new();
    private readonly ConcurrentDictionary<ulong, List<UserDailyXp>> _dailyXp = new();
    private readonly ConcurrentDictionary<ulong, string> _permanentUsernames = new();

    // Las confesiones se resetean juntas como unidad y tienen operaciones multi-dict; sin acceso concurrente real
    private Dictionary<ulong, ulong> _dailyConfessionUsers = new();
    private Dictionary<ulong, ulong> _dailyConfessedMessages = new();
    private Dictionary<ulong, List<ulong>> _confessions = new();

    #region Emote mode
    public bool YepMode { get; private set; }

    public DiscordEmoji? Emote { get; private set; }

    public void ActivarYepMode(DiscordEmoji emojiNuevo)
    {
        YepMode = true;
        Emote = emojiNuevo;
    }

    public void DesactivarYepMode()
    {
        if (!YepMode) return;
        YepMode = false;
        Emote = null;
    }
    #endregion

    #region Cache de usuarios AniList
    public List<UsuarioAnilist> Usuarios => _usuarios;

    public void SetUsuarios(List<UsuarioAnilist> newUsers) => _usuarios = newUsers;
    #endregion

    #region Highlights
    public void SetHighlightedWords(Dictionary<ulong, List<string>> highlights)
    {
        _highlightedWords.Clear();
        foreach (var (key, value) in highlights)
            _highlightedWords[key] = value;
    }

    public IReadOnlyDictionary<ulong, List<string>> GetHighlightedWords() => _highlightedWords;

    public void AddHighlightedWord(ulong userId, string word)
    {
        _highlightedWords.AddOrUpdate(userId, _ => [word], (_, words) =>
        {
            words.Add(word);
            return words;
        });
    }

    public void RemoveHighlightedWord(ulong userId, string word)
    {
        if (_highlightedWords.TryGetValue(userId, out var words))
            words.Remove(word);
    }
    #endregion

    #region Triggers
    public IReadOnlyDictionary<string, Trigger> GetActiveTriggers() => _triggers;

    public void FillTriggers(List<Trigger> triggers)
    {
        _triggers.Clear();
        foreach (Trigger trigger in triggers)
            _triggers[trigger.Nombre] = trigger;
    }

    public void SetTrigger(Trigger trigger) => _triggers[trigger.Nombre] = trigger;

    public void RemoveTriggerFromActiveList(string triggerName) => _triggers.TryRemove(triggerName, out _);
    #endregion

    #region Control usuarios activos/inactivos
    public async Task AddDailyActiveUser(ulong id, DiscordGuild guild)
    {
        if (!_dailyActiveUsers.TryAdd((long)id, true)) return;

        DiscordRole inactivoRole = guild.Roles[config.Roles.Inactivo];
        await usuariosActivosRepository.SetUsuarioActividad((long)id);

        if (guild.Members.TryGetValue(id, out DiscordMember? member) && member.Roles.Any(x => x.Id == inactivoRole.Id))
            await member.RevokeRoleAsync(inactivoRole);
    }
    #endregion

    #region Permiso de mandar invites a otros discords
    public IReadOnlyDictionary<ulong, DateTime> GetLinkRoleUsers() => _linkRoleUsers;

    public void AddLinkRoleUser(ulong id, DateTime expiration) => _linkRoleUsers.TryAdd(id, expiration);

    public void RemoveLinkRoleUser(ulong id) => _linkRoleUsers.TryRemove(id, out _);
    #endregion

    #region Control cuentas hackeadas
    public bool IsHackedAccount(ulong userId)
    {
        if (!_lastMessagesUsers.TryGetValue(userId, out var userMessages)) return false;

        if (userMessages.Select(x => x.Content).Distinct().Count() == 1 &&
            userMessages.Select(x => x.ChannelId).Distinct().Count() == 3)
        {
            DateTime messageDate = userMessages.First().CreatedAt;
            foreach (var message in userMessages.Skip(1))
            {
                if (message.CreatedAt.Subtract(messageDate).TotalMinutes >= 1) return false;
            }
            return true;
        }

        return false;
    }

    public void AddRecentUserMessage(ulong userId, ulong channelId, string content)
    {
        _lastMessagesUsers.AddOrUpdate(
            userId,
            _ => [new(content, channelId, DateTime.Now)],
            (_, existing) =>
            {
                var source = existing.Count == 3 ? existing.Skip(1) : existing.AsEnumerable();
                return [..source, new(content, channelId, DateTime.Now)];
            });
    }
    #endregion

    #region Teiou cooldown
    public bool TeiouInCooldown(ulong id) =>
        _teiouNicknameCooldown.TryGetValue(id, out var cd) && cd > DateTime.Now;

    public double GetHoursCooldownTeiou(ulong id) =>
        (DateTime.Now - _teiouNicknameCooldown[id]).TotalHours;

    public void AddTeiouCooldown(ulong id) =>
        _teiouNicknameCooldown[id] = DateTime.Now.AddHours(24);

    public void FillTeiouFromDb(List<TeiouCooldownNickname> list)
    {
        _teiouNicknameCooldown.Clear();
        foreach (var t in list)
        {
            var dt = DateTime.SpecifyKind(t.Cooldown, DateTimeKind.Utc).ToLocalTime();
            _teiouNicknameCooldown[(ulong)t.UserId] = dt;
        }
    }
    #endregion

    #region Boosters extra xp
    public void FillBoosters(List<ulong> users) => _boosters = users;
    public List<ulong> GetBoosters() => _boosters;
    #endregion

    #region Xp por minuto
    public void AddMemberToObtainXp(ulong userId) => _usersXp.TryAdd(userId, true);
    public void ResetMembersToObtainXp() => _usersXp.Clear();
    public List<ulong> GetMembersToObtainXp() => [.._usersXp.Keys];
    #endregion

    #region Cache de xp del servidor
    public void FillGuildXp(Dictionary<ulong, UserXp> users)
    {
        _generalXp.Clear();
        foreach (var (key, value) in users)
            _generalXp[key] = value;
    }

    public void UpdateUserXp(ulong userId, long xp, TipoXp tipo)
    {
        if (!_generalXp.TryGetValue(userId, out var value))
        {
            _generalXp[userId] = new UserXp { Total = xp, UserId = (long)userId };
            return;
        }

        switch (tipo)
        {
            case TipoXp.Total: value.Total = xp; break;
            case TipoXp.Booster: value.Booster = xp; break;
            case TipoXp.Challenges: value.Challenges = xp; break;
            case TipoXp.Eventos: value.Eventos = xp; break;
            case TipoXp.Intercambios: value.Intercambios = xp; break;
            default: value.Otros = xp; break;
        }

        _generalXp[userId] = value;
    }

    public List<UserXp> GetGuildXp(DiscordGuild guild) =>
        _generalXp.Where(x => guild.Members.ContainsKey(x.Key)).Select(x => x.Value).ToList();

    public UserXp GetUserXp(ulong userId) =>
        _generalXp.TryGetValue(userId, out var xp) ? xp : new UserXp();
    #endregion

    #region Daily XP Chart
    public async Task AddUserXpToChartHistory(ulong userId, long xpFromDay, DateTime date)
    {
        if (_dailyXp.TryGetValue(userId, out var xp))
        {
            xp.Add(new UserDailyXp { Date = date, UserId = (long)userId, Xp = xpFromDay });
        }
        else
        {
            xp = await usuariosDiscordRepository.GetDailyXpChartFromUser(userId, DateRangeXp.Anual);
            _dailyXp[userId] = xp;
        }
    }

    public async Task<List<UserDailyXp>> GetUserChartHistory(
        ulong userId,
        bool includeZeroXp = false,
        bool rellenarDiasFaltantes = true)
    {
        if (!_dailyXp.TryGetValue(userId, out var listTmp))
        {
            listTmp = await usuariosDiscordRepository.GetDailyXpChartFromUser(userId, DateRangeXp.Anual);
            _dailyXp[userId] = listTmp;
        }

        int yearActual = DateTime.Now.Year;
        var registrosAnteriores = listTmp.Where(x => x.Date.Year != yearActual).OrderBy(x => x.Date).ToList();
        var registrosActual = listTmp.Where(x => x.Date.Year == yearActual).OrderBy(x => x.Date).ToList();

        List<UserDailyXp> resultado = [..registrosAnteriores];

        if (!rellenarDiasFaltantes)
        {
            resultado.AddRange(registrosActual);

            DateTime hoy = DateTime.Today;
            if (!registrosActual.Any(x => x.Date.Date == hoy))
            {
                long lastXp = registrosActual.LastOrDefault()?.Xp ?? registrosAnteriores.LastOrDefault()?.Xp ?? 0;
                resultado.Add(new UserDailyXp { Date = hoy, UserId = (long)userId, Xp = lastXp });
            }

            return resultado.OrderBy(x => x.Date).ToList();
        }

        if (registrosActual.Count > 0)
        {
            DateTime startDate = new(yearActual, 1, 1);
            DateTime endDate = DateTime.Today;
            long lastXp = 0;
            int idx = 0;
            bool modified = false;

            while (startDate <= endDate)
            {
                if (idx < registrosActual.Count && registrosActual[idx].Date.Date == startDate)
                {
                    resultado.Add(registrosActual[idx]);
                    lastXp = registrosActual[idx].Xp;
                    idx++;
                }
                else if (includeZeroXp || lastXp != 0)
                {
                    resultado.Add(new UserDailyXp { Date = startDate, UserId = (long)userId, Xp = lastXp });

                    if (lastXp != 0)
                    {
                        _ = usuariosDiscordRepository.AddDailyXp(startDate, userId, lastXp);
                        modified = true;
                    }
                }
                startDate = startDate.AddDays(1);
            }

            if (modified)
                _dailyXp[userId] = resultado;
        }

        return resultado.OrderBy(x => x.Date).ToList();
    }

    public void ResetXpChartHistory() => _dailyXp.Clear();
    #endregion

    #region Boluditos
    public bool IsBoludito(ulong userId) => _boluditos.ContainsKey(userId);
    public void AddBoludito(ulong userId) => _boluditos.TryAdd(userId, true);
    public void ResetBoluditos() => _boluditos.Clear();
    #endregion

    #region Confesiones usuarios
    public void ResetDailyConfessions()
    {
        _dailyConfessionUsers.Clear();
        _confessions.Clear();
        _dailyConfessedMessages.Clear();
    }

    public void AddDailyConfessionUser(ulong userId, ulong messageId)
    {
        if (_dailyConfessionUsers.TryAdd(userId, messageId))
            _confessions.Add(userId, []);
    }

    public bool UserConfessed(ulong userId) => _dailyConfessionUsers.ContainsKey(userId);

    public bool MessageConfessionGuessed(ulong messageId) => _dailyConfessedMessages.ContainsKey(messageId);

    public bool IsConfession(ulong messageId) => _dailyConfessionUsers.ContainsValue(messageId);

    public (bool, ulong, ulong) AddConfessionReaction(ulong messageId, ulong userReactedId)
    {
        var confessionUser = _dailyConfessionUsers.First(x => x.Value == messageId);
        var confessionReactions = _confessions[confessionUser.Key];

        if (confessionReactions.Contains(userReactedId) || userReactedId == confessionUser.Key) return (false, 0, 0);

        confessionReactions.Add(userReactedId);
        _confessions[confessionUser.Key] = confessionReactions;

        int revealPercentage = confessionReactions.Count * 5;
        if (NumberHelper.GetNumeroRandom(0, 100) > revealPercentage) return (false, 0, 0);

        _dailyConfessedMessages.Add(confessionUser.Value, confessionUser.Key);
        return (true, confessionUser.Value, confessionUser.Key);
    }
    #endregion

    #region Anilist baneados
    public void FillAnilistBaneados(List<UsuarioAnilistBaneado> baneados)
    {
        _anilistBaneados = [..baneados.Select(x => x.AnilistUserId)];
    }

    public List<int> GetAnilistUsersBaneados() => _anilistBaneados;

    public async Task AddAnilistUserBaneado(int userId)
    {
        if (_anilistBaneados.Contains(userId)) return;
        _anilistBaneados.Add(userId);
        await usuariosAnilistRepository.AgregarUsuarioBaneado(userId);
    }

    public async Task RemoveAnilistUserBaneado(int userId)
    {
        if (!_anilistBaneados.Contains(userId)) return;
        _anilistBaneados.Remove(userId);
        await usuariosAnilistRepository.DeleteUsuarioBaneado(userId);
    }

    public bool IsAnilistUserBaneado(int userId) => _anilistBaneados.Contains(userId);
    #endregion

    #region Debug XP
    public (bool, ulong) GetDebugXp() => _debugXp;
    public void EnableDebugXp(ulong id) => _debugXp = (true, id);
    public void DisableDebugXp() => _debugXp = (false, 0);
    #endregion

    #region Permanent Usernames
    public void SetPermanentUsername(ulong userId, string username) => _permanentUsernames[userId] = username;

    public IReadOnlyDictionary<ulong, string> GetPermanentUsernames() => _permanentUsernames;

    public void RemovePermanentUsername(ulong userId) => _permanentUsernames.TryRemove(userId, out _);
    #endregion
}