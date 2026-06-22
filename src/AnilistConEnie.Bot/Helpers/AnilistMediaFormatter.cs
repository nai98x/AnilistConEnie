using AnilistConEnie.Application.Helpers;
using AnilistConEnie.Model.Entities.Anilist;

namespace AnilistConEnie.Bot.Helpers;

/// <summary>
/// Convierte un <see cref="AnilistMedia"/> (datos crudos) a los strings listos para mostrar en un
/// embed de Discord. La presentación vive en la capa Bot; el modelo de dominio se mantiene limpio.
/// </summary>
public static class AnilistMediaFormatter
{
    public static string DisplayTitle(AnilistMedia media) =>
        media.Title?.Romaji ?? media.Title?.English ?? media.Title?.Native ?? "(Sin título)";

    public static string Genres(AnilistMedia media) => string.Join(", ", media.Genres);

    /// <summary>Une los tags separados por coma; los marcados como spoiler se envuelven en <c>||…||</c>.</summary>
    public static string Tags(AnilistMedia media) =>
        string.Join(", ", media.Tags.Select(t => t.IsSpoiler ? $"||{t.Name}||" : t.Name));

    public static string Studios(AnilistMedia media) =>
        string.Join(", ", media.Studios.Select(s => $"[{s.Name}]({s.SiteUrl})"));

    public static string ExternalLinks(AnilistMedia media) =>
        string.Join(", ", media.ExternalLinks.Select(l => $"[{l.Site}]({l.Url})"));

    public static string Score(AnilistMedia media) =>
        media.MeanScore is { } score ? $"{score}/100" : "—";

    public static string Status(AnilistMedia media)
    {
        if (string.IsNullOrEmpty(media.Status)) return "—";
        string lower = media.Status.ToLowerInvariant();
        return char.ToUpperInvariant(lower[0]) + lower[1..];
    }

    public static string Description(AnilistMedia media)
    {
        string clean = StringHelper.NormalizarDescription(StringHelper.LimpiarTexto(media.Description ?? string.Empty));
        return string.IsNullOrEmpty(clean) ? "(Sin descripción)" : clean;
    }

    public static string Dates(AnilistMedia media)
    {
        if (media.StartDate is not { Day: not null } start)
            return "Sin fecha de emisión";

        string startText = $"{start.Day}/{start.Month}/{start.Year}";

        if (media.EndDate is { Day: not null } end)
            return $"{startText} al {end.Day}/{end.Month}/{end.Year}";

        return $"En emisión desde {startText}";
    }
}
