using System.ComponentModel;

namespace AnilistConEnie.Domain.Enum;

public enum TipoXp
{
    [Description("Total")]
    Total,
    [Description("Intercambios")]
    Intercambios,
    [Description("Eventos y actividades")]
    Eventos,
    [Description("Challenges")]
    Challenges,
    [Description("Booster")]
    Booster,
    [Description("Otros")]
    Otros
}