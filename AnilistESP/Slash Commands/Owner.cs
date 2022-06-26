namespace AnilistConEnie.Commands
{
    using AnilistESP;
    using DSharpPlus;
    using DSharpPlus.Entities;
    using DSharpPlus.SlashCommands;
    using DSharpPlus.SlashCommands.Attributes;
    using GraphQL;
    using GraphQL.Client.Http;
    using GraphQL.Client.Serializer.Newtonsoft;
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    [SlashCommandGroup("owner", "Comandos solo disponibles para el owner de Yumiko")]
    [SlashRequireOwner]
    [SlashCommandPermissions(Permissions.Administrator)]
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

        [SlashCommand("ratelimits", "Shows the ratelimits")]
        [DescriptionLocalization(Localization.Spanish, "Muestra los ratelimits")]
        public async Task Ratelimits(InteractionContext ctx)
        {
            await ctx.DeferAsync();

            GraphQLHttpClient graphQlClient = new("https://graphql.anilist.co", new NewtonsoftJsonSerializer());
            var request = new GraphQLRequest
            {
                Query =
                    "query {" +
                    "   Media (id: 1) {" +
                    "       id" +
                    "   }" +
                    "}"
            };
            var data = await graphQlClient.SendQueryAsync<dynamic>(request);

            var response = data.AsGraphQLHttpResponse();
            var rateLimitLimit = response.ResponseHeaders.GetValues("X-RateLimit-Limit").First();
            var rateLimitRemaining = response.ResponseHeaders.GetValues("X-RateLimit-Remaining").First();

            string desc = $"{Formatter.Bold("AniList:")}\n" +
                $"Limit: {rateLimitLimit}\n" +
                $"Remaining: {rateLimitRemaining}";

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
            {
                Title = "Ratelimits",
                Description = Funciones.NormalizarDescription(desc),
                Color = Funciones.GetColor()
            }));
        }
    }
}
