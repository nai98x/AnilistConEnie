using AnilistConEnie.Application.Helpers;
using AnilistConEnie.Model.Entities;
using AnilistConEnie.Model.Enum;

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
