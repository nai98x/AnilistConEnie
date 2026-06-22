using System.Globalization;
using AnilistConEnie.Application.Helpers;
using AnilistConEnie.Bot.Configuration;
using AnilistConEnie.Model.Entities;
using AnilistConEnie.Model.Entities.Anilist;
using AnilistConEnie.Model.Enum;
using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using DSharpPlus.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AnilistConEnie.Bot.Helpers;

public class DiscordHelper(BotConfiguration config, ILogger<DiscordHelper> logger)
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
    
    public static long GetXpFromRango(RangoEnum rango)
    {
        return rango switch
        {
            RangoEnum.Miembro => 0,
            RangoEnum.Tama => 2000,
            RangoEnum.Casual => 10000,
            RangoEnum.Kouhai => 40000,
            RangoEnum.Senpai => 80000,
            RangoEnum.Hikikomori => 120000,
            RangoEnum.Sensei => 200000,
            RangoEnum.Ousama => 500000,
            _ => 1000000
        };
    }
    
    public DiscordRole GetRoleByXp(DiscordGuild guild, long xp)
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

        return xp switch
        {
            >= 1000000 => teiou,
            >= 500000 => ousama,
            >= 200000 => sensei,
            >= 120000 => hikikomori,
            >= 80000 => senpai,
            >= 40000 => kouhai,
            >= 10000 => casual,
            >= 2000 => tama,
            _ => miembro,
        };
    }

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
    
    public static async Task<int> GetElegidoAsync(CommandContext ctx, double timeoutGeneral, List<TitleDescription> opciones)
    {
        int cantidadOpciones = opciones.Count;
        if (cantidadOpciones == 1)
        {
            return 1;
        }
        
        InteractivityExtension interactivity = ctx.ServiceProvider.GetRequiredService<InteractivityExtension>();
        List<DiscordSelectComponentOption> options = [];
        const string customId = "dropdownGetElegido";

        int i = 0;
        opciones.ForEach(opc =>
        {
            if (i >= 25 || opc.Title == null) return;
            i++;
            options.Add(new DiscordSelectComponentOption(StringHelper.NormalizarBoton(opc.Title), $"{i}", opc.Description ?? string.Empty));
        });

        DiscordSelectComponent dropdown = new(customId, "Selecciona una opción", options);

        DiscordEmbedBuilder embed = new()
        {
            Color = DiscordColor.Blurple,
            Title = "Elige una opción",
        };

        DiscordMessage elegirMsg = await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddActionRowComponent(dropdown).AddEmbed(embed));

        InteractivityResult<ComponentInteractionCreatedEventArgs> msgElegirInter = await interactivity.WaitForSelectAsync(elegirMsg, ctx.User, customId, TimeSpan.FromSeconds(timeoutGeneral));

        if (msgElegirInter.TimedOut) return -1;
        
        ComponentInteractionCreatedEventArgs resultElegir = msgElegirInter.Result;
        return int.Parse(resultElegir.Values[0]);

    }
}