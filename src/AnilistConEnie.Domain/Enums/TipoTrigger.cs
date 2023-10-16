using Discord.Interactions;

namespace AnilistConEnie.Domain.Enums
{
    public enum TipoTrigger
    {
        [ChoiceDisplay("Texto exacto")]
        TEXTO_EXACTO,
        [ChoiceDisplay("Termina en")]
        TERMINA_EN,
        [ChoiceDisplay("Empieza con")]
        EMPIEZA_CON,
        [ChoiceDisplay("Libre")]
        LIBRE,
    }
}
