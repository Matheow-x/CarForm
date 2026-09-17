using System.Windows.Media.Imaging;

namespace CarForm.Services;

public enum ImageKind
{
    /// <summary>Company logo (kept small, transparency preserved).</summary>
    Logo = 0,

    /// <summary>Owner photo.</summary>
    Photo = 1,

    /// <summary>Full page print template background (kept at print resolution).</summary>
    Template = 2
}

public interface IImageStore
{
    /// <summary>Copies, resizes and compresses an image into the app data folder.</summary>
    /// <returns>The stored path relative to the application data folder, or null on failure.</returns>
    string? Save(string sourcePath, ImageKind kind);

    void Delete(string? relativePath);

    BitmapImage? Load(string? relativePath, int decodeWidth = 0);

    bool Exists(string? relativePath);
}
