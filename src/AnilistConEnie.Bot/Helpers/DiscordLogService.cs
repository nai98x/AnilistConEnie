using System.Globalization;
using AnilistConEnie.Bot.Configuration;
using AnilistConEnie.Model.Entities;
using DSharpPlus;
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
        DiscordChannel channelErrores = guild.Channels[config.Channels.LogChannelError];
        await channelErrores.SendMessageAsync(new DiscordEmbedBuilder
        {
            Title = "Error no controlado",
            Description = descripcion,
            Color = DiscordColor.Red,
        });
        logger.LogError("Error no controlado: {Descripcion}", descripcion);
    }
}
