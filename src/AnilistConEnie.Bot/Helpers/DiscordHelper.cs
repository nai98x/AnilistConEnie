using AnilistConEnie.Bot.Configuration;
using AnilistConEnie.Model.Enum;
using DSharpPlus.Entities;

namespace AnilistConEnie.Bot.Helpers;

public class DiscordHelper(BotConfiguration config)
{
    public static DiscordColor GetColor() => DiscordColor.Blurple;

    public bool RangoAPartirDe(DiscordGuild guild, DiscordMember member, RangoEnum rango, bool verificarActivo)
    {
        if (verificarActivo && member.Roles.Any(x => x.Id ==  config.Roles.Inactivo))
            return false;

        ulong tama = config.Roles.Rangos.Tama;
        ulong casual = config.Roles.Rangos.Casual;
        ulong kouhai = config.Roles.Rangos.Kouhai;
        ulong senpai = config.Roles.Rangos.Senpai;
        ulong hikikomori = config.Roles.Rangos.Hikikomori;
        ulong sensei = config.Roles.Rangos.Sensei;
        ulong ousama = config.Roles.Rangos.Ousama;
        ulong teiou = config.Roles.Rangos.Teiou;

        return rango switch
        {
            RangoEnum.Tama => member.Roles.Any(x => x.Id == tama) || member.Roles.Any(x => x.Id == casual) || member.Roles.Any(x => x.Id == kouhai) || member.Roles.Any(x => x.Id == senpai) ||
                                    member.Roles.Any(x => x.Id == hikikomori) || member.Roles.Any(x => x.Id == sensei) || member.Roles.Any(x => x.Id == ousama) || member.Roles.Any(x => x.Id == teiou),
            RangoEnum.Casual => member.Roles.Any(x => x.Id == casual) || member.Roles.Any(x => x.Id == kouhai) || member.Roles.Any(x => x.Id == senpai) ||
                                    member.Roles.Any(x => x.Id == hikikomori) || member.Roles.Any(x => x.Id == sensei) || member.Roles.Any(x => x.Id == ousama) || member.Roles.Any(x => x.Id == teiou),
            RangoEnum.Kouhai => member.Roles.Any(x => x.Id == kouhai) || member.Roles.Any(x => x.Id == senpai) || member.Roles.Any(x => x.Id == hikikomori) ||
                                    member.Roles.Any(x => x.Id == sensei) || member.Roles.Any(x => x.Id == ousama) || member.Roles.Any(x => x.Id == teiou),
            RangoEnum.Senpai => member.Roles.Any(x => x.Id == senpai) || member.Roles.Any(x => x.Id == hikikomori) ||
                                    member.Roles.Any(x => x.Id == sensei) || member.Roles.Any(x => x.Id == ousama) || member.Roles.Any(x => x.Id == teiou),
            RangoEnum.Hikikomori => member.Roles.Any(x => x.Id == hikikomori) || member.Roles.Any(x => x.Id == sensei) || member.Roles.Any(x => x.Id == ousama) || member.Roles.Any(x => x.Id == teiou),
            RangoEnum.Sensei => member.Roles.Any(x => x.Id == sensei) || member.Roles.Any(x => x.Id == ousama) || member.Roles.Any(x => x.Id == teiou),
            RangoEnum.Ousama => member.Roles.Any(x => x.Id == ousama) || member.Roles.Any(x => x.Id == teiou),
            RangoEnum.Teiou => member.Roles.Any(x => x.Id == teiou),
            _ => true,
        };
    }
}