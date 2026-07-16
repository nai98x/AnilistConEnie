using AnilistConEnie.Bot.Commands.Framework.Checks;
using AnilistConEnie.Bot.Commands.Framework.Attributes;
using AnilistConEnie.Bot.Helpers;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.TextCommands;
using DSharpPlus.Entities;

namespace AnilistConEnie.Bot.Commands.Text;

[TestCommand]
public class Admin
{
    private const int MaxBotonesPorFila = 5;
    private const int MaxFilas = 5;

    [Command("yoink")]
    [RequireKamiSama]
    public async Task Yoink(TextCommandContext ctx)
    {
        DiscordMessage? referenciado = ctx.Message.ReferencedMessage;
        if (referenciado is null)
        {
            await ctx.RespondAsync("Respondé a un mensaje con emotes para poder robarlos.");
            return;
        }

        DiscordGuild guild = ctx.Guild!;
        int maxSlots = MaxEmojiSlots(guild.PremiumTier);
        int estaticosUsados = guild.Emojis.Values.Count(e => !e.IsAnimated);
        int animadosUsados = guild.Emojis.Values.Count(e => e.IsAnimated);

        List<(string Name, ulong Id, bool Animated)> candidatos = DiscordEmojiHelper.FindAllCustomEmotes(referenciado.Content)
            .Where(e => !guild.Emojis.ContainsKey(e.Id))
            .Where(e => (e.Animated ? animadosUsados : estaticosUsados) < maxSlots)
            .Take(MaxBotonesPorFila * MaxFilas)
            .ToList();

        if (candidatos.Count == 0)
        {
            await ctx.RespondAsync("No hay emotes de otros servidores para robar en ese mensaje (o ya están en este server / no hay lugar).");
            return;
        }

        DiscordContainerComponent container = new(
            [new DiscordTextDisplayComponent($"{candidatos.Count} emote(s) para robar:")],
            color: DiscordEmojiHelper.GetColor());

        DiscordMessageBuilder builder = new DiscordMessageBuilder()
            .EnableV2Components()
            .AddContainerComponent(container);

        foreach ((string Name, ulong Id, bool Animated)[] fila in candidatos.Chunk(MaxBotonesPorFila))
        {
            builder.AddActionRowComponent(fila.Select(e => new DiscordButtonComponent(
                DiscordButtonStyle.Secondary,
                $"yoink-{e.Id}-{(e.Animated ? 1 : 0)}-{e.Name}",
                "Yoink",
                emoji: new DiscordComponentEmoji(e.Id))));
        }

        await ctx.RespondAsync(builder);
    }

    private static int MaxEmojiSlots(DiscordPremiumTier tier) => tier switch
    {
        DiscordPremiumTier.Tier_3 => 250,
        DiscordPremiumTier.Tier_2 => 150,
        DiscordPremiumTier.Tier_1 => 100,
        _ => 50
    };
}
