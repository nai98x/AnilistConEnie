namespace AnilistConEnie.Application.Fun;

public sealed record TrueLoveResult(
    ulong? MatchId,
    int MaxPorcentaje,
    IReadOnlyList<(ulong Id, int Porcentaje)> Pretendientes,
    IReadOnlyList<(ulong Id, int Porcentaje)> Odiados);

public static class TrueLoveCalculator
{
    // Afinidad determinística entre dos usuarios: misma pareja → mismo porcentaje siempre.
    public static int Afinidad(ulong targetId, ulong candidateId) =>
        new Random((int)(targetId + candidateId)).Next(0, 101);

    // Calcula el match (mayor afinidad, primero en orden ante empate), el top 5 de pretendientes y el
    // top 5 de odiados. El orden de candidatos define el desempate; se respeta el que venga por parámetro.
    public static TrueLoveResult Calcular(ulong targetId, IEnumerable<ulong> candidateIds)
    {
        int maxPorcentaje = 0;
        ulong? matchId = null;
        List<(ulong Id, int Porcentaje)> amorios = [];

        foreach (ulong candidateId in candidateIds)
        {
            int porcentaje = Afinidad(targetId, candidateId);
            amorios.Add((candidateId, porcentaje));

            if (porcentaje > maxPorcentaje)
            {
                maxPorcentaje = porcentaje;
                matchId = candidateId;
            }
        }

        List<(ulong Id, int Porcentaje)> pretendientes = amorios.OrderByDescending(x => x.Porcentaje).Take(5).ToList();
        List<(ulong Id, int Porcentaje)> odiados = amorios.OrderBy(x => x.Porcentaje).Take(5).ToList();

        return new TrueLoveResult(matchId, maxPorcentaje, pretendientes, odiados);
    }
}
