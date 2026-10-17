using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using SkiaSharp;

namespace AnilistConEnie.Infrastructure.Charts;

/// <summary>
/// Texto con fallback de fuente por carácter. Inter no cubre símbolos, emojis ni letras "decorativas"
/// Unicode (✨, 𝓐, 𝔸) frecuentes en los nicknames de Discord: cada tramo se dibuja con la primera
/// fuente que tenga el glifo, entre Inter y las instaladas en el sistema (ver <c>deploy/README.md</c>).
/// </summary>
internal static class ChartText
{
    public static readonly SKTypeface Primary =
        ChartTheme.FontPath is not null ? SKTypeface.FromFile(ChartTheme.FontPath) : SKTypeface.Default;

    /// <summary>
    /// Familias del sistema que se prueban antes que el resto. Emoji va primero para que lo que
    /// también tiene versión de contorno en Symbols/Math (⭐, ❤) salga a color.
    /// </summary>
    private static readonly string[] PreferredFamilies =
    [
        "Noto Color Emoji",
        "Noto Sans Symbols 2",
        "Noto Sans Symbols",
        "Noto Sans Math"
    ];

    private static readonly ConcurrentDictionary<int, SKTypeface> Matches = new();

    // El build de Skia sin fontconfig no implementa MatchCharacter: se recorren las familias instaladas.
    private static readonly Lazy<SKTypeface[]> SystemTypefaces = new(() =>
    [
        .. SKFontManager.Default.GetFontFamilies()
            .OrderBy(family => Array.IndexOf(PreferredFamilies, family) is var i and >= 0 ? i : PreferredFamilies.Length)
            .ThenBy(family => family, StringComparer.Ordinal)
            .Select(family => SKFontManager.Default.MatchFamily(family))
            .OfType<SKTypeface>()
    ]);

    public static SKFontMetrics Metrics(float size)
    {
        using SKFont font = new(Primary, size);
        return font.Metrics;
    }

    public static float Measure(string text, float size)
    {
        float width = 0;
        foreach ((string run, SKTypeface typeface) in Runs(text))
        {
            using SKFont font = new(typeface, size);
            width += font.MeasureText(run);
        }

        return width;
    }

    public static void Draw(SKCanvas canvas, string text, float x, float baseline, float size, SKPaint paint)
    {
        foreach ((string run, SKTypeface typeface) in Runs(text))
        {
            using SKFont font = new(typeface, size);
            canvas.DrawText(run, x, baseline, SKTextAlign.Left, font, paint);
            x += font.MeasureText(run);
        }
    }

    /// <summary>Recorta el texto (por grafema, sin partir emojis) agregando "…" si no entra en <paramref name="maxWidth"/>.</summary>
    public static string Ellipsize(string text, float size, float maxWidth)
    {
        if (Measure(text, size) <= maxWidth) return text;

        int[] boundaries = StringInfo.ParseCombiningCharacters(text);
        for (int i = boundaries.Length - 1; i > 0; i--)
        {
            string candidate = text[..boundaries[i]].TrimEnd() + "…";
            if (Measure(candidate, size) <= maxWidth) return candidate;
        }

        return "…";
    }

    private static IEnumerable<(string Text, SKTypeface Typeface)> Runs(string text)
    {
        StringBuilder run = new();
        SKTypeface? current = null;

        TextElementEnumerator elements = StringInfo.GetTextElementEnumerator(text);
        while (elements.MoveNext())
        {
            string element = elements.GetTextElement();
            Rune.DecodeFromUtf16(element, out Rune rune, out _);
            bool emojiPresentation = element.Contains('\uFE0F');
            SKTypeface typeface = TypefaceFor(rune.Value, emojiPresentation);

            if (current is not null && typeface != current)
            {
                yield return (run.ToString(), current);
                run.Clear();
            }

            current = typeface;
            // Sin shaping el selector se dibujaría como un glifo vacío con ancho propio.
            run.Append(emojiPresentation ? element.Replace("\uFE0F", "") : element);
        }

        if (current is not null) yield return (run.ToString(), current);
    }

    /// <summary>
    /// Con el selector de variación U+FE0F (ej: "❤️") el carácter pide su versión emoji, así que se
    /// saltea Inter aunque tenga el glifo en versión texto.
    /// </summary>
    private static SKTypeface TypefaceFor(int codepoint, bool emojiPresentation)
    {
        if (!emojiPresentation && Primary.ContainsGlyph(codepoint)) return Primary;

        return Matches.GetOrAdd(codepoint, c =>
            SystemTypefaces.Value.FirstOrDefault(t => t.ContainsGlyph(c)) ?? Primary);
    }
}
