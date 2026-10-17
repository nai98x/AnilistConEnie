using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;

namespace AnilistConEnie.Infrastructure.Firebase;

public class FirebaseService
{
    private const string AnilistConEnieCredentialFile = "firebase-anilistconenie.json";

    private readonly string _credentialsDir;
    private readonly SemaphoreSlim _storageLock = new(1, 1);
    private volatile StorageCliente? _storage;

    private sealed record StorageCliente(StorageClient Client, string Bucket);

    public FirebaseService(string credentialsDir)
    {
        _credentialsDir = credentialsDir;
    }

    public async Task<(StorageClient Client, string Bucket)> GetAnilistConEnieStorageAsync()
    {
        if (_storage is { } storage) return (storage.Client, storage.Bucket);

        await _storageLock.WaitAsync();
        try
        {
            if (_storage is { } creado) return (creado.Client, creado.Bucket);

            string jsonCredentials = await File.ReadAllTextAsync(CredentialPath(AnilistConEnieCredentialFile));
            StorageClient client = await StorageClient.CreateAsync(CredentialFrom(jsonCredentials));
            StorageCliente nuevo = new(client, $"{ProjectId(jsonCredentials, AnilistConEnieCredentialFile)}.appspot.com");
            _storage = nuevo;
            return (nuevo.Client, nuevo.Bucket);
        }
        finally
        {
            _storageLock.Release();
        }
    }

    private static GoogleCredential CredentialFrom(string jsonCredentials) =>
        CredentialFactory.FromJson<ServiceAccountCredential>(jsonCredentials).ToGoogleCredential();

    private string CredentialPath(string credentialFile) => Path.Combine(_credentialsDir, credentialFile);

    private static string ProjectId(string jsonCredentials, string credentialFile) =>
        JsonDocument.Parse(jsonCredentials).RootElement.GetProperty("project_id").GetString()
        ?? throw new InvalidOperationException($"'project_id' no encontrado en {credentialFile}");
}
