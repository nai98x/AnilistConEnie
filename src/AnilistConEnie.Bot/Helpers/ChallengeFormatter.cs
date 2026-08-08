using System.Text;
using AnilistConEnie.Domain.Entities;
using DSharpPlus;

namespace AnilistConEnie.Bot.Helpers;

/// <summary>
/// Arma las descripciones de embed de los challenges: los agrupa por disponibilidad y por si tienen
/// vencimiento, y los lista como links con la fecha de vencimiento cuando corresponde.
/// </summary>
public static class ChallengeFormatter
{
    /// <summary>
    /// Lista todos los challenges agrupados en "Por tiempo limitado", "Sin limite de tiempo"
    /// (disponibles) y "No disponibles".
    /// </summary>
    public static string ChunkByDisponibilidad(List<Challenge> challenges)
    {
        Dictionary<string, List<Challenge>> grupos = [];

        foreach (IGrouping<bool, Challenge> porDisponible in challenges.GroupBy(x => x.Disponible).OrderByDescending(x => x.Key))
        {
            if (porDisponible.Key)
            {
                foreach (IGrouping<bool, Challenge> porVencimiento in porDisponible.GroupBy(x => x.Vencimiento != null))
                    grupos.Add(porVencimiento.Key ? "Por tiempo limitado" : "Sin limite de tiempo", porVencimiento.ToList());
            }
            else
            {
                grupos.Add("No disponibles", porDisponible.ToList());
            }
        }

        return BuildDescription(grupos);
    }

    /// <summary>
    /// Lista los challenges con la disponibilidad indicada, agrupados en "Por tiempo limitado" y
    /// "Sin limite de tiempo".
    /// </summary>
    public static string ChunkIfDisponibles(List<Challenge> challenges, bool disponible)
    {
        Dictionary<string, List<Challenge>> grupos = [];

        foreach (IGrouping<bool, Challenge> porVencimiento in challenges.Where(x => x.Disponible == disponible).GroupBy(x => x.Vencimiento != null))
            grupos.Add(porVencimiento.Key ? "Por tiempo limitado" : "Sin limite de tiempo", porVencimiento.ToList());

        return BuildDescription(grupos);
    }

    private static string BuildDescription(Dictionary<string, List<Challenge>> grupos)
    {
        StringBuilder sb = new();

        foreach ((string titulo, List<Challenge> challenges) in grupos)
        {
            sb.Append(Formatter.Bold($"{titulo}:")).Append('\n');

            if (challenges.Count > 0)
            {
                foreach (Challenge x in challenges)
                {
                    sb.Append($"[{x.Nombre}]({x.Link})");
                    if (x.Vencimiento is { } dt && x.Disponible)
                        sb.Append($" (hasta {Formatter.Bold($"{dt.Day}/{dt.Month}/{dt.Year}")})");
                    sb.Append('\n');
                }
            }
            else
            {
                sb.Append("(Sin registros)\n");
            }

            sb.Append('\n');
        }

        return sb.ToString();
    }
}
