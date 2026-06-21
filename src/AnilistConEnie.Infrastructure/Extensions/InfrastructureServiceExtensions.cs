using AnilistConEnie.Infrastructure.Firebase;
using AnilistConEnie.Infrastructure.Repositories;
using AnilistConEnie.Model.Interfaces.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace AnilistConEnie.Infrastructure.Extensions;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<FirebaseService>();

        services.AddSingleton<IChallengesRepository, ChallengesRepository>();
        services.AddSingleton<IUsuariosAnilistRepository, UsuariosAnilistRepository>();
        services.AddSingleton<IUsuariosDiscordRepository, UsuariosDiscordRepository>();
        services.AddSingleton<ITriggersRepository, TriggersRepository>();
        services.AddSingleton<ITemazosRepository, TemazosRepository>();
        services.AddSingleton<IPremiosRepository, PremiosRepository>();
        services.AddSingleton<IImagenesRepository, ImagenesRepository>();
        services.AddSingleton<IIntercambiosRepostRepository, IntercambiosRepostRepository>();
        services.AddSingleton<IUsuariosActivosRepository, UsuariosActivosRepository>();

        return services;
    }
}
