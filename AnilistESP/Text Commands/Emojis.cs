namespace AnilistConEnie.Commands
{
    using DSharpPlus;
    using DSharpPlus.CommandsNext;
    using DSharpPlus.CommandsNext.Attributes;
    using DSharpPlus.Entities;
    using System.IO;
    using System.Net.Http;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    public class Emojis : BaseCommandModule
    {
		// By Naamloos/ModCore
		[Command("yoink")]
		[Description("Copies an emoji from a different server to this one")]
		[RequirePermissions(Permissions.ManageEmojis)]
		public async Task YoinkAsync(CommandContext ctx, DiscordEmoji emoji, [RemainingText] string name = "")
		{
			if (!emoji.ToString().StartsWith('<'))
			{
				await ctx.RespondAsync("⚠️ This is not a valid guild emoji!");
				return;
			}
			await StealieEmoji(ctx, string.IsNullOrEmpty(name) ? emoji.Name : name, emoji.Id, emoji.IsAnimated);
		}

		const string EMOJI_REGEX = @"<a?:(.+?):(\d+)>";
		[Command("yoink")]
		[RequirePermissions(Permissions.ManageEmojis)]
		public async Task YoinkAsync(CommandContext ctx, int index = 1)
		{
			if (ctx.Message.ReferencedMessage != null)
			{
				var matches = Regex.Matches(ctx.Message.ReferencedMessage.Content, EMOJI_REGEX);
				if (matches.Count < index || index < 1)
				{
					await ctx.RespondAsync("⚠️ Referenced emoji not found!");
					return;
				}

				var split = matches[index - 1].Groups[2].Value;
				var emojiName = matches[index - 1].Groups[1].Value;
				var animated = matches[index - 1].Value.StartsWith("<a");

				if (ulong.TryParse(split, out ulong emoji_id))
				{
					await StealieEmoji(ctx, emojiName, emoji_id, animated);
					return;
				}
				else
				{
					await ctx.RespondAsync("⚠️ Failed to fetch your new emoji.");
					return;
				}
			}
			await ctx.RespondAsync("⚠️ You need to reply to an existing message to use this command!");
		}

		private async Task StealieEmoji(CommandContext ctx, string name, ulong id, bool animated)
		{
			using HttpClient _client = new();
			var downloadedEmoji = await _client.GetStreamAsync($"https://cdn.discordapp.com/emojis/{id}.{(animated ? "gif" : "png")}");
			using MemoryStream memory = new();
			downloadedEmoji.CopyTo(memory);
			downloadedEmoji.Dispose();
			var newEmoji = await ctx.Guild.CreateEmojiAsync(name, memory);
			await ctx.RespondAsync($"✅ Yoink! This emoji has been added to your server: {newEmoji.ToString()}");
		}
	}
}
