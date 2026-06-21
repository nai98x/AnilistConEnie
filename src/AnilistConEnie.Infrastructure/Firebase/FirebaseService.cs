using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;

namespace AnilistConEnie.Infrastructure.Firebase;

public class FirebaseService
{
    private FirestoreDb? _anilistConEnie;
    private FirestoreDb? _yumiko;

    public async Task<FirestoreDb> GetAnilistConEnie()
    {
        if (_anilistConEnie != null) return _anilistConEnie;

        string path = AppDomain.CurrentDomain.BaseDirectory + "firebase-anilistconenie.json";
        string jsonString = await File.ReadAllTextAsync(path);
        FirestoreClientBuilder builder = new() { JsonCredentials = jsonString };
        _anilistConEnie = await FirestoreDb.CreateAsync("anilistconenie-e09cb", await builder.BuildAsync());
        return _anilistConEnie;
    }

    public FirestoreDb GetYumiko()
    {
        if (_yumiko != null) return _yumiko;

        string path = AppDomain.CurrentDomain.BaseDirectory + "firebase-yumiko.json";
        string jsonString = File.ReadAllText(path);
        FirestoreClientBuilder builder = new() { JsonCredentials = jsonString };
        _yumiko = FirestoreDb.Create("yumiko-1590195019393", builder.Build());
        return _yumiko;
    }
}
