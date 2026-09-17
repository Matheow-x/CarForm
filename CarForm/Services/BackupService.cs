using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CarForm.Core.Helpers;
using CarForm.Data;
using CarForm.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CarForm.Services;

public class BackupService : IBackupService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly IAuditService _audit;

    public BackupService(IDbContextFactory<AppDbContext> factory, IAuditService audit)
    {
        _factory = factory;
        _audit = audit;
    }

    public async Task<string> BackupAsync(string? folder = null)
    {
        AppPaths.EnsureFolders();
        var targetFolder = string.IsNullOrWhiteSpace(folder)
            ? AppPaths.BackupsFolder
            : folder;

        if (!Directory.Exists(targetFolder)) Directory.CreateDirectory(targetFolder);

        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        var target = Path.Combine(targetFolder, $"carform-backup-{stamp}.db");
        var counter = 1;
        while (File.Exists(target))
        {
            target = Path.Combine(targetFolder, $"carform-backup-{stamp}-{counter++}.db");
        }

        await CopyDatabaseAsync(target, useVacuum: true);

        await _audit.LogAsync(AuditEntityType.Database, AuditAction.Create, null,
            $"تهیه نسخه پشتیبان از پایگاه داده در مسیر {target}");

        return target;
    }

    public async Task<string> CreateSnapshotAsync(string reason)
    {
        AppPaths.EnsureFolders();
        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        var target = Path.Combine(AppPaths.BackupsFolder, $"carform-{reason}-{stamp}.db");
        await CopyDatabaseAsync(target, useVacuum: false);
        return target;
    }

    public async Task<string> RestoreAsync(string backupFilePath)
    {
        if (string.IsNullOrWhiteSpace(backupFilePath) || !File.Exists(backupFilePath))
            throw new FileNotFoundException("فایل پشتیبان انتخاب شده یافت نشد.", backupFilePath);

        if (!IsValidDatabase(backupFilePath))
            throw new InvalidOperationException("فایل انتخاب شده یک پایگاه داده معتبر از این برنامه نیست.");

        AppPaths.EnsureFolders();

        // 1) keep a safety copy of the current database before overwriting it
        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        var safety = Path.Combine(AppPaths.BackupsFolder, $"carform-before-restore-{stamp}.db");
        if (File.Exists(AppPaths.DatabasePath))
        {
            SqliteConnection.ClearAllPools();
            File.Copy(AppPaths.DatabasePath, safety, true);
        }

        // 2) replace the live database file
        SqliteConnection.ClearAllPools();
        File.Copy(backupFilePath, AppPaths.DatabasePath, true);

        await _audit.LogAsync(AuditEntityType.Database, AuditAction.Update, null,
            $"بازیابی پایگاه داده از فایل {Path.GetFileName(backupFilePath)}",
            $"نسخه پشتیبان پیش از بازیابی: {safety}");

        return safety;
    }

    public List<BackupInfo> GetBackups()
    {
        if (!Directory.Exists(AppPaths.BackupsFolder)) return new List<BackupInfo>();

        return Directory.GetFiles(AppPaths.BackupsFolder, "*.db")
            .Select(path => new FileInfo(path))
            .OrderByDescending(f => f.CreationTime)
            .Select(f => new BackupInfo(f.FullName, f.Name, f.Length, f.CreationTime))
            .ToList();
    }

    private async Task CopyDatabaseAsync(string target, bool useVacuum)
    {
        if (useVacuum)
        {
            try
            {
                await using var db = await _factory.CreateDbContextAsync();
                // VACUUM INTO produces a consistent, de-fragmented copy even while the app is running.
                await db.Database.ExecuteSqlRawAsync("VACUUM INTO {0}", target);
                if (File.Exists(target)) return;
            }
            catch
            {
                // fall back to a plain file copy
            }
        }

        SqliteConnection.ClearAllPools();
        File.Copy(AppPaths.DatabasePath, target, true);
    }

    private static bool IsValidDatabase(string path)
    {
        try
        {
            using var connection = new SqliteConnection($"Data Source={path};Mode=ReadOnly");
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name IN ('SalesDocuments','Owners','Vehicles')";
            using var reader = command.ExecuteReader();
            var found = 0;
            while (reader.Read()) found++;
            return found >= 3;
        }
        catch
        {
            return false;
        }
    }
}
