using AnilistConEnie.Domain.Entities;

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

        foreach (DateTime checkpoint in checkpoints)
        {
            // Avanza hasta el último registro real que no supera el checkpoint, dejando siempre uno
            // "siguiente" disponible en sorted[idx + 1] si existe (para interpolar contra él).
            while (idx < sorted.Count - 1 && sorted[idx + 1].Date.Date <= checkpoint)
                idx++;

            points.Add(new UserDailyXp { UserId = userId, Date = checkpoint, Xp = InterpolatedXp(sorted, idx, checkpoint) });
        }

        // El último punto siempre refleja el total actual en vivo, más preciso que el último
        // registro persistido (que puede tener hasta un día de rezago).
        points[^1] = new UserDailyXp { UserId = userId, Date = todayDate, Xp = currentXp };

        return points;
    }

    /// <summary>
    /// Valor de XP en <paramref name="checkpoint"/> interpolado linealmente entre el registro real en
    /// <paramref name="index"/> y el siguiente (si existe); evita los "escalones" de mantener el
    /// último valor conocido plano cuando pasan varias semanas sin un registro nuevo. Si no hay
    /// registro siguiente (checkpoint en el tramo más reciente, aún sin dato posterior) se sostiene
    /// el último valor conocido, que es lo único que se puede inferir.
    /// </summary>
    private static long InterpolatedXp(IReadOnlyList<UserDailyXp> sorted, int index, DateTime checkpoint)
    {
        UserDailyXp current = sorted[index];
        if (index + 1 >= sorted.Count) return current.Xp;

        UserDailyXp next = sorted[index + 1];
        double totalDays = (next.Date.Date - current.Date.Date).TotalDays;
        if (totalDays <= 0) return current.Xp;

        double t = Math.Clamp((checkpoint - current.Date.Date).TotalDays / totalDays, 0, 1);
        return current.Xp + (long)Math.Round((next.Xp - current.Xp) * t);
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
