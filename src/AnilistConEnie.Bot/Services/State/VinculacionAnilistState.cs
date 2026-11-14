using System.Buffers.Text;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using DSharpPlus.Entities;

namespace AnilistConEnie.Bot.Services.State;

public sealed record VinculacionPendiente(DiscordInteraction Interaccion, DateTimeOffset Vence);

/// <summary>
/// Enlaces de autorización de AniList entregados y todavía sin usar, indexados por el <c>state</c> de
/// OAuth: es lo que permite saber, cuando AniList redirige al callback, a qué miembro corresponde el
/// código. Cada miembro tiene a lo sumo un enlace vigente.
/// </summary>
public class VinculacionAnilistState
{
    private readonly ConcurrentDictionary<string, VinculacionPendiente> _pendientes = new();

    public string Registrar(DiscordInteraction interaccion, TimeSpan validez)
    {
        DateTimeOffset ahora = DateTimeOffset.UtcNow;
        foreach ((string state, VinculacionPendiente pendiente) in _pendientes)
        {
            if (pendiente.Vence <= ahora || pendiente.Interaccion.User.Id == interaccion.User.Id)
                _pendientes.TryRemove(state, out _);
        }

        string nuevo = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));
        _pendientes[nuevo] = new VinculacionPendiente(interaccion, ahora + validez);
        return nuevo;
    }

    public VinculacionPendiente? Consumir(string state) =>
        _pendientes.TryRemove(state, out VinculacionPendiente? pendiente) && pendiente.Vence > DateTimeOffset.UtcNow
            ? pendiente
            : null;
}
