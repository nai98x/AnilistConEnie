using AnilistConEnie;
using DSharpPlus;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

string discordToken = config["discordToken"] ?? throw new Exception("Es necesario configurar el token de Discord");

DiscordClientBuilder builder = DiscordClientBuilder.CreateDefault(discordToken, DiscordIntents.All);

builder.ConfigureEventHandlers
(
    b => b.HandleMessageCreated(Events.MessageCreated)
);

await builder.ConnectAsync();
await Task.Delay(-1);
