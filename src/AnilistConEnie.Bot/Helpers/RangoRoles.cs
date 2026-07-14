using AnilistConEnie.Application.Xp;
using AnilistConEnie.Bot.Configuration;
using AnilistConEnie.Model.Enum;
using DSharpPlus.Entities;

namespace AnilistConEnie.Bot.Helpers;

/// <summary>Resolución de rangos y roles del servidor a partir de la configuración (seam dominio &lt;-&gt; Discord).</summary>
public class RangoRoles(BotConfiguration config)
{
    public bool RangoAPartirDe(DiscordGuild guild, DiscordMember member, RangoEnum rango, bool verificarActivo)
    {
        if (verificarActivo && member.Roles.Any(x => x.Id ==  config.Roles.Inactivo))
            return false;

        if (rango < RangoEnum.Tama) return true;

        HashSet<ulong> aceptados = RangoJerarquia.IgualOSuperior(rango).Select(RoleIdForRango).ToHashSet();
        return member.Roles.Any(x => aceptados.Contains(x.Id));
    }

    /// <summary>Devuelve el rol de país del miembro (según los timezones configurados), o <c>null</c> si no tiene.</summary>
    public DiscordRole? GetMemberPais(DiscordMember member)
    {
        HashSet<ulong> paisRoles = config.PaisTimezones.Select(x => x.RoleId).ToHashSet();
        return member.Roles.FirstOrDefault(x => paisRoles.Contains(x.Id));
    }

    public DiscordRole GetRoleByXp(DiscordGuild guild, long xp) => guild.Roles[RoleIdForRango(RangoXp.RangoActual(xp))];

    public RangoEnum RangoForRole(ulong roleId) =>
        System.Enum.GetValues<RangoEnum>().FirstOrDefault(x => x != RangoEnum.Miembro && RoleIdForRango(x) == roleId);

    private ulong RoleIdForRango(RangoEnum rango) => rango switch
    {
        RangoEnum.Tama => config.Roles.Rangos.Tama,
        RangoEnum.Casual => config.Roles.Rangos.Casual,
        RangoEnum.Kouhai => config.Roles.Rangos.Kouhai,
        RangoEnum.Senpai => config.Roles.Rangos.Senpai,
        RangoEnum.Hikikomori => config.Roles.Rangos.Hikikomori,
        RangoEnum.Sensei => config.Roles.Rangos.Sensei,
        RangoEnum.Ousama => config.Roles.Rangos.Ousama,
        RangoEnum.Teiou => config.Roles.Rangos.Teiou,
        _ => config.Roles.Miembro,
    };

    public DiscordRole GetRoleByXpActual(DiscordGuild guild, DiscordMember member)
    {
        DiscordRole miembro = guild.Roles[config.Roles.Miembro];
        DiscordRole tama = guild.Roles[config.Roles.Rangos.Tama];
        DiscordRole casual = guild.Roles[config.Roles.Rangos.Casual];
        DiscordRole kouhai = guild.Roles[config.Roles.Rangos.Kouhai];
        DiscordRole senpai = guild.Roles[config.Roles.Rangos.Senpai];
        DiscordRole hikikomori = guild.Roles[config.Roles.Rangos.Hikikomori];
        DiscordRole sensei = guild.Roles[config.Roles.Rangos.Sensei];
        DiscordRole ousama = guild.Roles[config.Roles.Rangos.Ousama];
        DiscordRole teiou = guild.Roles[config.Roles.Rangos.Teiou];

        return member.Roles.FirstOrDefault(x => x.Id == tama.Id) ?? member.Roles.FirstOrDefault(x => x.Id == casual.Id) ??
               member.Roles.FirstOrDefault(x => x.Id == kouhai.Id) ?? member.Roles.FirstOrDefault(x => x.Id == senpai.Id) ??
               member.Roles.FirstOrDefault(x => x.Id == hikikomori.Id) ?? member.Roles.FirstOrDefault(x => x.Id == sensei.Id) ??
               member.Roles.FirstOrDefault(x => x.Id == ousama.Id) ?? member.Roles.FirstOrDefault(x => x.Id == teiou.Id) ??
               miembro;
    }
}
