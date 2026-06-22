namespace AnilistConEnie.Model.Entities.Anilist;

/// <summary>
/// Entrada de la lista de un usuario de AniList para un media puntual (su score, estado y progreso).
/// <see cref="RawScore"/> viene en el formato elegido por el usuario (<see cref="ScoreFormat"/>);
/// <see cref="Score100"/> es el mismo score normalizado a /100 por la propia API (para promediar).
/// </summary>
public class AnilistUserScore
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserSiteUrl { get; set; } = string.Empty;
    public string? ScoreFormat { get; set; }
    public double RawScore { get; set; }
    public double Score100 { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? Progress { get; set; }

    public bool HasScore => RawScore > 0;
}
