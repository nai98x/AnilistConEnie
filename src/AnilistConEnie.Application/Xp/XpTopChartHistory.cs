using AnilistConEnie.Model.Entities;

namespace AnilistConEnie.Application.Xp;

/// <summary>
/// Arma el historial semanal (1 punto cada 7 días) de los últimos 365 días para el comparativo de
/// "/topchart". Si el usuario tiene menos de 365 días de historial, arranca desde su primer registro
/// en vez de rellenar con ceros previos a su ingreso al servidor.
/// </summary>
public static class XpTopChartHistory
{
    private const int WindowDays = 365;
    private const int StepDays = 7;

    public static List<UserDailyXp> Build(long userId, IReadOnlyList<UserDailyXp> history, DateTime today, long currentXp)
    {
        DateTime todayDate = today.Date;

        if (history.Count == 0)
            return [new UserDailyXp { UserId = userId, Date = todayDate, Xp = currentXp }];

        List<UserDailyXp> sorted = [.. history.OrderBy(x => x.Date)];
        DateTime firstRecord = sorted[0].Date.Date;
        DateTime earliestWindow = todayDate.AddDays(-WindowDays);
        DateTime windowStart = firstRecord > earliestWindow ? firstRecord : earliestWindow;

        List<DateTime> checkpoints = [];
        for (DateTime day = todayDate; day >= windowStart; day = day.AddDays(-StepDays))
            checkpoints.Add(day);
        checkpoints.Reverse();

        List<UserDailyXp> points = [];
        int idx = 0;
        long lastXp = 0;

        foreach (DateTime checkpoint in checkpoints)
        {
            while (idx < sorted.Count && sorted[idx].Date.Date <= checkpoint)
            {
                lastXp = sorted[idx].Xp;
                idx++;
            }

            points.Add(new UserDailyXp { UserId = userId, Date = checkpoint, Xp = lastXp });
        }

        // El último punto siempre refleja el total actual en vivo, más preciso que el último
        // registro persistido (que puede tener hasta un día de rezago).
        points[^1] = new UserDailyXp { UserId = userId, Date = todayDate, Xp = currentXp };

        return points;
    }

    /// <summary>
    /// Alinea el historial de un usuario a una grilla de <paramref name="gridLength"/> puntos,
    /// rellenando con <see cref="double.NaN"/> las semanas anteriores a su primer registro. Como
    /// <see cref="Build"/> siempre arranca la cuenta regresiva desde el mismo "hoy" en pasos de
    /// <see cref="StepDays"/>, un historial más corto es exactamente la cola (más reciente) de uno
    /// más largo, así que alinear "a la derecha" reconstruye la fecha real de cada punto.
    /// </summary>
    public static double[] AlignToGrid(IReadOnlyList<UserDailyXp> points, int gridLength)
    {
        double[] result = new double[gridLength];
        int offset = gridLength - points.Count;

        for (int i = 0; i < gridLength; i++)
            result[i] = i < offset ? double.NaN : points[i - offset].Xp;

        return result;
    }
}
