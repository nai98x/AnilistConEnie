using AnilistConEnie.Application.Helpers;
using SkiaSharp;

namespace AnilistConEnie.Application.Tests.Helpers;

public class ImageHelperTests
{
    private static byte[] PngValido()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(2, 2));
        surface.Canvas.Clear(SKColors.Red);
        using SKImage snapshot = surface.Snapshot();
        using SKData data = snapshot.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    [Fact]
    public void EsImagenValida_RealPng_ReturnsTrue()
    {
        Assert.True(ImageHelper.EsImagenValida(PngValido()));
    }

    [Fact]
    public void EsImagenValida_ArbitraryBytes_ReturnsFalse()
    {
        Assert.False(ImageHelper.EsImagenValida("esto no es una imagen"u8.ToArray()));
    }

    [Fact]
    public void EsImagenValida_Empty_ReturnsFalse()
    {
        Assert.False(ImageHelper.EsImagenValida([]));
    }
}
