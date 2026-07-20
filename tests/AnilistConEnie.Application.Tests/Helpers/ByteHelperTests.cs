using AnilistConEnie.Application.Helpers;

namespace AnilistConEnie.Application.Tests.Helpers;

public class ByteHelperTests
{
    [Fact]
    public void ToMemoryStream_ConservaLosBytes()
    {
        byte[] datos = [1, 2, 3, 42, 255];

        using MemoryStream stream = datos.ToMemoryStream();

        Assert.Equal(datos, stream.ToArray());
    }

    [Fact]
    public void ToMemoryStream_ArrancaEnPosicionCero()
    {
        using MemoryStream stream = new byte[] { 9, 8, 7 }.ToMemoryStream();

        Assert.Equal(0, stream.Position);
    }

    [Fact]
    public void ToMemoryStream_ArrayVacio_StreamVacio()
    {
        using MemoryStream stream = Array.Empty<byte>().ToMemoryStream();

        Assert.Equal(0, stream.Length);
    }
}
