using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace AnilistESP
{
    public class MonoschinosDownloader
    {
        const string BaseUrl = "https://monoschinos2.com/";

        public async Task<List<SearchResults>> Search(string searchText)
        {
            List<SearchResults> searchResults = new();
            string[] searchTextList = searchText.Split(' ');
            var url = BaseUrl + "/buscar?q=";
            foreach (string word in searchTextList)
            {
                url += word + "+";
            }
            url = url.Remove(url.Length-1);
            var response = await GetHtml(url);

            HtmlDocument htmlDoc = new();
            htmlDoc.LoadHtml(response);
            var nodeResults = htmlDoc.DocumentNode.SelectNodes("//div[@class='col-md-4 col-lg-2 col-6']");

            if (nodeResults != null)
            {
                foreach (var node in nodeResults)
                {
                    var href = node.Descendants("a").First().Attributes[0].Value.Replace("https://monoschinos2.com/anime", "");
                    var asd = node.Descendants("a").First();

                    var name = node.Descendants("a").First().Descendants("series").First().Descendants("seriesdetails").First().Descendants("seristitles").First().InnerText;
                    var type = node.Descendants("a").First().Descendants("series").First().Descendants("seriesdetails").First().Descendants("seriesinfo").First().InnerText;

                    searchResults.Add(
                        new SearchResults
                        {
                            Href = href,
                            Name = name,
                            Type = type
                        }
                    );
                }
            }
            return searchResults;
        }

        public async Task<AnimeLinks> GetLinks(string url, string name)
        {
            url = url.Replace("-sub-espanol", "");
            url = BaseUrl + "ver" + url + "-episodio-";
            int episodeNumber = 1;
            bool nextEpisode = true;
            AnimeLinks result = new() {
                Name = name,
                Hosts = new List<Host>()
            };

            while (nextEpisode)
            {
                var response = await GetHtml(url + episodeNumber);
                HtmlDocument htmlDoc = new();
                htmlDoc.LoadHtml(response);

                var downloadLinks = htmlDoc.DocumentNode.SelectNodes("//tbody//tr").ToList();

                downloadLinks.ForEach(tr =>
                {
                    var tdArray = tr.Descendants("td").ToArray();
                    string href = tdArray[2].Descendants("a").First().Attributes[1].Value;
                    string nomServer = href.Replace("https://", "");
                    int index = nomServer.IndexOf("/");
                    if (index > 0)
                        nomServer = nomServer.Substring(0, index);

                    if (!result.Hosts.Exists(h => h.Name == nomServer))
                    {
                        result.Hosts.Add(
                            new Host()
                            {
                                Name = nomServer,
                                Links = new List<Link>()
                            }
                        );
                    }

                    result.Hosts.Where(h => h.Name == nomServer).First().Links.Add(
                        new Link()
                        {
                            Number = episodeNumber,
                            Href = href
                        }
                    );
                });

                nextEpisode = NextEpisodeExists(htmlDoc);
                episodeNumber++;
            }

            return result;
        }

        private async Task<string> GetHtml(string url)
        {
            HttpClient client = new();
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            client.DefaultRequestHeaders.Accept.Clear();
            var response = await client.GetStringAsync(url);
            return response;
        }

        private bool NextEpisodeExists(HtmlDocument htmlDoc)
        {
            var next = htmlDoc.DocumentNode.SelectSingleNode("//a[@class='btnWeb']//i[@class='fas fa-arrow-circle-right']");
            if (next != null)
            {
                return true;
            }
            return false;
        }
    }
}
