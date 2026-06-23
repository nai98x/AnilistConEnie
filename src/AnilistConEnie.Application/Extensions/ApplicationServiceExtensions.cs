using AnilistConEnie.Application.Anilist;
using Microsoft.Extensions.DependencyInjection;

namespace AnilistConEnie.Application.Extensions;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<AnilistServerScoreService>();
        return services;
    }
}
