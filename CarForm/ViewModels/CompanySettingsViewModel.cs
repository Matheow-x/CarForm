using System;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using CarForm.Core.Mvvm;
using CarForm.Models;
using CarForm.Services;

namespace CarForm.ViewModels;

/// <summary>Single company profile used as the issuer (seller) of every printed document.</summary>
public sealed class CompanySettingsViewModel : ViewModelBase, IPageViewModel
{
    private readonly ICompanyService _companyService;
    private readonly IDialogService _dialogs;
    private readonly IImageStore _images;
    private readonly IAuditService _audit;

    private Company _company = new();
    private BitmapImage? _logo;

    public CompanySettingsViewModel(ICompanyService companyService, IDialogService dialogs, IImageStore images, IAuditService audit)
    {
        _companyService = companyService;
        _dialogs = dialogs;
        _images = images;
        _audit = audit;

        PickLogoCommand = new RelayCommand(PickLogo);
        ClearLogoCommand = new RelayCommand(ClearLogo);
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        ReloadCommand = new AsyncRelayCommand(LoadAsync);
    }

    public string PageKey => PageKeys.Company;
    public string Title => "تنظیمات شرکت";
    public string Icon => "\uE731";
    public int Order => 5;

    public Company Company
    {
        get => _company;
        private set
        {
            if (!SetProperty(ref _company, value)) return;
            _logo = null;
            OnPropertyChanged(nameof(Logo));
        }
    }

    public BitmapImage? Logo => _logo ??= _images.Load(Company.LogoPath, 300);

    public ICommand PickLogoCommand { get; }
    public ICommand ClearLogoCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand ReloadCommand { get; }

    public Task OnNavigatedAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            Company = await _companyService.GetAsync();
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

    private void PickLogo()
    {
        var path = _dialogs.OpenImageFile("انتخاب لوگوی شرکت");
        if (string.IsNullOrEmpty(path)) return;

        var stored = _images.Save(path, ImageKind.Logo);
        if (stored is null)
        {
            _dialogs.ShowError("امکان بارگذاری این تصویر وجود ندارد.");
            return;
        }

        Company.LogoPath = stored;
        _logo = null;
        OnPropertyChanged(nameof(Logo));
    }

    private void ClearLogo()
    {
        Company.LogoPath = null;
        _logo = null;
        OnPropertyChanged(nameof(Logo));
    }

    public override bool ValidateAll()
    {
        ClearAllErrors();
        Validate("Name", !string.IsNullOrWhiteSpace(Company.Name), "نام شرکت الزامی است.");
        return !HasErrors;
    }

    private async Task SaveAsync()
    {
        if (!ValidateAll())
        {
            _dialogs.ShowWarning("لطفاً فیلدهای ضروری را تکمیل کنید.", "اعتبارسنجی");
            return;
        }

        try
        {
            await _companyService.SaveAsync(Company);
            _dialogs.ShowInfo("اطلاعات شرکت با موفقیت ذخیره شد.", "ذخیره");
        }
        catch (Exception exception)
        {
            _dialogs.ShowError(exception, "خطا در ذخیره‌سازی");
        }
    }
}
