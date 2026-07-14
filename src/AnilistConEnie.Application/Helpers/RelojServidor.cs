namespace AnilistConEnie.Application.Helpers;

/// <summary>
/// Reloj de negocio del bot: todos los cortes de día (actividad, resets, cumpleaños, vencimientos,
/// crons) se rigen por el horario de Argentina, sin depender del timezone del OS donde corre.
/// Para instantes absolutos (cooldowns, comparaciones contra timestamps de Discord/AniList) usar UTC.
/// </summary>
public static class RelojServidor
{
    private static readonly TimeZoneInfo Zona = TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires");

    public static DateTime Ahora => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zona);

    public static DateTime Hoy => Ahora.Date;

    /// <summary>Convierte un instante UTC a la hora de Argentina, con su offset.</summary>
    public static DateTimeOffset EnHoraLocal(DateTime utc) =>
        TimeZoneInfo.ConvertTime(new DateTimeOffset(DateTime.SpecifyKind(utc, DateTimeKind.Utc)), Zona);
}
