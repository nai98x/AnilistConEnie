using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System.Threading.Tasks;

namespace AnilistESP
{
    public class Help : ApplicationCommandModule
    {
        [SlashCommand("help", "Informacion y ayuda del bot")]
        public async Task HelpCommand(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            var comandosTopLevel = ctx.SlashCommandsExtension.RegisteredCommands;
            string comandos = string.Empty;

            string descGeneral = "```Bot oficial de la comunidad Añilist```\n";

            comandos += "**Comandos Disponibles**\n";
            foreach (var cmdPairTopLevel in comandosTopLevel)
            {
                var cmdTopLevel = cmdPairTopLevel.Value;
                foreach (var cmd in cmdTopLevel)
                {
                    if (cmd.Type == ApplicationCommandType.SlashCommand)
                    {
                        comandos += $"`/{cmd.Name}` {cmd.Description}\n";
                    }
                }
            }

            var embed = new DiscordEmbedBuilder
            {
                Title = "Acerca del bot",
                Description = descGeneral + comandos,
                Color = Funciones.GetColor()
            };

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
        }
    }
}
