using System.Collections.Concurrent;

namespace AnilistConEnie.Bot.Services.State;

/// <summary>
/// Mensajes con botones de aprobación (repartos de XP, vinculaciones) que ya empezaron a resolverse,
/// para que dos clicks casi simultáneos no apliquen la misma resolución dos veces.
/// </summary>
public class ResolucionesState
{
    private readonly ConcurrentDictionary<ulong, byte> _mensajes = new();

    public bool TryIniciar(ulong messageId) => _mensajes.TryAdd(messageId, 0);

    public bool EstaResuelto(ulong messageId) => _mensajes.ContainsKey(messageId);

    public void Liberar(ulong messageId) => _mensajes.TryRemove(messageId, out _);
}
