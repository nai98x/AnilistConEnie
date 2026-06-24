using AnilistConEnie.Infrastructure.Anilist;
using AnilistConEnie.Infrastructure.Charts;
using AnilistConEnie.Infrastructure.Firebase;
using AnilistConEnie.Infrastructure.Repositories;
using AnilistConEnie.Model.Interfaces;
using AnilistConEnie.Model.Interfaces.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace AnilistConEnie.Infrastructure.Extensions;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string firebaseCredentialsDir)
    {
        services.AddSingleton(new FirebaseService(firebaseCredentialsDir));

        services.AddSingleton<IChallengesRepository, ChallengesRepository>();
        services.AddSingleton<IUsuariosAnilistRepository, UsuariosAnilistRepository>();
        services.AddSingleton<IUsuariosDiscordRepository, UsuariosDiscordRepository>();
        services.AddSingleton<ITriggersRepository, TriggersRepository>();
        services.AddSingleton<IPremiosRepository, PremiosRepository>();
        services.AddSingleton<IImagenesRepository, ImagenesRepository>();
        services.AddSingleton<IImagenStorageRepository, ImagenStorageRepository>();
        services.AddSingleton<IIntercambiosRepostRepository, IntercambiosRepostRepository>();
        services.AddSingleton<IUsuariosActivosRepository, UsuariosActivosRepository>();

        // Cliente de AniList: el executor (que posee el GraphQLHttpClient) y el cliente de alto
        // nivel son singletons para reutilizar la conexión HTTP entre todas las consultas.
        services.AddSingleton<AnilistGraphQLExecutor>();
        services.AddSingleton<IAnilistClient, AnilistClient>();

        // Cliente de charts (QuickChart) centralizado, vía typed HttpClient.
        services.AddHttpClient<IChartClient, QuickChartClient>();

        return services;
    }
}
