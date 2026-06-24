using AnilistConEnie.Application.Anilist;
using AnilistConEnie.Application.Xp;
using Microsoft.Extensions.DependencyInjection;

namespace AnilistConEnie.Application.Extensions;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<AnilistServerScoreService>();
        services.AddSingleton<XpChartService>();
        return services;
    }
}
