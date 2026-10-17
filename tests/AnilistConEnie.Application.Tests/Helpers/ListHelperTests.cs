using AnilistConEnie.Application.Helpers;

namespace AnilistConEnie.Application.Tests.Helpers;

public class ListHelperTests
{
    [Fact]
    public void Shuffle_List_PreservesElements()
    {
        List<int> list = [.. Enumerable.Range(1, 100)];

        list.Shuffle();

        Assert.Equal([.. Enumerable.Range(1, 100)], list.OrderBy(x => x));
        Assert.Equal(100, list.Count);
    }

    [Fact]
    public void Shuffle_SingleElementList_Unchanged()
    {
        List<string> list = ["solo"];
        list.Shuffle();
        Assert.Equal(["solo"], list);
    }
}
