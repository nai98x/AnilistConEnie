using System.Collections.Concurrent;
using AnilistConEnie.Application.Helpers;
using AnilistConEnie.Domain.Entities;

namespace AnilistConEnie.Bot.Services.State;

/// <summary>
/// Fechas de entrada registradas al servidor (la primera entrada; prevalece sobre el joined_at de Discord).
/// Siempre en hora de Argentina: el día y la hora de entrada son datos de negocio y se comparan contra
/// <see cref="RelojServidor.Ahora"/>.
/// </summary>
public class FechaEntradaState
{
    private readonly ConcurrentDictionary<ulong, DateTimeOffset> _fechas = new();

    public void FillFechasEntrada(List<Usuario> usuarios)
    {
        _fechas.Clear();
        foreach (Usuario usuario in usuarios.Where(x => x.FechaEntrada != null))
            _fechas[(ulong)usuario.UserId] = RelojServidor.EnHoraLocal(usuario.FechaEntrada!.Value);
    }

    public void RegistrarSiNoExiste(ulong userId, DateTimeOffset fechaEntrada) =>
        _fechas.TryAdd(userId, RelojServidor.EnHoraLocal(fechaEntrada.UtcDateTime));

    public void SetFechaEntrada(ulong userId, DateTime fechaEntradaUtc) =>
        _fechas[userId] = RelojServidor.EnHoraLocal(fechaEntradaUtc);

    public DateTimeOffset GetFechaEntrada(ulong userId, DateTimeOffset joinedAt) =>
        _fechas.TryGetValue(userId, out DateTimeOffset fecha) ? fecha : RelojServidor.EnHoraLocal(joinedAt.UtcDateTime);
}
