using System.Globalization;
using AnilistConEnie.Bot.Configuration;
using AnilistConEnie.Model.Entities;
using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.MessageCommands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.TextCommands;
using DSharpPlus.Commands.Processors.UserCommands;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Microsoft.Extensions.Logging;

namespace AnilistConEnie.Bot.Helpers;

/// <summary>Escritura de mensajes a los canales de log/moderación del servidor.</summary>
public class DiscordLogService(BotConfiguration config, ILogger<DiscordLogService> logger)
{
    public async Task BorrarMensajeUsuarioAnilist(DiscordGuild guild, long oldMessageId)
    {
        DiscordChannel canalPerfiles = guild.Channels[config.Channels.Perfiles];
        try
        {
            DiscordMessage mensaje = await canalPerfiles.GetMessageAsync((ulong)oldMessageId);
            await mensaje.DeleteAsync();
        }
        catch (NotFoundException)
        {
            logger.LogWarning("No se ha podido borrar el mensaje {OldMessageId} del canal de perfiles de Anilist porque no se ha encontrado", oldMessageId);
        }
    }

    public async Task GrabarLogUsuarioOutAnilist(DiscordGuild guild, DiscordMember member, UsuarioAnilist userAnilist, UserXp rank)
    {
        try
        {
            NumberFormatInfo nfi = new CultureInfo("es-ES", false).NumberFormat;
            nfi.NumberDecimalSeparator = ",";
            nfi.NumberGroupSeparator = ".";
            nfi.NumberDecimalDigits = 0;
            DiscordChannel channelPuerta = guild.Channels[config.Channels.LogChannelPuerta];

            await channelPuerta.SendMessageAsync(new DiscordEmbedBuilder
            {
                Title = "Un miembro se ha ido del servidor",
                Author = new DiscordEmbedBuilder.EmbedAuthor()
                {
                    IconUrl = member.AvatarUrl,
                    Name = member.DisplayName
                },
                Description =
                    $"{member.Mention}\n" +
                    $"- Entró el {Formatter.Timestamp(member.JoinedAt, TimestampFormat.LongDate)}\n" +
                    $"- Su {Formatter.MaskedUrl("perfil", new Uri(userAnilist.AnilistURL))} de AniList\n" +
                    $"- XP: {rank.Total.ToString("N", nfi)}\n" +
                    $"- Roles: {string.Join(" ", member.Roles.Select(x => x.Mention))}",
                Footer = new DiscordEmbedBuilder.EmbedFooter()
                {
                    Text = $"ID: {member.Id}"
                },
                Color = DiscordColor.Red
            });
        }
        catch (Exception ex)
        {
            await GrabarLogGeneralError(guild, $"Error al grabar log de usuario que se fue de AniList: {ex.Message}\n\n{Formatter.BlockCode(ex.StackTrace ?? string.Empty)}");
        }
    }

    public async Task GrabarLogGeneralError(DiscordGuild guild, string descripcion)
    {
        logger.LogError("Error no controlado: {Descripcion}", descripcion);
        try
        {
            DiscordChannel channelErrores = guild.Channels[config.Channels.LogChannelError];
            await channelErrores.SendMessageAsync(new DiscordEmbedBuilder
            {
                Title = "Error no controlado",
                Description = descripcion,
                Color = DiscordColor.Red,
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No se pudo escribir en el canal de errores");
        }
    }

    public async Task GrabarLogComandoEjecutado(CommandContext ctx)
    {
        if (ctx.Guild is null) return;

        try
        {
            string tipo = ctx switch
            {
                TextCommandContext => "Texto",
                UserCommandContext => "Menú de usuario",
                MessageCommandContext => "Menú de mensaje",
                SlashCommandContext => "Slash",
                _ => "Desconocido"
            };

            string argumentos = ctx.Arguments.Count == 0
                ? "Ninguno"
                : string.Join("\n", ctx.Arguments.Select(kv => $"- **{kv.Key.Name}**: {kv.Value}"));
            
            logger.LogInformation("Comando ejecutado [{Tipo}]: {Comando} - Usuario: {Usuario} - Canal: {Canal}",
                tipo, ctx.Command.FullName, ctx.User.Username, ctx.Channel.Name);

            DiscordChannel channelInfo = ctx.Guild.Channels[config.Channels.LogChannelInfo];
            await channelInfo.SendMessageAsync(new DiscordEmbedBuilder
            {
                Title = "Comando ejecutado",
                Author = new DiscordEmbedBuilder.EmbedAuthor()
                {
                    IconUrl = ctx.User.AvatarUrl,
                    Name = ctx.User.Username
                },
                Description =
                    $"- Usuario: {ctx.User.Mention}\n" +
                    $"- Canal: {ctx.Channel.Mention}\n" +
                    $"- Tipo: {tipo}\n" +
                    $"- Comando: `{ctx.Command.FullName}`\n" +
                    $"- Argumentos: {argumentos}",
                Footer = new DiscordEmbedBuilder.EmbedFooter()
                {
                    Text = $"ID: {ctx.User.Id}"
                },
                Color = DiscordColor.Blurple
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No se pudo escribir el log de comando ejecutado");
        }
    }

    public async Task LogException(DiscordGuild guild, Exception ex, string contexto)
    {
        if (ex is NotFoundException)
        {
            logger.LogDebug("{Contexto}: entidad no encontrada ({Message})", contexto, ex.Message);
            return;
        }

        string stackTrace = ex.StackTrace ?? string.Empty;
        if (stackTrace.Length > 1500)
            stackTrace = stackTrace[..1500];

        await GrabarLogGeneralError(guild, $"{contexto}: {ex.Message}\n\n{Formatter.BlockCode(stackTrace)}");
    }
}
