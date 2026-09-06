using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LabelFlow.Models;
using ZXing;
using ZXing.Common;

namespace LabelFlow.Services;

public static class BarcodeService
{
    public static ImageSource? Generate(string? value, BarcodeType type, int widthPx, int heightPx)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        widthPx = Math.Max(widthPx, 80);
        heightPx = Math.Max(heightPx, 30);

        var writer = new BarcodeWriterPixelData
        {
            Format = type == BarcodeType.Code39 ? BarcodeFormat.CODE_39 : BarcodeFormat.CODE_128,
            Options = new EncodingOptions
            {
                Width = widthPx,
                Height = heightPx,
                Margin = 2,
                PureBarcode = false
            }
        };

        try
        {
            var pixelData = writer.Write(value.Trim());
            var bitmap = new WriteableBitmap(
                pixelData.Width,
                pixelData.Height,
                96, 96,
                PixelFormats.Bgra32,
                null);

            bitmap.WritePixels(
                new Int32Rect(0, 0, pixelData.Width, pixelData.Height),
                pixelData.Pixels,
                pixelData.Width * 4,
                0);

            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return null;
        }
    }
}
