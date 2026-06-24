namespace AnilistConEnie.Model.Interfaces.Repositories;

public interface IImagenStorageRepository
{
    Task<string> UploadImageAsync(Stream stream, string fileName, ulong userId);
}
