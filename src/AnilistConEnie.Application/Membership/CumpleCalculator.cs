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

    public static List<UserCumple> DelDia(IReadOnlyList<Usuario> cumples, DateTime dia) =>
        cumples
            .Where(x => x.CumpleDia == dia.Day && x.CumpleMes == dia.Month)
            .Select(x => Build(x, dia.Year))
            .ToList();

    public static List<UserCumple> Proximos(IReadOnlyList<Usuario> cumples, DateTime now, bool soloMes)
    {
        List<UserCumple> lista = [];
        foreach (Usuario x in cumples)
        {
            int dia = x.CumpleDia!.Value;
            int mes = x.CumpleMes!.Value;
            DateTime esteAnio = new(now.Year, mes, dia);

            if (soloMes && (esteAnio < now || esteAnio > now.AddMonths(1)))
                continue;

            lista.Add(Build(x, now.Year));
        }

        lista.Sort((a, b) => a.BirthdayActual.CompareTo(b.BirthdayActual));
        return lista;
    }

    private static UserCumple Build(Usuario x, int anioReferencia)
    {
        int dia = x.CumpleDia!.Value;
        int mes = x.CumpleMes!.Value;
        DateTime esteAnio = new(anioReferencia, mes, dia);
        DateTime proximo = DateTime.UtcNow > new DateTime(DateTime.UtcNow.Year, mes, dia)
            ? new DateTime(DateTime.UtcNow.Year + 1, mes, dia)
            : new DateTime(DateTime.UtcNow.Year, mes, dia);

        return new UserCumple
        {
            Id = x.UserId,
            Birthday = esteAnio,
            BirthdayActual = proximo,
            MostrarYear = false,
            FechaOriginal = new DateTime(AnioDisplay, mes, dia)
        };
    }
}
