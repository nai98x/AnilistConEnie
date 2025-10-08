using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;

namespace AnilistConEnie.Helpers;

public static class FirebaseHelper
{
    public static FirestoreDb GetFirestoreClientYumiko()
    {
        string path = AppDomain.CurrentDomain.BaseDirectory + @"firebase-yumiko.json";
        var jsonString = File.ReadAllText(path);
        var builder = new FirestoreClientBuilder { JsonCredentials = jsonString };
        return FirestoreDb.Create("yumiko-1590195019393", builder.Build());
    }

    public static async Task<FirestoreDb> GetFirestoreClientAnilistConEnie()
    {
        string path = AppDomain.CurrentDomain.BaseDirectory + @"firebase-anilistconenie.json";
        var jsonString = File.ReadAllText(path);
        var builder = new FirestoreClientBuilder { JsonCredentials = jsonString };
        return await FirestoreDb.CreateAsync("anilistconenie-e09cb", builder.Build());
    }
}
