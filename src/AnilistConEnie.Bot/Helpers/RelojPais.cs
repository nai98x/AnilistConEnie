using AnilistConEnie.Application.Helpers;
using AnilistConEnie.Bot.Configuration;
using DSharpPlus.Entities;

namespace AnilistConEnie.Bot.Helpers;

/// <summary>Fecha de negocio según el rol de país del miembro; sin rol de país rige el reloj del servidor.</summary>
public class RelojPais(BotConfiguration config, DiscordLogService logService)
{
    public async Task<DateTime> HoyDe(DiscordGuild guild, DiscordMember member)
    {
        BotConfiguration.PaisTimezoneConfiguration? pais =
            config.PaisTimezones.FirstOrDefault(x => member.Roles.Any(y => y.Id == x.RoleId));
        if (pais is null) return RelojServidor.Hoy;

        try
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(pais.Timezone)).Date;
        }
        catch (TimeZoneNotFoundException)
        {
            await logService.GrabarLogGeneralWarning(guild,
                $"Timezone `{pais.Timezone}` inválido en Ids:PaisTimezones para el rol <@&{pais.RoleId}> (miembro {member.Mention}). Se usa el horario del servidor.");
            return RelojServidor.Hoy;
        }
    }
}
