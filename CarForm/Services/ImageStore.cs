using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CarForm.Core.Helpers;

namespace CarForm.Services;

/// <summary>
/// Stores uploaded images (logos, photos, templates) after resizing/compressing them,
/// so that a huge upload can never break the print layout or bloat the database folder.
/// </summary>
public class ImageStore : IImageStore
{
    private const int LogoMaxSize = 512;
    private const int PhotoMaxSize = 512;
    private const int TemplateMaxSize = 1600;
    private const long PngSizeLimitBytes = 300 * 1024;

    public string? Save(string sourcePath, ImageKind kind)
    {
        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath)) return null;

        var maxSize = kind switch
        {
            ImageKind.Photo => PhotoMaxSize,
            ImageKind.Template => TemplateMaxSize,
            _ => LogoMaxSize
        };

        BitmapImage source;
        try
        {
            source = new BitmapImage();
            source.BeginInit();
            source.CacheOption = BitmapCacheOption.OnLoad;
            source.UriSource = new Uri(Path.GetFullPath(sourcePath));
            source.EndInit();
            source.Freeze();
        }
        catch
        {
            return null;
        }

        var resized = Resize(source, maxSize);

        AppPaths.EnsureFolders();
        var extension = ".png";
        var path = Path.Combine(AppPaths.ImagesFolder, $"{kind.ToString().ToLowerInvariant()}-{Guid.NewGuid():N}{extension}");

        try
        {
            SavePng(resized, path);
            var info = new FileInfo(path);
            if (info.Length > PngSizeLimitBytes)
            {
                // Fall back to JPEG for photographic content (smaller file, still crisp when printed).
                File.Delete(path);
                path = Path.ChangeExtension(path, ".jpg");
                SaveJpeg(resized, path, 88);
            }
        }
        catch
        {
            return null;
        }

        return Path.Combine("Images", Path.GetFileName(path));
    }

    public void Delete(string? relativePath)
    {
        var full = AppPaths.ResolveImage(relativePath);
        if (full is null || !File.Exists(full)) return;

        // Never delete a file that is referenced by a document template.
        try
        {
            File.Delete(full);
        }
        catch
        {
            // ignore
        }
    }

    public bool Exists(string? relativePath)
    {
        var full = AppPaths.ResolveImage(relativePath);
        return full is not null && File.Exists(full);
    }

    public BitmapImage? Load(string? relativePath, int decodeWidth = 0)
    {
        var full = AppPaths.ResolveImage(relativePath);
        if (full is null || !File.Exists(full)) return null;

        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            if (decodeWidth > 0) image.DecodePixelWidth = decodeWidth;
            image.UriSource = new Uri(full, UriKind.Absolute);
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch
        {
            return null;
        }
    }

    private static BitmapSource Resize(BitmapSource source, int maxSize)
    {
        var width = source.PixelWidth;
        var height = source.PixelHeight;
        if (width <= 0 || height <= 0) return source;

        var scale = Math.Min(1d, (double)maxSize / Math.Max(width, height));
        if (scale >= 1d) return source;

        var transformed = new TransformedBitmap(source, new ScaleTransform(scale, scale));
        transformed.Freeze();
        return transformed;
    }

    private static void SavePng(BitmapSource image, string path)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(image));
        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
        encoder.Save(stream);
    }

    private static void SaveJpeg(BitmapSource image, string path, int quality)
    {
        var encoder = new JpegBitmapEncoder { QualityLevel = quality };
        encoder.Frames.Add(BitmapFrame.Create(image));
        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
        encoder.Save(stream);
    }
}
