using AnilistConEnie.Infrastructure.Anilist;
using AnilistConEnie.Infrastructure.Charts;
using AnilistConEnie.Infrastructure.Database;
using AnilistConEnie.Infrastructure.Firebase;
using AnilistConEnie.Infrastructure.Repositories;
using AnilistConEnie.Model.Interfaces;
using AnilistConEnie.Model.Interfaces.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace AnilistConEnie.Infrastructure.Extensions;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string firebaseCredentialsDir, string dbConnectionString)
    {
        services.AddSingleton(new FirebaseService(firebaseCredentialsDir));
        services.AddSingleton(new DbConnectionFactory(dbConnectionString));
        services.AddSingleton<IUsuariosRepository, UsuariosRepository>();
        services.AddSingleton<IXpUsuariosRepository, XpUsuariosRepository>();
        services.AddSingleton<IXpDiarioRepository, XpDiarioRepository>();
        services.AddSingleton<IChallengesRepository, ChallengesRepository>();
        services.AddSingleton<ITriggersRepository, TriggersRepository>();
        services.AddSingleton<IPremiosRepository, PremiosRepository>();
        services.AddSingleton<IAnilistBaneadosRepository, AnilistBaneadosRepository>();
        services.AddSingleton<IAnilistApprovalRepository, AnilistApprovalRepository>();
        services.AddSingleton<IIntercambiosRepostRepository, IntercambiosRepostRepository>();
        services.AddSingleton<ITeiouCooldownRepository, TeiouCooldownRepository>();

        // Firebase: Yumiko (espejo del vínculo) y Storage (subir imágenes).
        services.AddSingleton<IFirebaseRepository, FirebaseRepository>();

        // Cliente de AniList: el executor (que posee el GraphQLHttpClient) y el cliente de alto
        // nivel son singletons para reutilizar la conexión HTTP entre todas las consultas.
        services.AddSingleton<AnilistGraphQLExecutor>();
        services.AddSingleton<IAnilistClient, AnilistClient>();

        // Cliente de charts (QuickChart) centralizado, vía typed HttpClient.
        services.AddHttpClient<IChartClient, QuickChartClient>();

        return services;
    }
}
