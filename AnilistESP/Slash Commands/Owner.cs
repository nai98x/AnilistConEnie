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

        [SlashCommand("configurarbienvenida", "Agrega el mensaje de bienvenida")]
        public async Task ConfigurarBienvenida(InteractionContext ctx)
        {
            await ctx.DeferAsync(true);

            DiscordGuild guild = ctx.Client.Guilds[862408834693070898];
            DiscordChannel channel = guild.Channels[1096194551540101281];
            DiscordChannel general = guild.Channels[862408834693070901];
            DiscordEmoji worrylove = DiscordEmoji.FromGuildEmote(ctx.Client, 862730040504746014);

            var msgBuilder = new DiscordMessageBuilder();

            msgBuilder.AddEmbed(new DiscordEmbedBuilder()
                .WithThumbnail("https://media.discordapp.net/attachments/879612956848062514/879628082921762826/imagen_2021-07-15_150953_1.png")
                .WithTitle("Vincula tu AniList")
                .WithDescription($"Haz click en boton para vincular tu cuenta de AniList y desbloquear el contenido del servidor.\n\nSi necesitas ayuda, no dudes en preguntar por {general.Mention} {worrylove}")
                .WithColor(Funciones.GetColor())
            );

            msgBuilder.AddComponents(new DiscordButtonComponent(ButtonStyle.Primary, "btn_vincularAniList", "Vincular AniList"));

            await channel.SendMessageAsync(msgBuilder);

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Mensaje de bienvenida creado con exito"));
        }
    }
}
