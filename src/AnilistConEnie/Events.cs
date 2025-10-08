using AnilistConEnie.Repository;
using DSharpPlus;
using DSharpPlus.EventArgs;

namespace AnilistConEnie;

public static class Events
{
    public static async Task MessageCreated(DiscordClient client, MessageCreatedEventArgs args)
    {
        if (args.Message.ChannelId == 1106702589359292416 && !args.Author.IsBot)
        {
            await args.Message.RespondAsync("uwu");
        }
    }
}
