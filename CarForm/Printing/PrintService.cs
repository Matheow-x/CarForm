using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
using CarForm.Core.Helpers;
using CarForm.Core.Persian;
using CarForm.Models;
using CarForm.Services;
using CarForm.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace CarForm.Printing;

public class PrintService : IPrintService
{
    private readonly ICompanyService _companyService;
    private readonly ISettingsService _settings;
    private readonly IImageStore _images;
    private readonly IDialogService _dialogs;
    private readonly IServiceProvider _services;

    public PrintService(ICompanyService companyService, ISettingsService settings, IImageStore images,
        IDialogService dialogs, IServiceProvider services)
    {
        _companyService = companyService;
        _settings = settings;
        _images = images;
        _dialogs = dialogs;
        _services = services;
    }

    public FixedDocument BuildDocument(SalesDocument document)
    {
        // Synchronous by design: rendering happens on the UI thread, so blocking on async
        // database work here would risk a deadlock.
        var company = _companyService.Get();
        var settings = _settings.Print;

        if (settings.UseTemplateOverlay)
        {
            var layout = FormLayout.Load();
            var resolver = new DocumentFieldResolver(document, company, settings.UsePersianDigits);
            return new TemplateFormRenderer(document, company, settings, _images, layout, resolver.Resolve).Build();
        }

        return new DocumentFormRenderer(document, company, settings, _images).Build();
    }

    public void Preview(SalesDocument document)
    {
        var window = _services.GetRequiredService<PrintPreviewWindow>();
        var viewModel = _services.GetRequiredService<PrintPreviewViewModel>();
        viewModel.Load(document);
        window.DataContext = viewModel;
        window.Owner = System.Windows.Application.Current?.MainWindow;
        window.Show();
    }

    public bool Print(SalesDocument document)
    {
        try
        {
            var fixedDocument = BuildDocument(document);
            var dialog = new PrintDialog
            {
                PageRangeSelection = PageRangeSelection.AllPages,
                UserPageRangeEnabled = false
            };

            if (dialog.ShowDialog() != true) return false;

            var paginator = ((IDocumentPaginatorSource)fixedDocument).DocumentPaginator;
            dialog.PrintDocument(paginator, $"سند فروش {document.DocumentNumber}");
            return true;
        }
        catch (Exception exception)
        {
            _dialogs.ShowError(exception, "خطا در چاپ");
            return false;
        }
    }

    public async Task<string?> ExportPdfAsync(SalesDocument document, string? filePath = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                AppPaths.EnsureFolders();
                var safeNumber = string.Join('_', (document.DocumentNumber ?? string.Empty).Split(Path.GetInvalidFileNameChars()));
                var name = string.IsNullOrWhiteSpace(safeNumber) ? "document" : safeNumber;
                filePath = Path.Combine(AppPaths.ExportsFolder, $"{name}-{DateTime.Now:yyyyMMdd-HHmmss}.pdf");
            }

            await Task.Run(() =>
            {
                var fixedDocument = BuildDocument(document);
                FixedDocumentExporter.SavePdf(fixedDocument, filePath!, _settings.Print.PdfDpi,
                    $"سند فروش {document.DocumentNumber}");
            });

            return filePath;
        }
        catch (Exception exception)
        {
            _dialogs.ShowError(exception, "خطا در ایجاد فایل PDF");
            return null;
        }
    }
}
