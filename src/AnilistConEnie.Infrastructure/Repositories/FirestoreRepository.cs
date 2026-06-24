using AnilistConEnie.Infrastructure.Firebase;
using Google.Cloud.Firestore;

namespace AnilistConEnie.Infrastructure.Repositories;

public abstract class FirestoreRepository(FirebaseService firebase)
{
    protected Task<FirestoreDb> GetDbAsync() => firebase.GetAnilistConEnie();

    protected FirestoreDb GetYumikoDb() => firebase.GetYumiko();

    protected static async Task<List<T>> QueryListAsync<T>(Query query)
    {
        QuerySnapshot snap = await query.GetSnapshotAsync();
        return snap.Count > 0
            ? snap.Documents.Select(d => d.ConvertTo<T>()).ToList()
            : [];
    }

    protected static async Task<T?> GetDocumentAsync<T>(DocumentReference doc) where T : class
    {
        DocumentSnapshot snap = await doc.GetSnapshotAsync();
        return snap.Exists ? snap.ConvertTo<T>() : null;
    }

    protected static Task UpsertAsync(DocumentReference doc, Dictionary<string, object> data)
        => doc.SetAsync(data, SetOptions.MergeAll);

    protected static async Task SetIfAbsentAsync(DocumentReference doc, object data)
    {
        DocumentSnapshot snap = await doc.GetSnapshotAsync();
        if (!snap.Exists)
            await doc.SetAsync(data);
    }

    protected static async Task DeleteIfExistsAsync(DocumentReference doc)
    {
        DocumentSnapshot snap = await doc.GetSnapshotAsync();
        if (snap.Exists)
            await doc.DeleteAsync();
    }
}
