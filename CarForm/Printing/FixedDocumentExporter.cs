using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CarForm.Printing;

/// <summary>Renders <see cref="FixedPage"/>s to bitmaps and packs them into a PDF.</summary>
public static class FixedDocumentExporter
{
    public static byte[] ToPdf(FixedDocument document, int dpi = 300, string title = "سند فروش خودرو")
    {
        var pages = document.Pages.OfType<PageContent>()
            .Select(p => p.Child)
            .OfType<FixedPage>()
            .ToList();

        if (pages.Count == 0) pages.Add(new FixedPage { Width = PrintLayout.A4.Width, Height = PrintLayout.A4.Height });

        var writer = new MiniPdfWriter();
        foreach (var page in pages)
        {
            var pageSize = new Size(page.Width, page.Height);
            var (pixels, width, height) = RenderPage(page, pageSize, dpi);
            var points = PrintLayout.ToPoints(pageSize);
            writer.AddPage(MiniPdfWriter.Compress(pixels), width, height, points.Width, points.Height);
        }

        return writer.Build(title);
    }

    public static void SavePdf(FixedDocument document, string path, int dpi = 300, string title = "سند فروش خودرو")
    {
        var bytes = ToPdf(document, dpi, title);
        var directory = System.IO.Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory)) System.IO.Directory.CreateDirectory(directory);
        System.IO.File.WriteAllBytes(path, bytes);
    }

    /// <summary>Rasterises one page. Returns top-down RGB samples (flattened on white).</summary>
    private static (byte[] Pixels, int Width, int Height) RenderPage(Visual page, Size pageSize, int dpi)
    {
        dpi = Math.Clamp(dpi, 72, 600);
        var scale = dpi / PrintLayout.Dpi;

        var pixelWidth = Math.Max(1, (int)Math.Round(pageSize.Width * scale));
        var pixelHeight = Math.Max(1, (int)Math.Round(pageSize.Height * scale));

        if (page is UIElement element)
        {
            element.Measure(pageSize);
            element.Arrange(new Rect(0, 0, pageSize.Width, pageSize.Height));
            element.UpdateLayout();
        }

        var bitmap = new RenderTargetBitmap(pixelWidth, pixelHeight, dpi, dpi, PixelFormats.Pbgra32);
        bitmap.Render(page);

        var stride = pixelWidth * 4;
        var buffer = new byte[stride * pixelHeight];
        bitmap.CopyPixels(buffer, stride, 0);

        var rgb = new byte[pixelWidth * pixelHeight * 3];
        for (var y = 0; y < pixelHeight; y++)
        {
            var sourceRow = y * stride;
            var targetRow = y * pixelWidth * 3;
            for (var x = 0; x < pixelWidth; x++)
            {
                var source = sourceRow + (x * 4);
                var target = targetRow + (x * 3);

                // Pbgra32 stores pre-multiplied BGRA; flatten onto a white page.
                var b = buffer[source];
                var g = buffer[source + 1];
                var r = buffer[source + 2];
                var a = buffer[source + 3];

                rgb[target] = Flatten(r, a);
                rgb[target + 1] = Flatten(g, a);
                rgb[target + 2] = Flatten(b, a);
            }
        }

        return (rgb, pixelWidth, pixelHeight);
    }

    private static byte Flatten(byte channel, byte alpha)
    {
        if (alpha == 255) return channel;
        if (alpha == 0) return 255;
        var value = channel + (255 - alpha);
        return (byte)Math.Min(255, value);
    }
}
