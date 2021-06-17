using Newtonsoft.Json;

namespace AnilistESP
{
    public struct ConfigJson
    {
        [JsonProperty("token_prod")]
        public string TokenProd { get; private set; }

        [JsonProperty("token_test")]
        public string TokenTest { get; private set; }

        [JsonProperty("topgg_token")]
        public string TopGG_token { get; private set; }
    }
}
