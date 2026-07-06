using AnilistConEnie.Model.Entities;

namespace AnilistConEnie.Model.Interfaces.Repositories;

public interface IIntercambiosRepostRepository
{
    Task<List<MensajeIntercambioRepost>> GetMensajes(ulong idMensajeHiloForo);
    Task Upsert(MensajeIntercambioRepost mensaje);
    Task SetMensaje(ulong idCanalHiloForo, ulong idMensajeHiloForo, ulong idCanalMensajeRepost, ulong idMensajeRepost);
    Task DeleteMensaje(ulong idMensajeHiloForo);
}
