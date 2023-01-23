using AnilistESP;
using DSharpPlus;
using DSharpPlus.SlashCommands;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Linq;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Interactivity;

namespace AnilistConEnie.Commands
{
    public class Tatsu : ApplicationCommandModule
    {
        [SlashCommand("rank", "Muestra el ranking de experiencia del servidor")]
        public async Task Rank(InteractionContext ctx)
        {
            await ctx.DeferAsync();

            string token = await Funciones.ObtenerTokenTatsu();
            var client = new RestClient("https://api.tatsu.gg/v1");

            List<Ranking> rankings = new();
            List<Ranking> rankingsFiltered = new();
            int offset = 0;

            try
            {
                do
                {
                    var request = new RestRequest($"/guilds/{ctx.Guild.Id}/rankings/all?offset={offset}", Method.Get);
                    request.AddHeader("Authorization", token);
                    request.AddHeader("Content-Type", "application/json");
                    var response = await client.ExecuteAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        TatsuRanking ranking = TatsuRanking.FromJson(response.Content);

                        rankings.AddRange(ranking.Rankings);

                        offset += 100;
                        if (ranking.Rankings.Count < 100)
                        {
                            break;
                        }
                    }
                    else
                    {
                        await Funciones.GrabarLogError(Funciones.GetContext(ctx), $"Error obteniendo xp de Tatsu\n{response.ErrorMessage}");
                        break;
                    }
                } while (true);

                foreach(var user in rankings)
                {
                    if (ctx.Guild.Members.TryGetValue(ulong.Parse(user.UserId), out _))
                    {
                        rankingsFiltered.Add(user);
                    }
                }

                var chunks = rankingsFiltered.Chunk(29);
                List<Page> pages = new();

                DiscordEmoji umaPoints = DiscordEmoji.FromGuildEmote(ctx.Client, 862461175950606376);
                var embed = new DiscordEmbedBuilder
                {
                    Color = Funciones.GetColor(),
                    Title = $"Ranking de experiencia",
                    Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail
                    {
                        Url = "https://media.discordapp.net/attachments/862568630365323264/990747470508204032/unknown.png"
                    }
                };

                foreach (var chunk in chunks)
                {
                    Page page = new();
                    string desc = string.Empty;

                    foreach (var userRank in chunk)
                    {
                        desc += $"**#{userRank.Rank}** <@{userRank.UserId}> - **{userRank.Score} {umaPoints}**\n";
                    }

                    pages.Add(new Page() { Embed = embed.WithDescription(desc) });
                }

                var interactivity = ctx.Client.GetInteractivity();
                await interactivity.SendPaginatedResponseAsync(ctx.Interaction, false, ctx.User, pages, asEditResponse: true);
            }
            catch (Exception ex)
            {
                await Funciones.GrabarLogError(Funciones.GetContext(ctx), $"Error obteniendo xp de Tatsu\n{ex.Message}\n{Formatter.BlockCode(ex.StackTrace)}");
            }
        }
    }

    public partial class TatsuRanking
    {
        [JsonProperty("guild_id")]
        public string GuildId { get; set; }

        [JsonProperty("rankings")]
        public List<Ranking> Rankings { get; set; }
    }

    public partial class Ranking
    {
        [JsonProperty("rank")]
        public long Rank { get; set; }

        [JsonProperty("score")]
        public long Score { get; set; }

        [JsonProperty("user_id")]
        public string UserId { get; set; }
    }

    public partial class TatsuRanking
    {
        public static TatsuRanking FromJson(string json) => JsonConvert.DeserializeObject<TatsuRanking>(json, Converter.Settings);
    }

    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters =
            {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }
}
