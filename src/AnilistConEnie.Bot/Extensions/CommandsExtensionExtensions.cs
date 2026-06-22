using System.Reflection;
using AnilistConEnie.Bot.Commands.SlashCommands.Attributes;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Trees.Metadata;

namespace AnilistConEnie.Bot.Extensions;

public static class CommandsExtensionExtensions
{
    private const string SlashCommandsNamespace = "AnilistConEnie.Bot.Commands.SlashCommands";

    /// <summary>
    /// Descubre por reflexión todas las clases de slash command del namespace
    /// <see cref="SlashCommandsNamespace"/> y las registra en el guild indicado.
    /// En RELEASE registra todas; en DEBUG solo las marcadas con <see cref="TestCommandAttribute"/>.
    /// Una clase nueva queda registrada automáticamente, sin tocar este método.
    /// </summary>
    public static void AddDiscoveredSlashCommands(this CommandsExtension extension, ulong guildId)
    {
        IEnumerable<Type> commandTypes = typeof(TestCommandAttribute).Assembly
            .GetTypes()
            .Where(type => type.Namespace == SlashCommandsNamespace
                           && type is { IsClass: true, IsAbstract: false }
                           && type.GetCustomAttribute<CommandAttribute>() is not null);

#if DEBUG
        commandTypes = commandTypes.Where(type => type.GetCustomAttribute<TestCommandAttribute>() is not null);
#endif

        extension.AddCommands(commandTypes.ToArray(), guildId);
    }
}
