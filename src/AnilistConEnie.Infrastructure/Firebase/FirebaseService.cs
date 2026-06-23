using System.Text.Json;
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;

namespace AnilistConEnie.Infrastructure.Firebase;

public class FirebaseService
{
    private const string AnilistConEnieCredentialFile = "firebase-anilistconenie.json";
    private const string YumikoCredentialFile = "firebase-yumiko.json";

    private readonly Lazy<Task<FirestoreDb>> _anilistConEnie = new(() => CreateAsync(AnilistConEnieCredentialFile));
    private readonly Lazy<FirestoreDb> _yumiko = new(() => Create(YumikoCredentialFile));

    public Task<FirestoreDb> GetAnilistConEnie() => _anilistConEnie.Value;

    public FirestoreDb GetYumiko() => _yumiko.Value;

    private static async Task<FirestoreDb> CreateAsync(string credentialFile)
    {
        string jsonCredentials = await File.ReadAllTextAsync(CredentialPath(credentialFile));
        FirestoreClientBuilder builder = new() { JsonCredentials = jsonCredentials };
        return await FirestoreDb.CreateAsync(ProjectId(jsonCredentials, credentialFile), await builder.BuildAsync());
    }

    private static FirestoreDb Create(string credentialFile)
    {
        string jsonCredentials = File.ReadAllText(CredentialPath(credentialFile));
        FirestoreClientBuilder builder = new() { JsonCredentials = jsonCredentials };
        return FirestoreDb.Create(ProjectId(jsonCredentials, credentialFile), builder.Build());
    }

    private static string CredentialPath(string credentialFile) =>
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, credentialFile);

    private static string ProjectId(string jsonCredentials, string credentialFile) =>
        JsonDocument.Parse(jsonCredentials).RootElement.GetProperty("project_id").GetString()
        ?? throw new InvalidOperationException($"'project_id' no encontrado en {credentialFile}");
}
