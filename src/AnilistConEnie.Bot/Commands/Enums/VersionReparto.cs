using System.ComponentModel;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;

namespace AnilistConEnie.Bot.Commands.Enums;

public enum VersionReparto
{
    [ChoiceDisplayName("Clasica")] [Description("Clasica")]
    Clasica,
    [ChoiceDisplayName("GPT")] [Description("GPT")]
    GPT
}
