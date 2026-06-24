namespace AnilistConEnie.Model.Entities.Anilist;

/// <summary>
/// Estado del rate limit de la API de AniList, leído de los headers <c>X-RateLimit-*</c>
/// de la última respuesta. Los valores son nullable porque no todas las respuestas
/// incluyen los headers.
/// </summary>
public class AnilistRateLimit
{
    /// <summary>Cantidad máxima de requests permitidas en la ventana actual.</summary>
    public int? Limit { get; set; }

    /// <summary>Requests restantes en la ventana actual.</summary>
    public int? Remaining { get; set; }

    /// <summary>Momento en que la ventana se reinicia (si el header lo informa).</summary>
    public DateTimeOffset? ResetAt { get; set; }
}
