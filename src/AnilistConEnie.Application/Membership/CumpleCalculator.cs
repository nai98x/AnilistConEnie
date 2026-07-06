using AnilistConEnie.Model.Entities;

namespace AnilistConEnie.Application.Membership;

/// <summary>
/// Construye las vistas de cumpleaños (los que cumplen el día indicado y los próximos) a partir del
/// día/mes registrado de cada usuario. Sin año real: <see cref="UserCumple.FechaOriginal"/> usa un año
/// fijo solo para mostrar el día de la semana, replicando el comportamiento previo.
/// </summary>
public static class CumpleCalculator
{
    private const int AnioDisplay = 2023;

    /// <summary>Día/mes registrable como cumpleaños. El 29/2 se acepta usando un año bisiesto de referencia.</summary>
    public static bool EsFechaValida(int dia, int mes) =>
        mes is >= 1 and <= 12 && dia >= 1 && dia <= DateTime.DaysInMonth(2024, mes);

    public static List<UserCumple> DelDia(IReadOnlyList<Usuario> cumples, DateTime dia) =>
        cumples
            .Where(x => x.CumpleDia == dia.Day && x.CumpleMes == dia.Month)
            .Select(x => Build(x, dia))
            .ToList();

    public static List<UserCumple> Proximos(IReadOnlyList<Usuario> cumples, DateTime now, bool soloMes)
    {
        List<UserCumple> lista = [];
        foreach (Usuario x in cumples)
        {
            int dia = x.CumpleDia!.Value;
            int mes = x.CumpleMes!.Value;
            if (!EsFechaValida(dia, mes)) continue;

            DateTime esteAnio = Fecha(now.Year, mes, dia);

            if (soloMes && (esteAnio < now || esteAnio > now.AddMonths(1)))
                continue;

            lista.Add(Build(x, now));
        }

        lista.Sort((a, b) => a.BirthdayActual.CompareTo(b.BirthdayActual));
        return lista;
    }

    private static UserCumple Build(Usuario x, DateTime referencia)
    {
        int dia = x.CumpleDia!.Value;
        int mes = x.CumpleMes!.Value;
        DateTime esteAnio = Fecha(referencia.Year, mes, dia);
        DateTime proximo = referencia > esteAnio ? Fecha(referencia.Year + 1, mes, dia) : esteAnio;

        return new UserCumple
        {
            Id = x.UserId,
            Birthday = esteAnio,
            BirthdayActual = proximo,
            MostrarYear = false,
            FechaOriginal = Fecha(AnioDisplay, mes, dia)
        };
    }

    /// <summary>El 29/2 en años no bisiestos se corre al 28/2.</summary>
    private static DateTime Fecha(int anio, int mes, int dia) =>
        new(anio, mes, Math.Min(dia, DateTime.DaysInMonth(anio, mes)));
}
