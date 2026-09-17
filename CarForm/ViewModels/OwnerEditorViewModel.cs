using System;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using CarForm.Core.Mvvm;
using CarForm.Core.Persian;
using CarForm.Models;
using CarForm.Services;

namespace CarForm.ViewModels;

/// <summary>Add / edit dialog for an owner (individual or legal entity) including photo / logo.</summary>
public sealed class OwnerEditorViewModel : ViewModelBase
{
    private readonly IOwnerService _owners;
    private readonly IDialogService _dialogs;
    private readonly IImageStore _images;
    private readonly bool _isNew;

    private BitmapImage? _image;
    private string? _birthDateJalali;
    private string? _issueDateJalali;

    public OwnerEditorViewModel(IOwnerService owners, IDialogService dialogs, IImageStore images, Owner? owner)
    {
        _owners = owners;
        _dialogs = dialogs;
        _images = images;

        _isNew = owner is null;
        Owner = owner ?? new Owner { Type = OwnerType.Individual };

        Title = _isNew ? "مالک جدید" : $"ویرایش {Owner.DisplayName}";
        BirthDateJalali = JalaliDate.Format(Owner.BirthDate);
        IssueDateJalali = JalaliDate.Format(Owner.IssueDate);

        PickImageCommand = new RelayCommand(PickImage);
        ClearImageCommand = new RelayCommand(ClearImage, () => !string.IsNullOrWhiteSpace(Owner.ImagePath));
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        CancelCommand = new RelayCommand(() => Close(false));
    }

    public Owner Owner { get; }

    private string _title = string.Empty;
    public string Title { get => _title; set => SetProperty(ref _title, value); }

    public bool IsIndividual
    {
        get => Owner.Type == OwnerType.Individual;
        set
        {
            if (value) Owner.Type = OwnerType.Individual;
            OnPropertyChanged(nameof(IsIndividual));
            OnPropertyChanged(nameof(IsLegalEntity));
        }
    }

    public bool IsLegalEntity
    {
        get => Owner.Type == OwnerType.LegalEntity;
        set
        {
            if (value) Owner.Type = OwnerType.LegalEntity;
            OnPropertyChanged(nameof(IsIndividual));
            OnPropertyChanged(nameof(IsLegalEntity));
        }
    }

    public string BirthDateJalali
    {
        get => _birthDateJalali ?? string.Empty;
        set
        {
            if (!SetProperty(ref _birthDateJalali, value)) return;
            if (string.IsNullOrWhiteSpace(value))
            {
                Owner.BirthDate = null;
                Validate(nameof(BirthDateJalali), true, string.Empty);
            }
            else if (JalaliDate.TryParse(value, out var date))
            {
                Owner.BirthDate = date;
                Validate(nameof(BirthDateJalali), true, string.Empty);
            }
            else
            {
                Validate(nameof(BirthDateJalali), false, "قالب تاریخ تولد صحیح نیست (نمونه: 1365/04/12).");
            }
        }
    }

    /// <summary>تاریخ صدور (شمسی)</summary>
    public string IssueDateJalali
    {
        get => _issueDateJalali ?? string.Empty;
        set
        {
            if (!SetProperty(ref _issueDateJalali, value)) return;
            if (string.IsNullOrWhiteSpace(value))
            {
                Owner.IssueDate = null;
                Validate(nameof(IssueDateJalali), true, string.Empty);
            }
            else if (JalaliDate.TryParse(value, out var date))
            {
                Owner.IssueDate = date;
                Validate(nameof(IssueDateJalali), true, string.Empty);
            }
            else
            {
                Validate(nameof(IssueDateJalali), false, "قالب تاریخ صدور صحیح نیست (نمونه: 1365/04/12).");
            }
        }
    }

    public BitmapImage? Image => _image ??= _images.Load(Owner.ImagePath, 320);

    public ICommand PickImageCommand { get; }
    public ICommand ClearImageCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    private void PickImage()
    {
        var path = _dialogs.OpenImageFile(Owner.Type == OwnerType.LegalEntity ? "انتخاب لوگو" : "انتخاب تصویر");
        if (string.IsNullOrEmpty(path)) return;

        var stored = _images.Save(path, Owner.Type == OwnerType.LegalEntity ? ImageKind.Logo : ImageKind.Photo);
        if (stored is null)
        {
            _dialogs.ShowError("امکان بارگذاری این تصویر وجود ندارد. فرمت‌های png، jpg و bmp پشتیبانی می‌شوند.");
            return;
        }

        if (!string.IsNullOrWhiteSpace(Owner.ImagePath)) _images.Delete(Owner.ImagePath);
        Owner.ImagePath = stored;
        _image = null;
        OnPropertyChanged(nameof(Image));
        ((RelayCommand)ClearImageCommand).RaiseCanExecuteChanged();
    }

    private void ClearImage()
    {
        _images.Delete(Owner.ImagePath);
        Owner.ImagePath = null;
        _image = null;
        OnPropertyChanged(nameof(Image));
        ((RelayCommand)ClearImageCommand).RaiseCanExecuteChanged();
    }

    public override bool ValidateAll()
    {
        ClearAllErrors();

        if (Owner.Type == OwnerType.Individual)
        {
            Validate("FirstName", !string.IsNullOrWhiteSpace(Owner.FirstName), "نام الزامی است.");
            Validate("LastName", !string.IsNullOrWhiteSpace(Owner.LastName), "نام خانوادگی الزامی است.");
        }
        else
        {
            Validate("CompanyName", !string.IsNullOrWhiteSpace(Owner.CompanyName), "نام شرکت الزامی است.");
        }

        if (!HasErrors && !string.IsNullOrWhiteSpace(Owner.NationalCode) && Owner.NationalCode!.Length < 6)
        {
            Validate("NationalCode", false, "کد ملی وارد شده بسیار کوتاه است.");
        }

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
            await _owners.SaveAsync(Owner);
            Close(true);
        }
        catch (Exception exception)
        {
            _dialogs.ShowError(exception, "خطا در ذخیره‌سازی");
        }
    }
}
