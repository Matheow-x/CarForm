using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Compression;
using System.Text;

namespace CarForm.Printing;

/// <summary>
/// A tiny, dependency-free PDF writer: one rasterised (FlateDecode RGB) image per page.
/// Rasterising is intentional - it guarantees that the PDF is pixel-identical to the WPF
/// print preview, including right-to-left shaping of Persian text and custom fonts.
/// </summary>
public sealed class MiniPdfWriter
{
    private sealed record PageEntry(byte[] CompressedPixels, int PixelWidth, int PixelHeight, double WidthInPoints, double HeightInPoints);

    private readonly List<PageEntry> _pages = new();

    public int PageCount => _pages.Count;

    /// <param name="compressedPixels">Raw RGB samples (width * height * 3) compressed with zlib.</param>
    public void AddPage(byte[] compressedPixels, int pixelWidth, int pixelHeight, double widthInPoints, double heightInPoints)
        => _pages.Add(new PageEntry(compressedPixels, pixelWidth, pixelHeight, widthInPoints, heightInPoints));

    public byte[] Build(string title = "Document", string producer = "CarForm", string? author = null)
    {
        var objects = new List<byte[]>();

        var pageIds = new List<int>();
        for (var i = 0; i < _pages.Count; i++) pageIds.Add(3 + (3 * i));
        var infoId = 3 + (3 * _pages.Count);

        // 1 - catalog
        objects.Add(Ascii($"<< /Type /Catalog /Pages 2 0 R >>"));

        // 2 - pages
        var kids = string.Join(' ', pageIds.ConvertAll(id => $"{id} 0 R"));
        objects.Add(Ascii($"<< /Type /Pages /Kids [{kids}] /Count {_pages.Count} >>"));

        // 3.. pages
        for (var i = 0; i < _pages.Count; i++)
        {
            var page = _pages[i];
            var pageId = pageIds[i];
            var imageId = pageId + 1;
            var contentId = pageId + 2;

            var mediaBox = $"[0 0 {F(page.WidthInPoints)} {F(page.HeightInPoints)}]";
            objects.Add(Ascii($"<< /Type /Page /Parent 2 0 R /MediaBox {mediaBox} " +
                              $"/Resources << /XObject << /Im0 {imageId} 0 R >> >> /Contents {contentId} 0 R >>"));

            // image XObject
            var imageHeader = new List<byte>();
            imageHeader.AddRange(Ascii($"<< /Type /XObject /Subtype /Image /Width {page.PixelWidth} /Height {page.PixelHeight} " +
                                       $"/ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /FlateDecode /Length {page.CompressedPixels.Length} >>"));
            imageHeader.AddRange(Ascii("\nstream\n"));
            imageHeader.AddRange(page.CompressedPixels);
            imageHeader.AddRange(Ascii("\nendstream"));
            objects.Add(imageHeader.ToArray());

            var content = $"q\n{F(page.WidthInPoints)} 0 0 {F(page.HeightInPoints)} 0 0 cm\n/Im0 Do\nQ\n";
            var contentBytes = Ascii(content);
            var contentHeader = new List<byte>();
            contentHeader.AddRange(Ascii($"<< /Length {contentBytes.Length} >>"));
            contentHeader.AddRange(Ascii("\nstream\n"));
            contentHeader.AddRange(contentBytes);
            contentHeader.AddRange(Ascii("endstream"));
            objects.Add(contentHeader.ToArray());
        }

        // info
        var now = DateTime.Now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        objects.Add(Ascii($"<< /Title {PdfText(title)} /Producer {PdfText(producer)} " +
                          $"/Author {PdfText(author ?? producer)} /Creator {PdfText(producer)} " +
                          $"/CreationDate (D:{now}) >>"));

        // serialize
        var buffer = new List<byte>(1024 * 64);

        // PDF header (the second line marks the file as binary so that viewers do not treat it as text)
        buffer.AddRange(Ascii("%PDF-1.4\n"));
        buffer.AddRange(new byte[] { 0x25, 0xE2, 0xE3, 0xCF, 0xD3, 0x0A });
        var offsets = new List<int>(objects.Count);
        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(buffer.Count);
            buffer.AddRange(Ascii($"{i + 1} 0 obj\n"));
            buffer.AddRange(objects[i]);
            buffer.AddRange(Ascii("\nendobj\n"));
        }

        var xrefOffset = buffer.Count;
        buffer.AddRange(Ascii($"xref\n0 {objects.Count + 1}\n"));
        buffer.AddRange(Ascii("0000000000 65535 f \n"));
        foreach (var offset in offsets)
        {
            buffer.AddRange(Ascii($"{offset:D10} 00000 n \n"));
        }

        buffer.AddRange(Ascii($"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R /Info {infoId} 0 R >>\n"));
        buffer.AddRange(Ascii($"startxref\n{xrefOffset}\n%%EOF\n"));

        return buffer.ToArray();
    }

    public static byte[] Compress(byte[] rawSamples)
    {
        using var output = new System.IO.MemoryStream();
        using (var zlib = new ZLibStream(output, CompressionLevel.Optimal, true))
        {
            zlib.Write(rawSamples, 0, rawSamples.Length);
        }
        return output.ToArray();
    }

    private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    private static byte[] Ascii(string text) => Encoding.ASCII.GetBytes(text);

    /// <summary>PDF text string encoded as UTF-16BE hex (safe for Persian characters).</summary>
    private static string PdfText(string? text)
    {
        var value = text ?? string.Empty;
        var builder = new StringBuilder("<FEFF");
        foreach (var unit in value)
        {
            builder.Append(((int)unit).ToString("X4", CultureInfo.InvariantCulture));
        }
        builder.Append('>');
        return builder.ToString();
    }
}
