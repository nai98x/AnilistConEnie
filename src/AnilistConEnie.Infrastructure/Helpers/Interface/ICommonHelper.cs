using Google.Cloud.Firestore;

namespace AnilistConEnie.Infrastructure.Helpers.Interface
{
    public interface ICommonHelper
    {
        Task<FirestoreDb> GetFirestoreClientAnilistConEnie();
        FirestoreDb GetFirestoreClientYumiko();
        Task<byte[]> MergeImage(string link1, string link2, int x, int y);
        byte[] OverlapImage(byte[] image1, byte[] image2, int x, int y);
        MemoryStream ToMemoryStream(byte[] byteArray);
    }
}
