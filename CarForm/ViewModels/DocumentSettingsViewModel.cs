using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using CarForm.Core.Mvvm;
using CarForm.Core.Persian;
using CarForm.Models;
using CarForm.Printing;
using CarForm.Services;

namespace CarForm.ViewModels;

public sealed record ColorPreset(string Name, string Hex);

/// <summary>Printing / template settings. Every value here affects the rendered form only - never the data.</summary>
public sealed class DocumentSettingsViewModel : ViewModelBase, IPageViewModel
{
    private readonly ISettingsService _settings;
    private readonly System.IServiceProvider _services;
    private readonly IDialogService _dialogs;
    private readonly IImageStore _images;
    private readonly IPrintService _printService;

    private PrintSettings _settings2 = new();

    public DocumentSettingsViewModel(ISettingsService settings, System.IServiceProvider services, IDialogService dialogs,
        IImageStore images, IPrintService printService)
    {
        _settings = settings;
        _services = services;
        _dialogs = dialogs;
        _images = images;
        _printService = printService;

        Fonts = new ObservableCollection<string>(
            System.Windows.Media.Fonts.SystemFontFamilies
                .Select(f => f.Source)
                .Where(s => !string.IsNullOrWhiteSpace(s) && !s.Contains('#'))
                .OrderBy(s => s, StringComparer.CurrentCultureIgnoreCase));

        Presets = new List<ColorPreset>
        {
            new("سرمه‌ای", "#1F3A5F"),
            new("آبی", "#2563EB"),
            new("فیروزه‌ای", "#0E7490"),
            new("سبز", "#15803D"),
            new("زرشکی", "#9F1239"),
            new("بنفش", "#6D28D9"),
            new("ذغالی", "#374151"),
            new("مشکی", "#111827")
        };

        AccentPresets = new List<ColorPreset>
        {
            new("طلایی", "#C9A227"),
            new("نارنجی", "#EA580C"),
            new("قرمز", "#DC2626"),
            new("سبز", "#16A34A"),
            new("آبی روشن", "#38BDF8"),
            new("خاکستری", "#94A3B8")
        };

        Settings = _settings.Print.Clone();

        SaveCommand = new AsyncRelayCommand(SaveAsync);
        ResetCommand = new RelayCommand(Reset);
        ReloadCommand = new RelayCommand(Reload);
        PickTemplateCommand = new RelayCommand(PickTemplate);
        ClearTemplateCommand = new RelayCommand(ClearTemplate);
        PreviewSampleCommand = new RelayCommand(PreviewSample);
        ResetLayoutCommand = new RelayCommand(ResetLayout);
        OpenLayoutFileCommand = new RelayCommand(OpenLayoutFile);
    }

    public string PageKey => PageKeys.DocumentSettings;
    public string Title => "تنظیمات سند";
    public string Icon => "\uE8A1";
    public int Order => 6;

    public PrintSettings Settings
    {
        get => _settings2;
        private set => SetProperty(ref _settings2, value);
    }

    public ObservableCollection<string> Fonts { get; }

    public IReadOnlyList<ColorPreset> Presets { get; }

    public IReadOnlyList<ColorPreset> AccentPresets { get; }

    public ICommand SaveCommand { get; }
    public ICommand ResetCommand { get; }
    public ICommand ReloadCommand { get; }
    public ICommand PickTemplateCommand { get; }
    public ICommand ClearTemplateCommand { get; }
    public ICommand PreviewSampleCommand { get; }
    public ICommand ResetLayoutCommand { get; }
    public ICommand OpenLayoutFileCommand { get; }

    public Task OnNavigatedAsync()
    {
        Reload();
        return Task.CompletedTask;
    }

    private void Reload() => Settings = _settings.Print.Clone();

    private void Reset()
    {
        Settings = new PrintSettings();
        _dialogs.ShowInfo("تنظیمات چاپ به مقادیر پیش‌فرض بازگردانده شد. برای ثبت، دکمه ذخیره را بزنید.", "بازنشانی");
    }

    private void PickTemplate()
    {
        var path = _dialogs.OpenImageFile("انتخاب تصویر قالب چاپ (A4)");
        if (string.IsNullOrEmpty(path)) return;

        var stored = _images.Save(path, ImageKind.Template);
        if (stored is null)
        {
            _dialogs.ShowError("امکان بارگذاری این تصویر وجود ندارد.");
            return;
        }

        Settings.TemplateImagePath = stored;
        Settings.PrintTemplateBackground = true;
    }

    private void ClearTemplate()
    {
        Settings.TemplateImagePath = null;
        Settings.PrintTemplateBackground = false;
    }

