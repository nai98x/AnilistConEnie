namespace AnilistConEnie.Application.Membership;

/// <summary>
/// Cálculo de aniversarios de un miembro en el servidor a partir de su fecha de entrada.
/// </summary>
public static class AniversarioCalculator
{
    /// <summary>
    /// ¿La fecha de entrada cae en un aniversario (mismo día y hora actual) en este momento? Se consideran
    /// los aniversarios desde hace 1 año hasta la antigüedad del servidor.
    /// </summary>
    public static bool EsAniversarioAhora(DateTimeOffset fechaEntrada, DateTime now, int guildCreationYear)
    {
        int yearsGuildExists = now.Year - guildCreationYear;

        for (int i = 1; i <= yearsGuildExists; i++)
        {
            DateTime aniversaryDate = now.AddYears(-i).Date;
            if (fechaEntrada.Date == aniversaryDate && fechaEntrada.Hour == now.Hour)
                return true;
        }

        return false;
    }

    /// <summary>Años cumplidos en el servidor a la fecha indicada.</summary>
    public static int AniosEnServidor(DateTimeOffset fechaEntrada, DateTime now)
    {
        int years = now.Year - fechaEntrada.Year;

        if (fechaEntrada.Month > now.Month || (fechaEntrada.Month == now.Month && fechaEntrada.Day > now.Day))
            years--;

        return years;
    }
}
