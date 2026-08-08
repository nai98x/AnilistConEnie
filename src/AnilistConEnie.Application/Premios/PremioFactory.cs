using AnilistConEnie.Application.Helpers;
using AnilistConEnie.Domain.Entities;
using AnilistConEnie.Domain.Enum;

namespace AnilistConEnie.Application.Premios;

public static class PremioFactory
{
    public static Premio Crear(int anio, Season season, string link) =>
        new()
        {
            Nombre = $"{season.GetName()} {anio}",
            Link = link,
            Year = anio,
            Order = (int)season
        };
}
