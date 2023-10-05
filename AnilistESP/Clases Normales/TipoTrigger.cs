namespace AnilistESP
{
    using DSharpPlus.SlashCommands;

    public enum TipoTrigger
    {
        [ChoiceName("Texto exacto")]
        TEXTO_EXACTO,
        [ChoiceName("Termina en")]
        TERMINA_EN,
        [ChoiceName("Empieza con")]
        EMPIEZA_CON,
        [ChoiceName("Libre")]
        LIBRE,
    }
}
