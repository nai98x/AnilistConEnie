using System.ComponentModel;

namespace AnilistConEnie.Domain.Enum;

public enum TipoTrigger
{
    [Description("Texto exacto")]
    TEXTO_EXACTO,
    [Description("Termina en")]
    TERMINA_EN,
    [Description("Empieza con")]
    EMPIEZA_CON,
    [Description("Libre")]
    LIBRE,
}
