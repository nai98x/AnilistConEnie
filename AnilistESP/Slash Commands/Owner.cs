namespace AnilistConEnie.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using DSharpPlus;
    using DSharpPlus.Entities;
    using DSharpPlus.SlashCommands;
    using DSharpPlus.SlashCommands.Attributes;
    using Google.Cloud.Firestore;

    [SlashCommandGroup("owner", "Comandos solo disponibles para el owner de Yumiko")]
    [SlashRequireOwner]
    public class Owner : ApplicationCommandModule
    {
        [SlashCommand("test", "Testeos del bot")]
        public async Task Test(InteractionContext ctx)
        {
            await ctx.DeferAsync();
        }

        [SlashCommand("apagar", "Apaga el bot")]
        public async Task Shutdown(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Apagando...").AsEphemeral(true));
            Environment.Exit(0);
        }
    }
}
