using AnilistConEnie.Application.Helpers;

namespace AnilistConEnie.Application.Tests.Helpers;

public class ByteHelperTests
{
    [Fact]
    public void ToMemoryStream_Bytes_PreservesBytes()
    {
        byte[] datos = [1, 2, 3, 42, 255];

        using MemoryStream stream = datos.ToMemoryStream();

        Assert.Equal(datos, stream.ToArray());
    }

    [Fact]
    public void ToMemoryStream_Bytes_StartsAtPositionZero()
    {
        using MemoryStream stream = new byte[] { 9, 8, 7 }.ToMemoryStream();

        Assert.Equal(0, stream.Position);
    }

    [Fact]
    public void ToMemoryStream_EmptyArray_ReturnsEmptyStream()
    {
        using MemoryStream stream = Array.Empty<byte>().ToMemoryStream();

        Assert.Equal(0, stream.Length);
    }
}
