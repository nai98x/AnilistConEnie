namespace AnilistConEnie.Application.Helpers;

public static class ByteHelper
{
    public static MemoryStream ToMemoryStream(this byte[] byteArray)
    {
        return new MemoryStream(byteArray)
        {
            Position = 0,
        };
    }
}
