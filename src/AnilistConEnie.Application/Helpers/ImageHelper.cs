using SkiaSharp;

namespace AnilistConEnie.Application.Helpers;

public static class ImageHelper
{
    private static readonly SKSamplingOptions Sampling = new(SKFilterMode.Linear, SKMipmapMode.None);

    /// <summary>
    /// Coloca dos imágenes lado a lado (cada una ocupando la mitad izquierda/derecha) en un lienzo de x*y.
    /// </summary>
    public static byte[] MergeImage(byte[] bytes1, byte[] bytes2, int x, int y)
    {
        using SKImage img1 = SKImage.FromEncodedData(bytes1);
        using SKImage img2 = SKImage.FromEncodedData(bytes2);

        using SKSurface surface = SKSurface.Create(new SKImageInfo(x, y));
        SKCanvas canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        canvas.DrawImage(img1, new SKRect(0, 0, x / 2f, y), Sampling);
        canvas.DrawImage(img2, new SKRect(x / 2f, 0, x, y), Sampling);

        return Encode(surface);
    }

    /// <summary>
    /// Superpone <paramref name="image2"/> sobre <paramref name="image1"/> ocupando ambas el lienzo de x*y.
    /// </summary>
    public static byte[] OverlapImage(byte[] image1, byte[] image2, int x, int y)
    {
        using SKImage img1 = SKImage.FromEncodedData(image1);
        using SKImage img2 = SKImage.FromEncodedData(image2);

        using SKSurface surface = SKSurface.Create(new SKImageInfo(x, y));
        SKCanvas canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        SKRect dest = new(0, 0, x, y);
        canvas.DrawImage(img1, dest, Sampling);
        canvas.DrawImage(img2, dest, Sampling);

        return Encode(surface);
    }

    /// <summary>
    /// Dibuja <paramref name="image"/> (redimensionada a 500x500) dentro de <paramref name="templateImage"/> en la
    /// posición (x, y). Usa composición DstAtop para que la imagen solo se vea por los huecos transparentes de la
    /// plantilla, manteniéndola por detrás del marco.
    /// </summary>
    public static byte[] DrawIntoImage(byte[] templateImage, byte[] image, int x, int y)
    {
        using SKImage template = SKImage.FromEncodedData(templateImage);
        using SKImage img = SKImage.FromEncodedData(image);

        using SKSurface surface = SKSurface.Create(new SKImageInfo(template.Width, template.Height));
        SKCanvas canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        canvas.DrawImage(template, 0, 0, Sampling);

        using SKPaint paint = new() { BlendMode = SKBlendMode.DstATop };
        canvas.DrawImage(img, new SKRect(x, y, x + 500, y + 500), Sampling, paint);

        return Encode(surface);
    }

    /// <summary>Verifica que los bytes correspondan a una imagen decodificable (no alcanza con el content-type declarado).</summary>
    public static bool EsImagenValida(byte[] bytes)
    {
        if (bytes.Length == 0) return false;
        using SKData data = SKData.CreateCopy(bytes);
        using SKCodec? codec = SKCodec.Create(data);
        return codec is not null;
    }

    private static byte[] Encode(SKSurface surface)
    {
        using SKImage snapshot = surface.Snapshot();
        using SKData data = snapshot.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}
