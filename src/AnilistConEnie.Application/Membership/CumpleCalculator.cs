using AnilistConEnie.Domain.Entities;

namespace AnilistConEnie.Application.Membership;

/// <summary>
/// Construye las vistas de cumpleaños (los que cumplen el día indicado y los próximos) a partir del
/// día/mes registrado de cada usuario. No hay año de nacimiento: la fecha se proyecta sobre el próximo
/// festejo (<see cref="UserCumple.Proximo"/>).
/// </summary>
public static class CumpleCalculator
{
    /// <summary>Día/mes registrable como cumpleaños. El 29/2 se acepta usando un año bisiesto de referencia.</summary>
    public static bool EsFechaValida(int dia, int mes) =>
        mes is >= 1 and <= 12 && dia >= 1 && dia <= DateTime.DaysInMonth(2024, mes);

    public static bool EsDelDia(Usuario usuario, DateTime dia) =>
        usuario.CumpleDia == dia.Day && usuario.CumpleMes == dia.Month;

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

            lista.Add(new UserCumple
            {
                Id = x.UserId,
                Proximo = now > esteAnio ? Fecha(now.Year + 1, mes, dia) : esteAnio
            });
        }

        lista.Sort((a, b) => a.Proximo.CompareTo(b.Proximo));
        return lista;
    }

    /// <summary>El 29/2 en años no bisiestos se corre al 28/2.</summary>
    private static DateTime Fecha(int anio, int mes, int dia) =>
        new(anio, mes, Math.Min(dia, DateTime.DaysInMonth(anio, mes)));
}
