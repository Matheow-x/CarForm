using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CarForm.Core.Mvvm;
using CarForm.Core.Persian;
using CarForm.Models;
using CarForm.Printing;
using CarForm.Services;

namespace CarForm.ViewModels;

public sealed class SavedDocumentsViewModel : ViewModelBase, IPageViewModel
{
    private readonly IDocumentService _documents;
    private readonly IDialogService _dialogs;
    private readonly IPrintService _printService;
    private readonly INavigationService _navigation;

    private SalesDocument? _selectedDocument;

    public SavedDocumentsViewModel(IDocumentService documents, IDialogService dialogs,
        IPrintService printService, INavigationService navigation)
    {
        _documents = documents;
        _dialogs = dialogs;
        _printService = printService;
        _navigation = navigation;

        Documents = new ObservableCollection<SalesDocument>();

        SearchCommand = new AsyncRelayCommand(SearchAsync);
        ResetCommand = new AsyncRelayCommand(async () => { ResetFilters(); await SearchAsync(); });
        NewDocumentCommand = new AsyncRelayCommand(NewDocumentAsync);
        EditCommand = new AsyncRelayCommand(EditAsync, () => SelectedDocument is not null);
        DeleteCommand = new AsyncRelayCommand(DeleteAsync, () => SelectedDocument is not null);
        PrintCommand = new RelayCommand(Print, () => SelectedDocument is not null);
        PreviewCommand = new RelayCommand(Preview, () => SelectedDocument is not null);
        ExportPdfCommand = new AsyncRelayCommand(ExportPdfAsync, () => SelectedDocument is not null);
        SetTodayFromCommand = new RelayCommand(() => FromDateJalali = JalaliDate.TodayString);
        SetTodayToCommand = new RelayCommand(() => ToDateJalali = JalaliDate.TodayString);
    }

    public string PageKey => PageKeys.SavedDocuments;
    public string Title => "اسناد ذخیره شده";
    public string Icon => "\uE8A5";
    public int Order => 2;

    #region Filters

    private string? _numberFilter;
    public string? NumberFilter { get => _numberFilter; set => SetProperty(ref _numberFilter, value); }

    private string? _buyerFilter;
    public string? BuyerFilter { get => _buyerFilter; set => SetProperty(ref _buyerFilter, value); }

    private string? _vehicleFilter;
    public string? VehicleFilter { get => _vehicleFilter; set => SetProperty(ref _vehicleFilter, value); }

    private string? _vinFilter;
    public string? VinFilter { get => _vinFilter; set => SetProperty(ref _vinFilter, value); }

    private string? _fromDateJalali;
    public string? FromDateJalali
    {
        get => _fromDateJalali;
        set
        {
            if (!SetProperty(ref _fromDateJalali, value)) return;
            Validate(nameof(FromDateJalali), string.IsNullOrWhiteSpace(value) || JalaliDate.TryParse(value, out _),
                "قالب تاریخ شروع صحیح نیست (نمونه: 1403/01/01).");
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
                "قالب تاریخ پایان صحیح نیست (نمونه: 1403/12/29).");
        }
    }

    #endregion

    public ObservableCollection<SalesDocument> Documents { get; }

    public SalesDocument? SelectedDocument
    {
        get => _selectedDocument;
        set
        {
            if (!SetProperty(ref _selectedDocument, value)) return;
            RaiseCanExecuteChanged();
        }
    }

    public ICommand SearchCommand { get; }
    public ICommand ResetCommand { get; }
    public ICommand NewDocumentCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand PrintCommand { get; }
    public ICommand PreviewCommand { get; }
    public ICommand ExportPdfCommand { get; }
    public ICommand SetTodayFromCommand { get; }
    public ICommand SetTodayToCommand { get; }

    public Task OnNavigatedAsync() => SearchAsync();

    private async Task SearchAsync()
    {
        if (HasErrors) return;

        IsBusy = true;
        try
        {
            var criteria = new DocumentSearchCriteria
            {
                Number = NumberFilter,
                BuyerName = BuyerFilter,
                Vehicle = VehicleFilter,
                Vin = VinFilter,
                FromDate = JalaliDate.ParseOrNull(FromDateJalali),
                ToDate = JalaliDate.EndOfDayOrNull(ToDateJalali)
            };

            var results = await _documents.SearchAsync(criteria, 500);
            Documents.Clear();
            foreach (var document in results) Documents.Add(document);
        }
        catch (Exception exception)
        {
            _dialogs.ShowError(exception, "خطا در جستجو");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ResetFilters()
    {
        NumberFilter = null;
        BuyerFilter = null;
        VehicleFilter = null;
        VinFilter = null;
        FromDateJalali = null;
        ToDateJalali = null;
        ClearAllErrors();
    }

    private async Task NewDocumentAsync()
    {
        await _navigation.OpenDocumentAsync(new SalesDocument { DocumentDate = DateTime.Today });
    }

    private async Task EditAsync()
    {
        if (SelectedDocument is null) return;
        await _navigation.OpenDocumentAsync(SelectedDocument);
    }

    private async Task DeleteAsync()
    {
        if (SelectedDocument is null) return;

        if (!_dialogs.ShowWarningConfirm($"آیا از حذف سند شماره {SelectedDocument.DocumentNumber} اطمینان دارید؟", "حذف سند"))
        {
            return;
        }

        try
        {
            await _documents.DeleteAsync(SelectedDocument.Id);
            await SearchAsync();
        }
        catch (Exception exception)
        {
            _dialogs.ShowError(exception, "خطا در حذف سند");
        }
    }

    private void Print()
    {
        if (SelectedDocument is null) return;
        _printService.Print(SelectedDocument);
    }

    private void Preview()
    {
        if (SelectedDocument is null) return;
        _printService.Preview(SelectedDocument);
    }

    private async Task ExportPdfAsync()
    {
        if (SelectedDocument is null) return;

        var path = _dialogs.SaveFile("فایل PDF|*.pdf", $"document-{SelectedDocument.DocumentNumber}.pdf", "ذخیره PDF");
        if (string.IsNullOrEmpty(path)) return;

        var result = await _printService.ExportPdfAsync(SelectedDocument, path);
        if (!string.IsNullOrEmpty(result))
        {
            _dialogs.ShowInfo($"فایل PDF با موفقیت ایجاد شد:\n{result}", "خروجی PDF");
        }
    }

    private void RaiseCanExecuteChanged()
    {
        ((AsyncRelayCommand)EditCommand).RaiseCanExecuteChanged();
        ((AsyncRelayCommand)DeleteCommand).RaiseCanExecuteChanged();
        ((RelayCommand)PrintCommand).RaiseCanExecuteChanged();
        ((RelayCommand)PreviewCommand).RaiseCanExecuteChanged();
        ((AsyncRelayCommand)ExportPdfCommand).RaiseCanExecuteChanged();
    }
}
