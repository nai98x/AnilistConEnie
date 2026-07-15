namespace AnilistConEnie.Application.Membership;

/// <summary>
/// Reglas del rol de inactivo: solo lo llevan los miembros con antigüedad suficiente que no
/// registraron actividad dentro de la ventana.
/// </summary>
public static class InactividadCalculator
{
    /// <summary>
    /// ¿Corresponde el rol de inactivo? Un miembro que entró hace menos que la ventana todavía está
    /// en período de gracia, aunque no haya escrito nunca.
    /// </summary>
    public static bool CorrespondeRolInactivo(DateTimeOffset fechaEntrada, bool tieneActividadReciente, DateTimeOffset ahora, int meses) =>
        !tieneActividadReciente && fechaEntrada <= ahora.AddMonths(-meses);
}
