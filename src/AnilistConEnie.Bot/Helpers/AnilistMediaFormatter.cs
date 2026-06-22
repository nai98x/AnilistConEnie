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

    /// <summary>Subtítulo corto para listar un resultado en el selector: "Formato - Año".</summary>
    public static string OptionSubtitle(AnilistMedia media)
    {
        string formato = string.IsNullOrEmpty(media.Format) ? "?" : media.Format;
        int? anio = media.SeasonYear ?? media.StartDate?.Year;
        return $"{formato} - {(anio?.ToString() ?? "No emitido")}";
    }

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

    /// <summary>Muestra el score de un usuario en su propio formato de AniList (POINT_100/10/5/3…).</summary>
    public static string UserScore(AnilistUserScore score) => score.ScoreFormat switch
    {
        "POINT_10_DECIMAL" => $"{score.RawScore:0.#}/10",
        "POINT_10" => $"{score.RawScore:0}/10",
        "POINT_5" => $"{score.RawScore:0}/5",
        "POINT_3" => $"{score.RawScore:0}/3",
        _ => $"{score.RawScore:0}/100"
    };

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
