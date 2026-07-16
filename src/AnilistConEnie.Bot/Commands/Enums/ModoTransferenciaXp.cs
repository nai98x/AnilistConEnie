using System.ComponentModel;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;

namespace AnilistConEnie.Bot.Commands.Enums;

public enum ModoTransferenciaXp
{
    [ChoiceDisplayName("Reemplazar")] [Description("Reemplazar")]
    Reemplazar,
    [ChoiceDisplayName("Sumar")] [Description("Sumar")]
    Sumar
}
