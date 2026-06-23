using System.Text.Json;
using System.Text.Json.Serialization;

namespace AnilistConEnie.Bot.Helpers;

public static class TranslationHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Traduce un texto usando la instancia local de LibreTranslate. Si el servicio no está disponible
    /// o falla, devuelve el texto original sin modificar.
    /// </summary>
    public static async Task<string> TranslateAsync(HttpClient client, string text, string source, string target)
    {
        if (string.IsNullOrEmpty(text)) return text;

        string url = $"http://127.0.0.1:5000/translate?q={Uri.EscapeDataString(text)}&source={source}&target={target}";
        HttpResponseMessage response = await client.PostAsync(url, null);
        if (!response.IsSuccessStatusCode) return text;

        TranslateResponse? result = JsonSerializer.Deserialize<TranslateResponse>(await response.Content.ReadAsStringAsync(), JsonOptions);
        return string.IsNullOrEmpty(result?.TranslatedText) ? text : result.TranslatedText;
    }

    private sealed class TranslateResponse
    {
        [JsonPropertyName("translatedText")] public string TranslatedText { get; set; } = string.Empty;
    }
}
