using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using CarForm.Core.Helpers;
using CarForm.Core.Mvvm;
using CarForm.Core.Persian;
using CarForm.Models;
using CarForm.Services;

namespace CarForm.ViewModels;

/// <summary>Audit log: every create / update / delete of owners, vehicles, documents and settings.</summary>
public sealed class ReportsViewModel : ViewModelBase, IPageViewModel
{
    private readonly IAuditService _audit;
    private readonly IDialogService _dialogs;

    public ReportsViewModel(IAuditService audit, IDialogService dialogs)
    {
        _audit = audit;
        _dialogs = dialogs;

        Entries = new ObservableCollection<AuditEntry>();

        EntityTypes = new ObservableCollection<AuditEntityType?>(new AuditEntityType?[] { null }
            .Concat(Enum.GetValues<AuditEntityType>().Cast<AuditEntityType?>()).ToList());

        Actions = new ObservableCollection<AuditAction?>(new AuditAction?[] { null }
            .Concat(Enum.GetValues<AuditAction>().Cast<AuditAction?>()).ToList());

        SearchCommand = new AsyncRelayCommand(SearchAsync);
        ResetCommand = new AsyncRelayCommand(async () => { ResetFilters(); await SearchAsync(); });
        ExportCsvCommand = new RelayCommand(ExportCsv, () => Entries.Count > 0);
        SetTodayFromCommand = new RelayCommand(() => FromDateJalali = JalaliDate.TodayString);
        SetTodayToCommand = new RelayCommand(() => ToDateJalali = JalaliDate.TodayString);
    }

    public string PageKey => PageKeys.Reports;
    public string Title => "گزارش‌ها";
    public string Icon => "\uE9D2";
    public int Order => 7;

    public ObservableCollection<AuditEntry> Entries { get; }
    public ObservableCollection<AuditEntityType?> EntityTypes { get; }
    public ObservableCollection<AuditAction?> Actions { get; }

    private AuditEntityType? _entityTypeFilter;
    public AuditEntityType? EntityTypeFilter
    {
        get => _entityTypeFilter;
        set => SetProperty(ref _entityTypeFilter, value);
    }

    private AuditAction? _actionFilter;
    public AuditAction? ActionFilter
    {
        get => _actionFilter;
        set => SetProperty(ref _actionFilter, value);
    }

    private string? _textFilter;
    public string? TextFilter { get => _textFilter; set => SetProperty(ref _textFilter, value); }

    private string? _fromDateJalali;
    public string? FromDateJalali
    {
        get => _fromDateJalali;
        set
        {
            if (!SetProperty(ref _fromDateJalali, value)) return;
            Validate(nameof(FromDateJalali), string.IsNullOrWhiteSpace(value) || JalaliDate.TryParse(value, out _),
                "قالب تاریخ شروع صحیح نیست.");
        }
    }

    private string? _toDateJalali;
    public string? ToDateJalali
    {
        get => _toDateJalali;
        set
        {
            if (!SetProperty(ref _toDateJalali, value)) return;
            Validate(nameof(ToDateJalali), string.IsNullOrWhiteSpace(value) || JalaliDate.TryParse(value, out _),
                "قالب تاریخ پایان صحیح نیست.");
        }
    }

    private int _resultCount;
    public int ResultCount { get => _resultCount; set => SetProperty(ref _resultCount, value); }

    public ICommand SearchCommand { get; }
    public ICommand ResetCommand { get; }
    public ICommand ExportCsvCommand { get; }
    public ICommand SetTodayFromCommand { get; }
    public ICommand SetTodayToCommand { get; }

    public Task OnNavigatedAsync() => SearchAsync();

    private async Task SearchAsync()
    {
        if (HasErrors) return;

        IsBusy = true;
        try
        {
            var results = await _audit.SearchAsync(EntityTypeFilter, ActionFilter,
                JalaliDate.ParseOrNull(FromDateJalali), JalaliDate.EndOfDayOrNull(ToDateJalali), TextFilter, 2000);

            Entries.Clear();
            foreach (var entry in results) Entries.Add(entry);

            ResultCount = Entries.Count;
            ((RelayCommand)ExportCsvCommand).RaiseCanExecuteChanged();
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

    private void ResetFilters()
    {
        EntityTypeFilter = null;
        ActionFilter = null;
        TextFilter = null;
        FromDateJalali = null;
        ToDateJalali = null;
        ClearAllErrors();
    }

    private void ExportCsv()
    {
        var path = _dialogs.SaveFile("فایل CSV|*.csv", $"audit-{DateTime.Now:yyyyMMdd-HHmmss}.csv", "خروجی گزارش");
        if (string.IsNullOrEmpty(path)) return;

        try
        {
            var builder = new StringBuilder();
            builder.AppendLine("موجودیت,عملیات,زمان (شمسی),شناسه,خلاصه");
            foreach (var entry in Entries)
            {
                builder.AppendLine(string.Join(',',
                    Quote(entry.EntityTypeName),
                    Quote(entry.ActionName),
                    Quote(entry.JalaliTimestamp),
                    entry.EntityId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                    Quote(entry.Summary)));
            }

            File.WriteAllText(path, builder.ToString(), new UTF8Encoding(true));
            _dialogs.ShowInfo($"گزارش با موفقیت ذخیره شد:\n{path}", "خروجی");
        }
        catch (Exception exception)
        {
            _dialogs.ShowError(exception, "خطا در ایجاد فایل گزارش");
        }
    }

    private static string Quote(string value) => $"\"{(value ?? string.Empty).Replace("\"", "'")}\"";
}
