using System.Collections.Generic;
using System.Threading.Tasks;

namespace CarForm.Services;

public sealed record BackupInfo(string Path, string FileName, long SizeBytes, System.DateTime CreatedAt)
{
    public string CreatedAtJalali => CarForm.Core.Persian.JalaliDate.FormatWithTime(CreatedAt, true);
}

public interface IBackupService
{
    /// <summary>Creates a timestamped copy of the SQLite database in the given folder.</summary>
    Task<string> BackupAsync(string? folder = null);

    /// <summary>
    /// Replaces the current database with the selected backup file.
    /// The previous database is copied next to the backups first; the user must restart the app.
    /// </summary>
    Task<string> RestoreAsync(string backupFilePath);

    List<BackupInfo> GetBackups();

    Task<string> CreateSnapshotAsync(string reason);
}
