using AnilistConEnie.Application.Anilist;

namespace AnilistConEnie.Application.Tests.Anilist;

public class AnilistActivityUrlTests
{
    [Theory]
    [InlineData("https://anilist.co/activity/12345", 12345)]
    [InlineData("https://anilist.co/activity/12345/", 12345)]
    [InlineData("67890", 67890)]
    public void TryGetActivityId_NumericLastSegment_ReturnsId(string url, int esperado)
    {
        Assert.True(AnilistActivityUrl.TryGetActivityId(url, out int activityId));
        Assert.Equal(esperado, activityId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("https://anilist.co/activity/abc")]
    public void TryGetActivityId_NonNumeric_ReturnsFalse(string? url)
    {
        Assert.False(AnilistActivityUrl.TryGetActivityId(url, out int activityId));
        Assert.Equal(0, activityId);
    }
}
