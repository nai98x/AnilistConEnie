using AnilistConEnie.Infrastructure.Firebase.Converters;
using Google.Cloud.Firestore;

namespace AnilistConEnie.Infrastructure.Firebase;

/// <summary>
/// Registry de converters de Firestore para las entidades del dominio. Mantiene el mapeo
/// entidad &lt;-&gt; documento fuera de la capa Model (que queda como POCOs puros).
/// </summary>
public static class FirestoreConverters
{
    public static ConverterRegistry Registry { get; } = Build();

    private static ConverterRegistry Build()
    {
        ConverterRegistry registry = [];
        registry.Add(new UserXpConverter());
        registry.Add(new UserDailyXpConverter());
        registry.Add(new UserApprovalAnilistConverter());
        registry.Add(new UsuarioDiscordConverter());
        registry.Add(new UsuarioAnilistConverter());
        registry.Add(new UsuarioAnilistYumikoConverter());
        registry.Add(new UsuarioAnilistBaneadoConverter());
        registry.Add(new UsuarioActivoConverter());
        registry.Add(new TeiouCooldownNicknameConverter());
        registry.Add(new ChallengeConverter());
        registry.Add(new ChallengeCompletadoConverter());
        registry.Add(new TriggerConverter());
        registry.Add(new PremioConverter());
        registry.Add(new ImagenConverter());
        registry.Add(new MensajeIntercambioRepostConverter());
        return registry;
    }
}
