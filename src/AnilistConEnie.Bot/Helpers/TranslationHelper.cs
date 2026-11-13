using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AnilistConEnie.Bot.Helpers;

public static class TranslationHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Traduce un texto usando la instancia local de LibreTranslate. Si el servicio responde con error
    /// devuelve el texto original sin modificar; si no se lo puede contactar, lanza.
    /// </summary>
    public static async Task<string> TranslateAsync(HttpClient client, string url, string text, string source, string target)
    {
        if (string.IsNullOrEmpty(text)) return text;

        HttpResponseMessage response = await client.PostAsJsonAsync(
            url,
            new { q = text, source, target, format = "text" });
        if (!response.IsSuccessStatusCode) return text;

        TranslateResponse? result = JsonSerializer.Deserialize<TranslateResponse>(await response.Content.ReadAsStringAsync(), JsonOptions);
        return string.IsNullOrEmpty(result?.TranslatedText) ? text : result.TranslatedText;
    }

    private sealed class TranslateResponse
    {
        [JsonPropertyName("translatedText")] public string TranslatedText { get; set; } = string.Empty;
    }
}
