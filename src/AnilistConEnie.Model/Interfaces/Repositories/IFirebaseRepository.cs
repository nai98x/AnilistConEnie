namespace AnilistConEnie.Model.Interfaces.Repositories;

public interface IFirebaseRepository
{
    Task SetAnilistYumiko(int anilistId, ulong userId);
    Task<string> UploadImageAsync(Stream stream, string fileName, ulong userId);
}
