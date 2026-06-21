using AnilistConEnie.Model.Entities;

namespace AnilistConEnie.Model.Interfaces.Repositories;

public interface IImagenesRepository
{
    Task<List<Imagen>> GetImagenesAsync(string categoria);
}
