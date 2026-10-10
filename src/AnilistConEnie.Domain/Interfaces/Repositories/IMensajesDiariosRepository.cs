using AnilistConEnie.Domain.Entities;

namespace AnilistConEnie.Domain.Interfaces.Repositories;

public interface IMensajesDiariosRepository
{
    Task Incrementar(ulong userId, DateOnly fecha);
    Task Purgar(DateOnly fechaMinima);
    Task<List<UsuarioMensajes>> ObtenerTotales(DateOnly desde, DateOnly hasta);
}
