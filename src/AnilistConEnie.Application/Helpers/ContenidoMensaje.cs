namespace AnilistConEnie.Application.Helpers;

public static class ContenidoMensaje
{
    /// <summary>
    /// Un mensaje que es un único token <c>&lt;...&gt;</c> (emote custom o mención sola) no cuenta como
    /// actividad: ni da XP ni suma al conteo de mensajes.
    /// </summary>
    public static bool CuentaComoActividad(string contenido) =>
        !(contenido.StartsWith('<') && contenido.EndsWith('>') && contenido.Split(' ').Length == 1);
}
