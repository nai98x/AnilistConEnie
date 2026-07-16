using System.ComponentModel;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;

namespace AnilistConEnie.Bot.Commands.Framework.Choices;

public enum VersionReparto
{
    [ChoiceDisplayName("Clasica")] [Description("Clasica")]
    Clasica,
    [ChoiceDisplayName("GPT")] [Description("GPT")]
    GPT
}
