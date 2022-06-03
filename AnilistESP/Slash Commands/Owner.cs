namespace AnilistESP
{
    using System;
    using System.Threading.Tasks;
    using DSharpPlus;
    using DSharpPlus.Entities;
    using DSharpPlus.SlashCommands;
    using DSharpPlus.SlashCommands.Attributes;

    [SlashCommandGroup("owner", "Comandos solo disponibles para el owner de Yumiko")]
    [SlashRequireOwner]
    public class Owner : ApplicationCommandModule
    {
        [SlashCommand("test", "Testeos del bot")]
        public async Task Test(InteractionContext ctx)
        {
            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"Nothing to test"));
        }

        [SlashCommand("eliminarguild", "Elimina a Yumiko de un servidor")]
        public async Task EliminarServer(InteractionContext ctx, [Option("Id", "Id del servidor a salirse")] string idStr)
        {
            try
            {
                long id = long.Parse(idStr);
                var guild = await ctx.Client.GetGuildAsync((ulong)id);
                if (guild != null)
                {
                    string nombre = guild.Name;
                    await guild.LeaveAsync();
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"He salido del servidor `{nombre} ({id})`"));
                }
                else
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"No se encontró el servidor con la Id `{id}`"));
                }
            }
            catch
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Hubo un error obteniendo el servidor con la Id `{idStr}`"));
            }
        }

        [SlashCommand("apagar", "Apaga el bot")]
        public async Task Shutdown(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Apagando...").AsEphemeral(true));
            Environment.Exit(0);
        }
    }
}
