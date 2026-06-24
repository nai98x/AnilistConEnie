using AnilistConEnie.Model.Entities;
using AnilistConEnie.Model.Enum;

namespace AnilistConEnie.Model.Interfaces.Repositories;

public interface IUsuariosDiscordRepository
{
    Task<List<UsuarioDiscord>> GetListaUsuarios();
    Task<List<UserCumple>> GetBirthdaysHoy();
    Task<List<UserCumple>> GetBirthdaysAhora();
    Task<List<UserCumple>> GetBirthdays(bool month);
    Task SetBirthday(ulong userId, DateTime fecha, bool mostrarEdad, DateTime fechaOriginal);
    Task DeleteBirthday(ulong userId);
    Task<List<UserDailyXp>> GetDailyXpChartFromUser(ulong userId, DateRangeXp range = DateRangeXp.Completo);
    Task AddDailyXp(DateTime date, ulong userId, long xp);
    Task AddOrRemoveUserXp(ulong userId, UserXpDelta delta, XpOperation operation = XpOperation.Add);
    Task<List<UserXp>> GetRanking();
    Task SetCooldownTeiou(ulong userId);
    Task<List<TeiouCooldownNickname>> GetListTeiouNicknameCooldown();
}
