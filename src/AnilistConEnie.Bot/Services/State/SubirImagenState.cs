using System.Collections.Concurrent;

namespace AnilistConEnie.Bot.Services.State;

public class SubirImagenState
{
    private readonly ConcurrentDictionary<ulong, DateTime> _proximaSubidaPermitida = new();

    public bool EnCooldown(ulong userId, DateTime ahora) =>
        _proximaSubidaPermitida.TryGetValue(userId, out DateTime proxima) && ahora < proxima;

    public void RegistrarSubida(ulong userId, DateTime proximaPermitida) =>
        _proximaSubidaPermitida[userId] = proximaPermitida;
}
