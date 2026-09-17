using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using CarForm.Core.Helpers;
using CarForm.Core.Mvvm;
using CarForm.Core.Persian;
using CarForm.Data;
using CarForm.Models;
using CarForm.Services;

namespace CarForm.ViewModels;

/// <summary>Backup / restore, numbering and general application settings.</summary>
public sealed class AppSettingsViewModel : ViewModelBase, IPageViewModel
{
    private readonly ISettingsService _settings;
    private readonly IBackupService _backup;
    private readonly IDialogService _dialogs;
    private readonly IThemeManager _themeManager;
    private readonly IDatabaseInitializer _initializer;

    public AppSettingsViewModel(ISettingsService settings, IBackupService backup, IDialogService dialogs,
        IThemeManager themeManager, IDatabaseInitializer initializer)
    {
        _settings = settings;
        _backup = backup;
        _dialogs = dialogs;
        _themeManager = themeManager;
        _initializer = initializer;

        Themes = new ObservableCollection<ThemeMode>(Enum.GetValues<ThemeMode>());

        Backups = new ObservableCollection<BackupInfo>();

        BackupToFolderCommand = new AsyncRelayCommand(BackupToFolderAsync);
        BackupNowCommand = new AsyncRelayCommand(BackupNowAsync);
        RestoreCommand = new AsyncRelayCommand(RestoreAsync);
        RefreshBackupsCommand = new RelayCommand(LoadBackups);
        OpenDataFolderCommand = new RelayCommand(OpenDataFolder);
        OpenBackupFolderCommand = new RelayCommand(OpenBackupFolder);
        SaveNumberingCommand = new AsyncRelayCommand(SaveNumberingAsync);
        SeedDemoDataCommand = new AsyncRelayCommand(SeedDemoDataAsync);
        OpenBackupFileCommand = new RelayCommand<BackupInfo?>(OpenBackupFile);
        OpenSelectedBackupCommand = new RelayCommand(() => OpenBackupFile(SelectedBackup));
    }

    public string PageKey => PageKeys.AppSettings;
    public string Title => "تنظیمات برنامه";
    public string Icon => "\uE713";
    public int Order => 8;

    #region Theme and general

    public ObservableCollection<ThemeMode> Themes { get; }

    public ThemeMode Theme
    {
        get => _settings.App.Theme;
        set
        {
            if (_settings.App.Theme == value) return;
            _settings.App.Theme = value;
            _themeManager.Apply(value);
            OnPropertyChanged(nameof(Theme));
            _ = _settings.SaveAppAsync(_settings.App);
        }
    }

    public bool UsePersianDigitsInUi
    {
        get => _settings.App.UsePersianDigitsInUi;
        set
        {
            if (_settings.App.UsePersianDigitsInUi == value) return;
            _settings.App.UsePersianDigitsInUi = value;
            OnPropertyChanged(nameof(UsePersianDigitsInUi));
            _ = _settings.SaveAppAsync(_settings.App);
        }
    }

    #endregion

    #region Numbering

    public bool AutoNumbering
    {
        get => _settings.App.AutoNumbering;
        set
        {
            if (_settings.App.AutoNumbering == value) return;
            _settings.App.AutoNumbering = value;
            OnPropertyChanged(nameof(AutoNumbering));
        }
    }

    public string NumberFormat
    {
        get => _settings.App.NumberFormat;
        set
        {
            if (_settings.App.NumberFormat == value) return;
            _settings.App.NumberFormat = value;
            OnPropertyChanged(nameof(NumberFormat));
            OnPropertyChanged(nameof(NumberSample));
        }
    }

    public int LastNumber => _settings.App.LastNumber;

    public string NumberSample => NumberingService.Format(NumberFormat, Math.Max(1, LastNumber + 1), DateTime.Today);

    public string NumberFormatHelp => "الگوهای قابل استفاده: {seq}، {seq:D4}، {yyyy}، {yy}، {mm}، {dd} (تاریخ شمسی سند)";

    #endregion

    #region Backup / restore

    public ObservableCollection<BackupInfo> Backups { get; }

    private BackupInfo? _selectedBackup;
    public BackupInfo? SelectedBackup
    {
        get => _selectedBackup;
        set => SetProperty(ref _selectedBackup, value);
    }

    public string DatabasePath => AppPaths.DatabasePath;

    public string DatabaseSizeText
    {
        get
        {
            try
            {
                var info = new FileInfo(AppPaths.DatabasePath);
                return info.Exists
                    ? $"{info.Length / 1024.0 / 1024.0:0.00} مگابایت"
                    : "ایجاد نشده";
            }
            catch
            {
                return "نامشخص";
            }
        }
    }

    public string AppDataPath => AppPaths.AppDataFolder;

