using AnilistConEnie.Application.Helpers;
using AnilistConEnie.Model.Entities;
using AnilistConEnie.Model.Enum;
using AnilistConEnie.Model.Interfaces.Repositories;
using DSharpPlus.Entities;

namespace AnilistConEnie.Bot.Services;

class SingletonService(
    IUsuariosActivosRepository usuariosActivosRepository,
    IUsuariosDiscordRepository usuariosDiscordRepository,
    IUsuariosAnilistRepository usuariosAnilistRepository)
{
    private bool _yepMode;
    private DiscordEmoji _emoji;
    private List<UsuarioAnilist> usuarios;
    private List<ulong> TempVoiceChannels;
    private Dictionary<ulong, List<string>> highlightedWords;
    private Dictionary<string, Trigger> _triggers = new();
    private List<long> _dailyActiveUsers = new();
    private Dictionary<ulong, ulong> _dailyConfessionUsers = new();
    private Dictionary<ulong, ulong> _dailyConfessedMessages = new();
    private Dictionary<ulong, List<ulong>> _confessions = new();
    private Dictionary<ulong, DateTime> _linkRoleUsers = new();
    private Dictionary<ulong, List<BasicMessage>> _lastMessagesUsers = new();
    private Dictionary<ulong, DateTime> _teiouNicknameCooldown = new();
    private List<ulong> _boosters = new();
    private List<ulong> _usersXp = new();
    private Dictionary<ulong, UserXp> _generalXp = new();
    private Dictionary<ulong, List<UserDailyXp>> _dailyXp = new();
    private List<ulong> _boluditos = new();
    private List<int> _anilistBaneados = new();
    private (bool, ulong) _debugXp = (false, 0);
    private Dictionary<ulong, string> _permanentUsernames = [];

    #region Emote mode
    public bool YepMode => _yepMode;

    public DiscordEmoji Emote => _emoji;

    public void ActivarYepmpde(DiscordEmoji emojiNuevo)
    {
        if (!_yepMode)
        {
            _yepMode = true;
            _emoji = emojiNuevo;
        }
        else
        {
            _emoji = emojiNuevo;
        }
    }

    public void DesactivarYepMode()
    {
        if (_yepMode)
        {
            _yepMode = false;
            _emoji = null;
        }
    }
    #endregion

    #region Cache de usuarios AniList
    public List<UsuarioAnilist> Usuarios => usuarios;

    public void SetUsuarios(List<UsuarioAnilist> newUsers)
    {
        usuarios = newUsers;
    }
    #endregion

    #region Canales de VC temporales
    public bool EsCanalTemporal(ulong id)
    {
        return TempVoiceChannels.Contains(id);
    }

    public void AgregarCanalTemporal(ulong id)
    {
        if (!EsCanalTemporal(id)) TempVoiceChannels.Add(id);
    }

    public void EliminarCanalTemporal(ulong id)
    {
        if (!EsCanalTemporal(id)) TempVoiceChannels.Remove(id);
    }
    #endregion

    #region Highlights
    public void SetHighlightedWords(Dictionary<ulong, List<string>> highlights)
    {
        highlightedWords = highlights;
    }

    public Dictionary<ulong, List<string>> GetHighlightedWords()
    {
        return highlightedWords;
    }

    public void AddHighlightedWord(ulong userId, string word)
    {
        bool encontro = highlightedWords.TryGetValue(userId, out var words);
        if (encontro)
        {
            words.Add(word);
            highlightedWords.Add(userId, words);
        }
        else
        {
            List<string> wordList = new()
            {
                word
            };
            highlightedWords.Add(userId, wordList);
        }
    }

    public void RemoveHighlightedWord(ulong userId, string word)
    {
        bool encontro = highlightedWords.TryGetValue(userId, out var words);
        if (encontro)
        {
            words.Remove(word);
        }
    }
    #endregion

    #region Triggers
    public Dictionary<string, Trigger> GetActiveTriggers()
    {
        return _triggers;
    }

    public void SetTrigger(Trigger trigger)
    {
        if (_triggers.TryGetValue(trigger.Nombre, out _))
        {
            _triggers.Remove(trigger.Nombre);
            _triggers.Add(trigger.Nombre, trigger);
        }
        else
        {
            _triggers.Add(trigger.Nombre, trigger);
        }
    }

    public void RemoveTriggerFromActiveList(string triggerName)
    {
        if (_triggers.TryGetValue(triggerName, out _))
        {
            _triggers.Remove(triggerName);
        }
    }
    #endregion

    #region Control usuarios activos/inactivos
    public async Task AddDailyActiveUser(ulong id, DiscordGuild guild)
    {
        if (!_dailyActiveUsers.Any(x => x == (long)id))
        {
            DiscordRole inactivoRole = guild.Roles[1205567006363877426];
            await usuariosActivosRepository.SetUsuarioActividad((long)id);

            if (guild.Members.TryGetValue(id, out DiscordMember? member) && member.Roles.Any(x => x.Id == inactivoRole.Id))
            {
                await member.RevokeRoleAsync(inactivoRole);
            }

            _dailyActiveUsers.Add((long)id);
        }
    }
    #endregion

    #region Permiso de mandar invites a otros discords
    public Dictionary<ulong, DateTime> GetLinkRoleUsers()
    {
        return _linkRoleUsers;
    }

    public void AddLinkRoleUser(ulong id, DateTime expiration)
    {
        if (!_linkRoleUsers.ContainsKey(id))
        {
            _linkRoleUsers.Add(id, expiration);
        }
    }

    public void RemoveLinkRoleUser(ulong id)
    {
        _linkRoleUsers.Remove(id);
    }
    #endregion

    #region Control cuentas hackeadas
    public bool IsHackedAccount(ulong userId)
    {
        if (_lastMessagesUsers.TryGetValue(userId, out var userMessages))
        {
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
        }

        return false;
    }

    public void AddRecentUserMessage(ulong userId, ulong channelId, string content)
    {
        if (_lastMessagesUsers.TryGetValue(userId, out var userMessages))
        {
            List<BasicMessage> newList = [];

            List<BasicMessage> oldList = userMessages;

            if (userMessages.Count == 3)
            {
                oldList = userMessages.Skip(1).ToList();
            }

            foreach (var message in oldList)
            {
                newList.Add(new(message.Content, message.ChannelId, message.CreatedAt));
            }

            newList.Add(new(content, channelId, DateTime.Now));

            _lastMessagesUsers[userId] = newList;
        }
        else
        {
            _lastMessagesUsers.Add(userId, [new(content, channelId, DateTime.Now)]);
        }
    }
    #endregion

    #region Teiou cooldown
    public bool TeiouInCooldown(ulong id)
    {
        if (!_teiouNicknameCooldown.ContainsKey(id))
            return false;

        var cd = _teiouNicknameCooldown[id];
        return cd > DateTime.Now;
    }

    public double GetHoursCooldownTeiou(ulong id)
    {
        return (DateTime.Now - _teiouNicknameCooldown[id]).TotalHours;
    }

    public void AddTeiouCooldown(ulong id)
    {
        DateTime cd = DateTime.Now.AddHours(24);

        if (!_teiouNicknameCooldown.ContainsKey(id))
        {
            _teiouNicknameCooldown.Add(id, cd);
        }
        else
        {
            _teiouNicknameCooldown[id] = cd;
        }
    }

    public void FillTeiouFromDb(List<TeiouCooldownNickname> list)
    {
        _teiouNicknameCooldown = [];

        foreach (var t in list)
        {
            var myDt = DateTime.SpecifyKind(t.Cooldown, DateTimeKind.Utc);

            _teiouNicknameCooldown.Add((ulong)t.UserId, myDt.ToLocalTime());
        }
    }
    #endregion

    #region Boosters extra xp
    public void FillBoosters(List<ulong> users)
    {
        _boosters = users;
    }

    public List<ulong> GetBoosters()
    {
        return _boosters;
    }
    #endregion

    #region Xp por minuto
    public void AddMemberToObtainXp(ulong userId)
    {
        if (!_usersXp.Exists(x => x == userId))
        {
            _usersXp.Add(userId);
        }
    }

    public void ResetMembersToObtainXp()
    {
        _usersXp.Clear();
    }

    public List<ulong> GetMembersToObtainXp()
    {
        return _usersXp;
    }
    #endregion

    #region Cache de xp del servidor
    public void FillGuildXp(Dictionary<ulong, UserXp> users)
    {
        _generalXp = users;
    }

    public void UpdateUserXp(ulong userId, long xp, TipoXp tipo)
    {
        if (_generalXp.ContainsKey(userId))
        {
            var value = _generalXp[userId];

            switch (tipo)
            {
                case TipoXp.Total:
                    value.Total = xp;
                    break;
                case TipoXp.Booster:
                    value.Booster = xp;
                    break;
                case TipoXp.Challenges:
                    value.Challenges = xp;
                    break;
                case TipoXp.Eventos:
                    value.Eventos = xp;
                    break;
                case TipoXp.Intercambios:
                    value.Intercambios = xp;
                    break;
                default:
                    value.Otros = xp;
                    break;
            }

            _generalXp[userId] = value;
        }
        else
        {
            UserXp value = new UserXp
            {
                Total = xp,
                Booster = 0,
                Challenges = 0,
                Eventos = 0,
                Intercambios = 0,
                Otros = 0,
                UserId = (long)userId
            };

            _generalXp.Add(userId, value);
        }
    }

    public List<UserXp> GetGuildXp(DiscordGuild guild)
    {
        List<UserXp> ret = new();

        foreach (var xpUsr in _generalXp)
        {
            if (guild.Members.TryGetValue(xpUsr.Key, out _))
            {
                ret.Add(xpUsr.Value);
            }
        }

        return ret;
    }

    public UserXp GetUserXp(ulong userId)
    {
        return _generalXp.TryGetValue(userId, out UserXp? xp) ? xp : new UserXp();
    }
    #endregion

    #region Daily XP Chart
    public async Task AddUserXpToChartHistory(ulong userId, long xpFromDay, DateTime date)
    {
        if (_dailyXp.TryGetValue(userId, out var xp))
        {
            var list = xp;
            list.Add(new UserDailyXp()
            {
                Date = date,
                UserId = (long)userId,
                Xp = xpFromDay
            });
            _dailyXp[userId] = list;
        }
        else
        {
            xp = await usuariosDiscordRepository.GetDailyXpChartFromUser(userId, DateRangeXp.Anual);
            _dailyXp.Add(userId, xp);
        }
    }

    public async Task<List<UserDailyXp>> GetUserChartHistory(
        ulong userId,
        bool includeZeroXp = false,
        bool rellenarDiasFaltantes = true)
    {
        List<UserDailyXp> listTmp;
        if (_dailyXp.TryGetValue(userId, out var xp))
        {
            listTmp = xp;
        }
        else
        {
            var chart = await usuariosDiscordRepository.GetDailyXpChartFromUser(userId, DateRangeXp.Anual);
            _dailyXp.Add(userId, chart);
            listTmp = chart;
        }

        int yearActual = DateTime.Now.Year;
        var registrosAnteriores = listTmp.Where(x => x.Date.Year != yearActual).OrderBy(x => x.Date).ToList();
        var registrosActual = listTmp.Where(x => x.Date.Year == yearActual).OrderBy(x => x.Date).ToList();

        List<UserDailyXp> resultado = new();
        resultado.AddRange(registrosAnteriores);

        if (!rellenarDiasFaltantes)
        {
            resultado.AddRange(registrosActual);

            DateTime hoy = DateTime.Today;
            if (!registrosActual.Any(x => x.Date.Date == hoy))
            {
                long lastXp = registrosActual.LastOrDefault()?.Xp ?? registrosAnteriores.LastOrDefault()?.Xp ?? 0;
                resultado.Add(new UserDailyXp
                {
                    Date = hoy,
                    UserId = (long)userId,
                    Xp = lastXp
                });
            }

            return resultado.OrderBy(x => x.Date).ToList();
        }

        if (registrosActual.Count > 0)
        {
            DateTime startDate = new DateTime(yearActual, 1, 1);
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
                    var reg = new UserDailyXp
                    {
                        Date = startDate,
                        UserId = (long)userId,
                        Xp = lastXp
                    };
                    resultado.Add(reg);

                    if (lastXp != 0)
                    {
                        _ = usuariosDiscordRepository.AddDailyXp(startDate, userId, lastXp);
                        modified = true;
                    }
                }
                startDate = startDate.AddDays(1);
            }

            if (modified)
            {
                _dailyXp[userId] = resultado;
            }
        }

        return resultado.OrderBy(x => x.Date).ToList();
    }

    public void ResetXpChartHistory()
    {
        _dailyXp.Clear();
    }
    #endregion

    #region boluditos
    public bool IsBoludito(ulong userId)
    {
        return _boluditos.Contains(userId);
    }

    public void AddBoludito(ulong userId)
    {
        if (!_boluditos.Contains(userId))
        {
            _boluditos.Add(userId);
        }
    }

    public void ResetBoluditos()
    {
        _boluditos = new List<ulong>();
    }
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
        bool added = _dailyConfessionUsers.TryAdd(userId, messageId);
        if (added)
        {
            _confessions.Add(userId, []);
        }
    }

    public bool UserConfessed(ulong userId) => _dailyConfessionUsers.ContainsKey(userId);

    public bool MessageConfessionGuessed(ulong messageId) => _dailyConfessedMessages.ContainsKey(messageId);

    public bool IsConfession(ulong messageId) => _dailyConfessionUsers.ContainsValue(messageId);

    public (bool, ulong?, ulong?) AddConfessionReaction(ulong messageId, ulong userReactedId)
    {
        var confessionUser = _dailyConfessionUsers.First(x => x.Value == messageId);
        var confessionReactions = _confessions[confessionUser.Key];

        if (!confessionReactions.Contains(userReactedId) && userReactedId != confessionUser.Key)
        {
            confessionReactions.Add(userReactedId);
            _confessions[confessionUser.Key] = confessionReactions;

            var revealPercentage = confessionReactions.Count * 5;
            if (NumberHelper.GetNumeroRandom(0, 100) <= revealPercentage)
            {
                _dailyConfessedMessages.Add(confessionUser.Value, confessionUser.Key);
                return (true, confessionUser.Value, confessionUser.Key);
            }
        }

        return (false, null, null);
    }
    #endregion

    #region Anilist baneados
    public async Task SetAnilistBaneados()
    {
        var baneados = await usuariosAnilistRepository.GetListaUsuariosBaneados();
        _anilistBaneados = [.. baneados.Select(x => x.AnilistUserId)];
    }

    public List<int> GetAnilistUsersBaneados()
    {
        return _anilistBaneados;
    }

    public async Task AddAnilistUserBaneado(int userId)
    {
        bool encontro = _anilistBaneados.Any(x => x == userId);
        if (!encontro)
        {
            _anilistBaneados.Add(userId);
            await usuariosAnilistRepository.AgregarUsuarioBaneado(userId);
        }
    }

    public async Task RemoveAnilistUserBaneado(int userId)
    {
        bool encontro = _anilistBaneados.Any(x => x == userId);
        if (encontro)
        {
            _anilistBaneados.Remove(userId);
            await usuariosAnilistRepository.DeleteUsuarioBaneado(userId);
        }
    }

    public bool IsAnilistUserBaneado(int userId)
    {
        return _anilistBaneados.Any(x => x == userId);
    }
    #endregion

    #region Debug XP
    public (bool, ulong) GetDebugXp()
    {
        return _debugXp;
    }

    public void EnableDebugXp(ulong id)
    {
        _debugXp = (true, id);
    }

    public void DisableDebugXp()
    {
        _debugXp = (false, 0);
    }
    #endregion

    #region Permanent Usernames
    public void SetPermanentUsername(ulong userId, string username)
    {
        if (_permanentUsernames.ContainsKey(userId))
        {
            _permanentUsernames[userId] = username;
        }
        else
        {
            _permanentUsernames.Add(userId, username);
        }
    }

    public Dictionary<ulong, string> GetPermanentUsernames()
    {
        return _permanentUsernames;
    }

    public void RemovePermanentUsername(ulong userId)
    {
        if (_permanentUsernames.ContainsKey(userId))
        {
            _permanentUsernames.Remove(userId);
        }
    }
    #endregion
}
