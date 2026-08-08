using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace AnilistConEnie.Application.Xp;

/// <summary>XP asignada a un usuario dentro de un reparto. Negativa si se le quita.</summary>
public readonly record struct AsignacionXp(ulong UserId, long Xp);

/// <summary>Resultado de interpretar el texto cargado por el staff: lo válido y lo que no se entendió.</summary>
public sealed record RepartoParseado(IReadOnlyList<AsignacionXp> Asignaciones, IReadOnlyList<string> Errores);

/// <summary>
/// Traduce entre el reparto de XP y sus dos representaciones de texto: la que el staff edita en el modal
/// (<c>Nombre = 500</c>) y la que se publica en el mensaje de aprobación (<c>&lt;@id&gt; · +500</c>).
/// El mensaje publicado es la única fuente de verdad del reparto pendiente, por eso tiene que poder
/// reinterpretarse con <see cref="ParseMensaje"/>.
/// </summary>
public static partial class RepartoXp
{
    private const string Separadores = "=:";

    /// <summary>
    /// Interpreta el texto del modal contra los usuarios que se habían elegido. Las líneas en cero y las
    /// vacías se descartan sin ruido (es la forma de dejar a alguien afuera del reparto).
    /// </summary>
    public static RepartoParseado Parse(string texto, IReadOnlyDictionary<ulong, string> nombresPorId)
    {
        Dictionary<string, List<ulong>> idsPorNombre = [];
        foreach ((ulong id, string nombre) in nombresPorId)
        {
            string clave = Normalizar(nombre);
            if (!idsPorNombre.TryGetValue(clave, out List<ulong>? ids))
                idsPorNombre[clave] = ids = [];
            ids.Add(id);
        }

        List<AsignacionXp> asignaciones = [];
        List<string> errores = [];

        foreach (string linea in (texto ?? string.Empty).Split('\n'))
        {
            string actual = linea.Trim();
            if (actual.Length == 0) continue;

            int corte = actual.LastIndexOfAny(Separadores.ToCharArray());
            if (corte <= 0 || corte == actual.Length - 1)
            {
                errores.Add($"No se entiende la línea `{actual}`. El formato es `Nombre = 500`.");
                continue;
            }

            string nombre = actual[..corte].Trim();
            if (!TryParseXp(actual[(corte + 1)..], out long xp))
            {
                errores.Add($"`{actual[(corte + 1)..].Trim()}` no es un número válido (línea de `{nombre}`).");
                continue;
            }

            if (!idsPorNombre.TryGetValue(Normalizar(nombre), out List<ulong>? candidatos))
            {
                errores.Add($"`{nombre}` no está entre los usuarios que elegiste.");
                continue;
            }

            if (candidatos.Count > 1)
            {
                errores.Add($"Hay más de un usuario elegido que se llama `{nombre}`, no se puede saber a cuál va la XP.");
                continue;
            }

            ulong userId = candidatos[0];
            if (asignaciones.Any(a => a.UserId == userId))
            {
                errores.Add($"`{nombre}` aparece más de una vez.");
                continue;
            }

            if (xp != 0)
                asignaciones.Add(new AsignacionXp(userId, xp));
        }

        if (asignaciones.Count == 0 && errores.Count == 0)
            errores.Add("No cargaste XP para ningún usuario.");

        return new RepartoParseado(asignaciones, errores);
    }

    /// <summary>Líneas del mensaje publicado. El formato tiene que seguir siendo legible por <see cref="ParseMensaje"/>.</summary>
    public static string Renderizar(IEnumerable<AsignacionXp> asignaciones) =>
        string.Join("\n", asignaciones.Select(a => $"<@{a.UserId}> · {(a.Xp >= 0 ? "+" : "-")}{Math.Abs(a.Xp)} XP"));

    /// <summary>Reconstruye el reparto desde el mensaje publicado, para poder resolverlo tras un reinicio del bot.</summary>
    public static IReadOnlyList<AsignacionXp> ParseMensaje(string contenido)
    {
        List<AsignacionXp> asignaciones = [];
        foreach (Match match in LineaPublicada().Matches(contenido ?? string.Empty))
        {
            if (ulong.TryParse(match.Groups[1].Value, out ulong userId)
                && long.TryParse(match.Groups[2].Value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long xp)
                && asignaciones.All(a => a.UserId != userId))
                asignaciones.Add(new AsignacionXp(userId, xp));
        }
        return asignaciones;
    }

    /// <summary>Texto para reabrir el modal de edición con lo ya cargado.</summary>
    public static string Prellenar(IEnumerable<AsignacionXp> asignaciones, IReadOnlyDictionary<ulong, string> nombresPorId) =>
        string.Join("\n", asignaciones.Select(a =>
            $"{LimpiarNombre(nombresPorId.TryGetValue(a.UserId, out string? nombre) ? nombre : a.UserId.ToString())} = {a.Xp}"));

    /// <summary>Interpreta un valor suelto de XP (el de una celda del modal): acepta signo y separadores de miles.</summary>
    public static bool TryParseXp(string valor, out long xp)
    {
        string limpio = new(valor.Where(c => !char.IsWhiteSpace(c) && c != '.' && c != ',' && c != '_').ToArray());
        return long.TryParse(limpio, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out xp);
    }

    /// <summary>Saca del nombre lo que rompería el formato `Nombre = 500` al prellenar el modal.</summary>
    private static string LimpiarNombre(string nombre)
    {
        string limpio = new(nombre.Where(c => !Separadores.Contains(c) && c != '\n' && c != '\r').ToArray());
        limpio = EspaciosRepetidos().Replace(limpio, " ").Trim();
        return limpio.Length == 0 ? "?" : limpio;
    }

    private static string Normalizar(string nombre)
    {
        string sinDiacriticos = new(LimpiarNombre(nombre).Normalize(NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .ToArray());
        return sinDiacriticos.Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }

    [GeneratedRegex(@"<@!?(\d+)>\s*·\s*([+-]?\d+)")]
    private static partial Regex LineaPublicada();

    [GeneratedRegex(@"\s+")]
    private static partial Regex EspaciosRepetidos();
}