    public ICommand BackupNowCommand { get; }
    public ICommand BackupToFolderCommand { get; }
    public ICommand RestoreCommand { get; }
    public ICommand RefreshBackupsCommand { get; }
    public ICommand OpenDataFolderCommand { get; }
    public ICommand OpenBackupFolderCommand { get; }
    public ICommand SaveNumberingCommand { get; }
    public ICommand SeedDemoDataCommand { get; }
    public ICommand OpenBackupFileCommand { get; }
    public ICommand OpenSelectedBackupCommand { get; }

    public Task OnNavigatedAsync()
    {
        LoadBackups();
        OnPropertyChanged(nameof(Theme));
        OnPropertyChanged(nameof(UsePersianDigitsInUi));
        OnPropertyChanged(nameof(AutoNumbering));
        OnPropertyChanged(nameof(NumberFormat));
        OnPropertyChanged(nameof(LastNumber));
        OnPropertyChanged(nameof(NumberSample));
        OnPropertyChanged(nameof(DatabaseSizeText));
        return Task.CompletedTask;
    }

    private void LoadBackups()
    {
        Backups.Clear();
        foreach (var backup in _backup.GetBackups()) Backups.Add(backup);
    }

    private async Task BackupNowAsync()
    {
        try
        {
            IsBusy = true;
            var path = await _backup.BackupAsync(null);
            LoadBackups();
            _dialogs.ShowInfo($"نسخه پشتیبان با موفقیت ایجاد شد:\n{path}", "پشتیبان‌گیری");
        }
        catch (Exception exception)
        {
            _dialogs.ShowError(exception, "خطا در تهیه نسخه پشتیبان");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task BackupToFolderAsync()
    {
        var folder = _dialogs.PickFolder("انتخاب پوشه برای ذخیره نسخه پشتیبان", _settings.App.LastBackupFolder);
        if (string.IsNullOrEmpty(folder)) return;

        try
        {
            IsBusy = true;
            _settings.App.LastBackupFolder = folder!;
            await _settings.SaveAppAsync(_settings.App);

            var path = await _backup.BackupAsync(folder);
            LoadBackups();
            _dialogs.ShowInfo($"نسخه پشتیبان با موفقیت ایجاد شد:\n{path}", "پشتیبان‌گیری");
        }
        catch (Exception exception)
        {
            _dialogs.ShowError(exception, "خطا در تهیه نسخه پشتیبان");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RestoreAsync()
    {
        var file = _dialogs.OpenFile("پایگاه داده SQLite|*.db;*.sqlite;*.sqlite3|همه فایل‌ها|*.*", "انتخاب فایل پشتیبان");
        if (string.IsNullOrEmpty(file)) return;

        if (!_dialogs.ShowWarningConfirm(
                "با بازیابی، پایگاه داده فعلی با فایل انتخاب شده جایگزین می‌شود.\n" +
                "از پایگاه داده فعلی یک نسخه امنیتی تهیه شده و سپس برنامه باید بسته و دوباره اجرا شود.\n\n" +
                "آیا ادامه می‌دهید؟", "بازیابی پایگاه داده"))
        {
            return;
        }

        try
        {
            IsBusy = true;
            var safety = await _backup.RestoreAsync(file!);
            _dialogs.ShowWarning(
                $"بازیابی با موفقیت انجام شد.\nنسخه امنیتی از پایگاه داده قبلی:\n{safety}\n\n" +
                "اکنون برنامه را بسته و دوباره اجرا کنید.", "بازیابی انجام شد");
        }
        catch (Exception exception)
        {
            _dialogs.ShowError(exception, "خطا در بازیابی");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void OpenDataFolder() => OpenFolder(AppPaths.AppDataFolder);

    private void OpenBackupFolder() => OpenFolder(AppPaths.BackupsFolder);

    private void OpenBackupFile(BackupInfo? info)
    {
        if (info is null) return;
        OpenFolder(Path.GetDirectoryName(info.Path) ?? AppPaths.BackupsFolder);
    }

    private static void OpenFolder(string path)
    {
        try
        {
            Directory.CreateDirectory(path);
            Process.Start(new ProcessStartInfo("explorer.exe", path) { UseShellExecute = true });
        }
        catch
        {
            // ignore
        }
    }

    #endregion

    private async Task SaveNumberingAsync()
    {
        try
        {
            await _settings.SaveAppAsync(_settings.App);
            OnPropertyChanged(nameof(NumberSample));
            _dialogs.ShowInfo("تنظیمات شماره‌گذاری ذخیره شد.", "ذخیره");
        }
        catch (Exception exception)
        {
            _dialogs.ShowError(exception);
        }
    }

    private async Task SeedDemoDataAsync()
    {
        if (!_dialogs.ShowConfirm("داده‌های نمونه (۲ مالک و یک خودرو) به پایگاه داده اضافه می‌شود. ادامه می‌دهید؟", "داده نمونه"))
        {
            return;
        }

        try
        {
            IsBusy = true;
            await Task.Run(() => _initializer.SeedDemoData());
            _dialogs.ShowInfo("داده‌های نمونه اضافه شد.", "داده نمونه");
        }
        catch (Exception exception)
        {
            _dialogs.ShowError(exception);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
