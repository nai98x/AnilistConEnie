using AnilistConEnie.Application.Xp;

namespace AnilistConEnie.Application.Charts;

/// <summary>
/// Paleta de colores centralizada de los gráficos, ajustada al fondo oscuro de los embeds de Discord
/// (#2B2D31). El orden de la paleta es parte de su accesibilidad: está elegido para que cada color sea
/// distinguible del siguiente también con daltonismo (protanopia/deuteranopia), así que no reordenar
/// ni reemplazar colores sueltos sin volver a validar el conjunto.
/// </summary>
public static class ChartColors
{
    private static readonly IReadOnlyList<string> Categorical =
    [
        "#3987E5", "#D95926", "#199E70", "#C98500", "#D55181", "#119611", "#9085E9", "#E66767"
    ];

    /// <summary>
    /// Máximo de porciones con color propio en una torta. Con 7 o más, la última queda pegada a la
    /// primera (violeta contra azul) y se confunden; el resto se agrupa en "Otros".
    /// </summary>
    public const int MaxPorcionesTorta = 6;

    /// <summary>Gris neutro para la categoría "Otros": no compite con los colores de las series.</summary>
    public const string Otros = "#A6A9B1";

    /// <summary>Cantidad de colores distintos disponibles para series.</summary>
    public static int Count => Categorical.Count;

    /// <summary>
    /// Color determinístico de la paleta según la posición. No se repiten colores: más series que
    /// colores tiene que agruparse en "Otros" antes de llegar acá.
    /// </summary>
    public static string ForIndex(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Categorical.Count);
        return Categorical[index];
    }

    /// <summary>Etiqueta, color y motivo (para el detalle en texto) de cada categoría de XP.</summary>
    public static (string Label, string Color, string Motivo) ForXpCategory(XpCategory category) => category switch
    {
        XpCategory.Mensajes => ("Mensajes", Categorical[0], "mensajes"),
        XpCategory.Challenges => ("Challenges", Categorical[1], "challenges"),
        XpCategory.Eventos => ("Eventos y actividades", Categorical[2], "eventos y actividades"),
        XpCategory.Intercambios => ("Intercambios", Categorical[3], "intercambios"),
        XpCategory.Booster => ("Extra por boosting", Categorical[4], "boostear el servidor"),
        _ => ("Otros", Otros, "otros motivos")
    };
}
