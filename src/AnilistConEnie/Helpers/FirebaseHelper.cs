using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;

namespace AnilistConEnie.Helpers;

public static class FirebaseHelper
{
    public static FirestoreDb GetFirestoreClientYumiko()
    {
        string path = AppDomain.CurrentDomain.BaseDirectory + @"firebase-yumiko.json";
        string jsonString = File.ReadAllText(path);
        FirestoreClientBuilder builder = new() { JsonCredentials = jsonString };
        return FirestoreDb.Create("yumiko-1590195019393", builder.Build());
    }

    public static async Task<FirestoreDb> GetFirestoreClientAnilistConEnie()
    {
        string path = AppDomain.CurrentDomain.BaseDirectory + @"firebase-anilistconenie.json";
        string jsonString = await File.ReadAllTextAsync(path);
        FirestoreClientBuilder builder = new() { JsonCredentials = jsonString };
        return await FirestoreDb.CreateAsync("anilistconenie-e09cb", await builder.BuildAsync());
    }
}
