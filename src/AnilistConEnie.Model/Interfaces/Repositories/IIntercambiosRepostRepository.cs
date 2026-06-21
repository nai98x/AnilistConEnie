using AnilistConEnie.Model.Entities;

namespace AnilistConEnie.Model.Interfaces.Repositories;

public interface IIntercambiosRepostRepository
{
    Task<MensajeIntercambioRepost?> GetMensaje(ulong idMensajeHiloForo);
    Task SetMensaje(ulong idCanalHiloForo, ulong idMensajeHiloForo, ulong idCanalMensajeRepost, ulong idMensajeRepost);
    Task DeleteMensaje(ulong idMensajeHiloForo);
}
