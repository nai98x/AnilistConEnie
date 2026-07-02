using AnilistConEnie.Application.Xp;

namespace AnilistConEnie.Application.Charts;

/// <summary>
/// Paleta de colores centralizada para los gráficos de XP. Reemplaza los colores hardcodeados que
/// antes estaban dispersos en los comandos y el color aleatorio por ejecución de "toppaises".
/// </summary>
public static class ChartColors
{
    /// <summary>Ciclo de acento para series/porciones sin categoría propia (top de usuarios, países, etc).</summary>
    private static readonly IReadOnlyList<string> Accent =
    [
        "#5865F2", "#EB459E", "#57F287", "#FEE75C", "#ED4245", "#3BA55D"
    ];

    /// <summary>Color determinístico y estable de la paleta de acento según la posición.</summary>
    public static string ForIndex(int index) => Accent[index % Accent.Count];

    /// <summary>Etiqueta, color y motivo (para el detalle en texto) de cada categoría de XP.</summary>
    public static (string Label, string Color, string Motivo) ForXpCategory(XpCategory category) => category switch
    {
        XpCategory.Mensajes => ("Mensajes", "#FF6384", "mensajes"),
        XpCategory.Challenges => ("Challenges", "#36A2EB", "challenges"),
        XpCategory.Eventos => ("Eventos y actividades", "#23C46C", "eventos y actividades"),
        XpCategory.Intercambios => ("Intercambios", "#C4BA23", "intercambios"),
        _ => ("Otros", "#8C23C4", "otros motivos")
    };
}
