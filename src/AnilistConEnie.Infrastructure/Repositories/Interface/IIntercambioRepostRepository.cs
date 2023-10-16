using AnilistConEnie.Domain.Firebase;

namespace AnilistConEnie.Infrastructure.Repositories.Interface
{
    public interface IIntercambioRepostRepository
    {
        Task<MensajeIntercambioRepost?> GetMensaje(ulong idMensajeHiloForo);
        Task SetMensaje(ulong idCanalHiloForo, ulong idMensajeHiloForo, ulong idCanalMensajeRepost, ulong idMensajeRepost);
        Task DeleteMensaje(ulong idMensajeHiloForo);
    }
}
