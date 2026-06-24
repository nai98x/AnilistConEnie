using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Google.Cloud.Storage.V1;

namespace AnilistConEnie.Infrastructure.Firebase;

public class FirebaseService
{
    private const string AnilistConEnieCredentialFile = "firebase-anilistconenie.json";
    private const string YumikoCredentialFile = "firebase-yumiko.json";

    private readonly Lazy<Task<FirestoreDb>> _anilistConEnie = new(() => CreateAsync(AnilistConEnieCredentialFile));
    private readonly Lazy<FirestoreDb> _yumiko = new(() => Create(YumikoCredentialFile));

    public Task<FirestoreDb> GetAnilistConEnie() => _anilistConEnie.Value;

    public FirestoreDb GetYumiko() => _yumiko.Value;

    public async Task<(StorageClient Client, string Bucket)> GetAnilistConEnieStorageAsync()
    {
        string jsonCredentials = await File.ReadAllTextAsync(CredentialPath(AnilistConEnieCredentialFile));
        GoogleCredential credential = GoogleCredential.FromJson(jsonCredentials);
        StorageClient client = await StorageClient.CreateAsync(credential);
        return (client, $"{ProjectId(jsonCredentials, AnilistConEnieCredentialFile)}.appspot.com");
    }

    private static async Task<FirestoreDb> CreateAsync(string credentialFile)
    {
        string jsonCredentials = await File.ReadAllTextAsync(CredentialPath(credentialFile));
        FirestoreDbBuilder builder = new()
        {
            ProjectId = ProjectId(jsonCredentials, credentialFile),
            JsonCredentials = jsonCredentials,
            ConverterRegistry = FirestoreConverters.Registry
        };
        return await builder.BuildAsync();
    }

    private static FirestoreDb Create(string credentialFile)
    {
        string jsonCredentials = File.ReadAllText(CredentialPath(credentialFile));
        FirestoreDbBuilder builder = new()
        {
            ProjectId = ProjectId(jsonCredentials, credentialFile),
            JsonCredentials = jsonCredentials,
            ConverterRegistry = FirestoreConverters.Registry
        };
        return builder.Build();
    }

    private static string CredentialPath(string credentialFile) =>
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, credentialFile);

    private static string ProjectId(string jsonCredentials, string credentialFile) =>
        JsonDocument.Parse(jsonCredentials).RootElement.GetProperty("project_id").GetString()
        ?? throw new InvalidOperationException($"'project_id' no encontrado en {credentialFile}");
}
