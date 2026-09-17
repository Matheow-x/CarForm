using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Input;
using CarForm.Core.Mvvm;
using CarForm.Models;
using CarForm.Printing;
using CarForm.Services;

namespace CarForm.ViewModels;

public sealed class PrintPreviewViewModel : ViewModelBase
{
    private readonly IPrintService _printService;
    private readonly IDialogService _dialogs;

    private FixedDocument? _document;
    private SalesDocument? _salesDocument;

    public PrintPreviewViewModel(IPrintService printService, IDialogService dialogs)
    {
        _printService = printService;
        _dialogs = dialogs;

        PrintCommand = new RelayCommand(Print, () => _salesDocument is not null);
        ExportPdfCommand = new AsyncRelayCommand(ExportPdfAsync, () => _salesDocument is not null);
        CloseCommand = new RelayCommand(() => Close(true));
    }

    public FixedDocument? Document
    {
        get => _document;
        private set => SetProperty(ref _document, value);
    }

    public ICommand PrintCommand { get; }
    public ICommand ExportPdfCommand { get; }
    public ICommand CloseCommand { get; }

    public void Load(SalesDocument document)
    {
        _salesDocument = document;
        Document = _printService.BuildDocument(document);
        ((RelayCommand)PrintCommand).RaiseCanExecuteChanged();
        ((AsyncRelayCommand)ExportPdfCommand).RaiseCanExecuteChanged();
    }

    private void Print()
    {
        if (_salesDocument is null) return;
        _printService.Print(_salesDocument);
    }

    private async Task ExportPdfAsync()
    {
        if (_salesDocument is null) return;

        var path = _dialogs.SaveFile("فایل PDF|*.pdf", $"document-{_salesDocument.DocumentNumber}.pdf", "ذخیره PDF");
        if (string.IsNullOrEmpty(path)) return;

        var result = await _printService.ExportPdfAsync(_salesDocument, path);
        if (!string.IsNullOrEmpty(result))
        {
            _dialogs.ShowInfo($"فایل PDF با موفقیت ایجاد شد:\n{result}", "خروجی PDF");
        }
    }
}
