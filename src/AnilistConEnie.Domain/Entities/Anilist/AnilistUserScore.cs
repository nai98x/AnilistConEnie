namespace AnilistConEnie.Domain.Entities.Anilist;

/// <summary>
/// Entrada de la lista de un usuario de AniList para un media puntual (su score, estado y progreso).
/// <see cref="RawScore"/> viene en el formato elegido por el usuario (<see cref="ScoreFormat"/>);
/// <see cref="Score100"/> es el mismo score normalizado a /100 por la propia API (para promediar).
/// </summary>
public record AnilistUserScore
{
    public int UserId { get; init; }
    public string UserName { get; init; } = string.Empty;
    public string UserSiteUrl { get; init; } = string.Empty;
    public string? ScoreFormat { get; init; }
    public double RawScore { get; init; }
    public double Score100 { get; init; }
    public string Status { get; init; } = string.Empty;
    public int? Progress { get; init; }

    public bool HasScore => RawScore > 0;
}
