using System;
using System.IO;

namespace CarForm.Core.Helpers;

/// <summary>All file-system locations used by the application (single-user, offline).</summary>
public static class AppPaths
{
    public static readonly string AppDataFolder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CarForm");

    public static readonly string DatabaseFolder = Path.Combine(AppDataFolder, "Data");
    public static readonly string ImagesFolder = Path.Combine(AppDataFolder, "Images");
    public static readonly string BackupsFolder = Path.Combine(AppDataFolder, "Backups");
    public static readonly string ExportsFolder = Path.Combine(AppDataFolder, "Exports");

    public static string DatabasePath => Path.Combine(DatabaseFolder, "carform.db");

    public static string ConnectionString => $"Data Source={DatabasePath}";

    public static void EnsureFolders()
    {
        Directory.CreateDirectory(AppDataFolder);
        Directory.CreateDirectory(DatabaseFolder);
        Directory.CreateDirectory(ImagesFolder);
        Directory.CreateDirectory(BackupsFolder);
        Directory.CreateDirectory(ExportsFolder);
    }

    /// <summary>Resolves an image path stored in the database (relative or absolute) to an absolute path.</summary>
    public static string? ResolveImage(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        return Path.IsPathRooted(path) ? path : Path.Combine(AppDataFolder, path);
    }
}
