namespace AnilistConEnie.Domain.Interfaces.Repositories;

public interface IFirebaseRepository
{
    Task<string> UploadImageAsync(Stream stream, string fileName, ulong userId);
}
