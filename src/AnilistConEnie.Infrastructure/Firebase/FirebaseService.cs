using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Google.Cloud.Storage.V1;

namespace AnilistConEnie.Infrastructure.Firebase;

public class FirebaseService
{
    private const string AnilistConEnieCredentialFile = "firebase-anilistconenie.json";
    private const string YumikoCredentialFile = "firebase-yumiko.json";

    private readonly string _credentialsDir;
    private readonly Lazy<FirestoreDb> _yumiko;

    public FirebaseService(string credentialsDir)
    {
        _credentialsDir = credentialsDir;
        _yumiko = new Lazy<FirestoreDb>(() => Create(YumikoCredentialFile));
    }

    public FirestoreDb GetYumiko() => _yumiko.Value;

    public async Task<(StorageClient Client, string Bucket)> GetAnilistConEnieStorageAsync()
    {
        string jsonCredentials = await File.ReadAllTextAsync(CredentialPath(AnilistConEnieCredentialFile));
        StorageClient client = await StorageClient.CreateAsync(CredentialFrom(jsonCredentials));
        return (client, $"{ProjectId(jsonCredentials, AnilistConEnieCredentialFile)}.appspot.com");
    }

    private FirestoreDb Create(string credentialFile)
    {
        string jsonCredentials = File.ReadAllText(CredentialPath(credentialFile));
        FirestoreDbBuilder builder = new()
        {
            ProjectId = ProjectId(jsonCredentials, credentialFile),
            GoogleCredential = CredentialFrom(jsonCredentials)
        };
        return builder.Build();
    }

    private static GoogleCredential CredentialFrom(string jsonCredentials) =>
        CredentialFactory.FromJson<ServiceAccountCredential>(jsonCredentials).ToGoogleCredential();

    private string CredentialPath(string credentialFile) => Path.Combine(_credentialsDir, credentialFile);

    private static string ProjectId(string jsonCredentials, string credentialFile) =>
        JsonDocument.Parse(jsonCredentials).RootElement.GetProperty("project_id").GetString()
        ?? throw new InvalidOperationException($"'project_id' no encontrado en {credentialFile}");
}
