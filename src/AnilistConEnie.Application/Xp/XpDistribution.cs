using AnilistConEnie.Model.Entities;

namespace AnilistConEnie.Application.Xp;

public enum XpCategory
{
    Mensajes,
    Challenges,
    Eventos,
    Intercambios,
    Otros
}

/// <summary>Aporte de una categoría a la XP total del usuario: valor absoluto y porcentaje.</summary>
public readonly record struct XpCategoryShare(XpCategory Category, long Value, decimal Percentage);

public static class XpDistribution
{
    /// <summary>
    /// Reparte la XP total del usuario entre sus categorías (mensajes, challenges, eventos, intercambios, otros).
    /// Requiere <see cref="UserXp.Total"/> mayor a cero. El porcentaje de "Mensajes" se calcula como el resto
    /// para que la suma cierre en 100.
    /// </summary>
    public static IReadOnlyList<XpCategoryShare> Build(UserXp rank)
    {
        long total = rank.Total;
        long challenges = rank.Challenges;
        long eventos = rank.Eventos;
        long intercambios = rank.Intercambios;
        long otros = rank.Otros;

        decimal challengesPct = Math.Round(challenges * 100m / total, 2);
        decimal eventosPct = Math.Round(eventos * 100m / total, 2);
        decimal intercambiosPct = Math.Round(intercambios * 100m / total, 2);
        decimal otrosPct = Math.Round(otros * 100m / total, 2);

        long mensajes = total - challenges - eventos - intercambios - otros;
        decimal mensajesPct = 100 - challengesPct - eventosPct - intercambiosPct - otrosPct;

        return
        [
            new XpCategoryShare(XpCategory.Mensajes, mensajes, mensajesPct),
            new XpCategoryShare(XpCategory.Challenges, challenges, challengesPct),
            new XpCategoryShare(XpCategory.Eventos, eventos, eventosPct),
            new XpCategoryShare(XpCategory.Intercambios, intercambios, intercambiosPct),
            new XpCategoryShare(XpCategory.Otros, otros, otrosPct),
        ];
    }
}
