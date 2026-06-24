using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AnilistConEnie.Model.Entities;
using AnilistConEnie.Model.Interfaces;

namespace AnilistConEnie.Infrastructure.Charts;

internal sealed class QuickChartClient(HttpClient httpClient) : IChartClient
{
    private const string ChartUrl = "https://quickchart.io/chart";
    private const string CreateUrl = "https://quickchart.io/chart/create";

    public async Task<byte[]> RenderAsync(ChartRequest request, CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await httpClient.PostAsJsonAsync(ChartUrl, BuildPayload(request), cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    public async Task<string> CreateUrlAsync(ChartRequest request, CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await httpClient.PostAsJsonAsync(CreateUrl, BuildPayload(request), cancellationToken);
        response.EnsureSuccessStatusCode();

        QuickChartCreateResponse? result = await response.Content.ReadFromJsonAsync<QuickChartCreateResponse>(cancellationToken);
        return result?.Url ?? throw new InvalidOperationException("QuickChart no devolvió una URL");
    }

    private static Dictionary<string, object> BuildPayload(ChartRequest request)
    {
        Dictionary<string, object> payload = new()
        {
            ["width"] = request.Width,
            ["height"] = request.Height,
            ["format"] = request.Format,
            ["chart"] = request.Config
        };

        if (request.BackgroundColor is not null)
            payload["backgroundColor"] = request.BackgroundColor;

        return payload;
    }

    private sealed class QuickChartCreateResponse
    {
        [JsonPropertyName("url")] public string? Url { get; set; }
    }
}