    private void PreviewSample()
    {
        var sample = new SalesDocument
        {
            DocumentNumber = "1403/0001",
            DocumentDate = DateTime.Today,
            BuyerName = "نمونه: امیر کریمی",
            BuyerNationalCode = "0012345678",
            BuyerPhone = "02100000000",
            BuyerAddress = "تهران - آدرس نمونه",
            VehicleMake = "Toyota",
            VehicleModel = "کرولا",
            VehicleTrim = "SE",
            VehicleYear = "1398",
            VehicleColor = "سفید",
            VehicleVin = "JTDBR32E120123456",
            VehiclePlate = "۱۲ب۳۴۵-۱۱",
            VehicleMileage = 85000,
            TaxReceiptNo = "12345",
            TaxReceiptDate = DateTime.Today,
            TaxPaymentId = "98765",
            TollReceiptNo = "54321",
            TollReceiptDate = DateTime.Today,
            TollPaymentId = "11223",
            InsurancePolicyNo = "99887766",
            InsuranceCompany = "بیمه نمونه",
            InsuranceIssueDate = DateTime.Today,
            InsuranceExpiryDate = DateTime.Today.AddYears(1),
            NoteColorChanged = false,
            NoteHasCabin = true,
            NoteIsUsed = true,
            Notes = "این یک پیش‌نمایش نمونه از تنظیمات چاپ است."
        };

        sample.Items.Add(new DocumentLineItem { Title = "حق‌الثبت و نقل و انتقال", Amount = 1250000, SortOrder = 0 });
        sample.Items.Add(new DocumentLineItem { Title = "عوارض شهرداری", Amount = 450000, SortOrder = 1 });
        sample.Items.Add(new DocumentLineItem { Title = "بیمه شخص ثالث", Amount = 3200000, SortOrder = 2 });

        // Preview with the settings currently being edited (without persisting them).
        using (_settings.OverridePrint(Settings))
        {
            _printService.Preview(sample);
        }
    }

    private void ResetLayout()
    {
        if (!_dialogs.ShowConfirm("جعبه‌های محل درج مقادیر به حالت پیش‌فرض بازگردانده می‌شوند. ادامه می‌دهید؟", "بازنشانی چیدمان"))
        {
            return;
        }

        FormLayout.Reset();
        _dialogs.ShowInfo($"چیدمان فرم بازنشانی شد.\nفایل تنظیمات:\n{FormLayout.FilePath}", "بازنشانی چیدمان");
    }

    private void OpenLayoutFile()
    {
        try
        {
            var editor = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<PrintLayoutEditorViewModel>(_services);
            _dialogs.ShowDialog(editor);
        }
        catch (Exception exception)
        {
            _dialogs.ShowError(exception);
        }
    }

    private async Task SaveAsync()
    {
        try
        {
            if (!ValidateAll())
            {
                _dialogs.ShowWarning("برخی مقادیر وارد شده معتبر نیستند.", "اعتبارسنجی");
                return;
            }

            await _settings.SavePrintAsync(Settings);
            _dialogs.ShowInfo("تنظیمات چاپ ذخیره شد.", "ذخیره");
        }
        catch (Exception exception)
        {
            _dialogs.ShowError(exception, "خطا در ذخیره تنظیمات");
        }
    }

    public override bool ValidateAll()
    {
        ClearAllErrors();

        var fontSizeOk = Settings.FontSize is >= 6 and <= 24;
        Validate("FontSize", fontSizeOk, "اندازه قلم باید بین ۶ و ۲۴ باشد.");

        var titleOk = Settings.TitleFontSize is >= 8 and <= 36;
        Validate("TitleFontSize", titleOk, "اندازه عنوان باید بین ۸ و ۳۶ باشد.");

        var marginOk = Settings.PageMarginMm is >= 0 and <= 40;
        Validate("PageMarginMm", marginOk, "حاشیه صفحه باید بین ۰ و ۴۰ میلی‌متر باشد.");

        var rowsOk = Settings.MaxInvoiceRows is >= 3 and <= 40;
        Validate("MaxInvoiceRows", rowsOk, "تعداد ردیف‌های فاکتور باید بین ۳ و ۴۰ باشد.");

        var dpiOk = Settings.PdfDpi is >= 96 and <= 600;
        Validate("PdfDpi", dpiOk, "وضوح خروجی PDF باید بین ۹۶ و ۶۰۰ باشد.");

        var fontOk = Fonts.Contains(Settings.FontFamily) || string.IsNullOrWhiteSpace(Settings.FontFamily);
        Validate("FontFamily", fontOk, "قلم انتخاب شده در ویندوز یافت نشد.");

        return !HasErrors;
    }
}
