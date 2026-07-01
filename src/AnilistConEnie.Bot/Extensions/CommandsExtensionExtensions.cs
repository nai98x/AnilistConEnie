using System.Reflection;
using AnilistConEnie.Bot.Commands.Slash.Attributes;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Trees.Metadata;

namespace AnilistConEnie.Bot.Extensions;

public static class CommandsExtensionExtensions
{
    private const string SlashCommandsNamespace = "AnilistConEnie.Bot.Commands.Slash";
    private const string TextCommandsNamespace = "AnilistConEnie.Bot.Commands.Text";

    /// <summary>
    /// Descubre por reflexión todas las clases de slash command del namespace
    /// <see cref="SlashCommandsNamespace"/> y las registra en el guild indicado.
    /// En RELEASE registra todas; en DEBUG solo las marcadas con <see cref="TestCommandAttribute"/>.
    /// Una clase nueva queda registrada automáticamente, sin tocar este método.
    /// </summary>
    public static void AddDiscoveredSlashCommands(this CommandsExtension extension, ulong guildId)
    {
        IEnumerable<Type> commandTypes = DiscoverCommandTypes(SlashCommandsNamespace);
        extension.AddCommands(commandTypes.ToArray(), guildId);
    }

    /// <summary>
    /// Descubre por reflexión todas las clases de text command del namespace
    /// <see cref="TextCommandsNamespace"/> y las registra (sin scope de guild: los comandos de texto
    /// no se registran como interacción, se resuelven por prefijo/mención al llegar el mensaje).
    /// Misma convención DEBUG/RELEASE que <see cref="AddDiscoveredSlashCommands"/>.
    /// </summary>
    public static void AddDiscoveredTextCommands(this CommandsExtension extension)
    {
        IEnumerable<Type> commandTypes = DiscoverCommandTypes(TextCommandsNamespace);
        extension.AddCommands(commandTypes.ToArray());
    }

    private static IEnumerable<Type> DiscoverCommandTypes(string ns)
    {
        IEnumerable<Type> commandTypes = typeof(TestCommandAttribute).Assembly
            .GetTypes()
            .Where(type => type.Namespace == ns
                           && type is { IsClass: true, IsAbstract: false }
                           && (type.GetCustomAttribute<CommandAttribute>() is not null
                               || type.GetMethods().Any(method => method.GetCustomAttribute<CommandAttribute>() is not null)));

#if DEBUG
        commandTypes = commandTypes.Where(type => type.GetCustomAttribute<TestCommandAttribute>() is not null);
#endif

        return commandTypes;
    }
}
