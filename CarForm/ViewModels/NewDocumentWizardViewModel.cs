using System;
using System.Collections.Generic;
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

public sealed record WizardStepItem(int Index, string Title, string Description)
{
    /// <summary>1-based number shown in the wizard header.</summary>
    public string Number => (Index + 1).ToString();
}

/// <summary>
/// Step-by-step creation / editing of a sales document.
/// The wizard edits a <see cref="SalesDocument"/> snapshot: picking a vehicle or an owner copies
/// their data into the document once, after which every field stays manually editable.
/// Documents are never locked - an existing document can be reopened and changed at any time.
/// </summary>
public sealed class NewDocumentWizardViewModel : ViewModelBase, IPageViewModel
{
    private readonly IDocumentService _documents;
    private readonly IVehicleService _vehicles;
    private readonly IOwnerService _owners;
    private readonly INumberingService _numbering;
    private readonly ISettingsService _settings;
    private readonly IDialogService _dialogs;
    private readonly IPrintService _printService;

    private SalesDocument _document = new();
    private int _stepIndex;
    private string? _statusMessage;
    private bool _useAutoNumbering;

    public NewDocumentWizardViewModel(IDocumentService documents, IVehicleService vehicles, IOwnerService owners,
        INumberingService numbering, ISettingsService settings, IDialogService dialogs, IPrintService printService)
    {
        _documents = documents;
        _vehicles = vehicles;
        _owners = owners;
        _numbering = numbering;
        _settings = settings;
        _dialogs = dialogs;
        _printService = printService;

        Steps = new ObservableCollection<WizardStepItem>
        {
            new(0, "سربرگ سند", "تاریخ و شماره سند"),
            new(1, "انتخاب خودرو", "جستجو و ثبت مشخصات خودرو"),
            new(2, "انتخاب مالک", "مشخصات خریدار (حقیقی یا حقوقی)"),
            new(3, "رسیدها و بیمه", "مالیات، عوارض و بیمه شخص ثالث"),
            new(4, "فاکتور فروش", "ردیف‌ها و مبلغ کل"),
            new(5, "توضیحات", "یادداشت آزاد و گزینه‌های آماده")
        };

        VehicleResults = new ObservableCollection<Vehicle>();
        OwnerResults = new ObservableCollection<Owner>();
        Items = new ObservableCollection<DocumentLineItem>();

        NextCommand = new RelayCommand(Next, () => !IsLastStep);
        BackCommand = new RelayCommand(Back, () => !IsFirstStep);
        GoToStepCommand = new RelayCommand<int?>(GoToStep);
        SaveCommand = new AsyncRelayCommand(() => SaveAsync(false, false), () => !IsBusy);
        SaveAndPrintCommand = new AsyncRelayCommand(() => SaveAsync(true, false), () => !IsBusy);
        SaveAndExportCommand = new AsyncRelayCommand(() => SaveAsync(false, true), () => !IsBusy);
        NewDocumentCommand = new AsyncRelayCommand(NewDocumentAsync);
        GenerateNumberCommand = new AsyncRelayCommand(GenerateNumberAsync);
        SearchVehiclesCommand = new AsyncRelayCommand(SearchVehiclesAsync);
        ClearVehicleCommand = new RelayCommand(ClearVehicle);
        SearchOwnersCommand = new AsyncRelayCommand(SearchOwnersAsync);
        ClearOwnerCommand = new RelayCommand(ClearOwner);
        AddInvoiceItemCommand = new RelayCommand(AddInvoiceItem);
        RemoveInvoiceItemCommand = new RelayCommand<DocumentLineItem?>(RemoveInvoiceItem);
        AddDefaultInvoiceItemsCommand = new RelayCommand(AddDefaultInvoiceItems);
        PrintCommand = new RelayCommand(() => _printService.Print(Document), () => Document.Id > 0);
        PreviewCommand = new RelayCommand(() => _printService.Preview(Document));
        ExportPdfCommand = new AsyncRelayCommand(ExportPdfAsync);
    }

    #region Page contract

    public string PageKey => PageKeys.NewDocument;
    public string Title => "سند جدید";
    public string Icon => "\uE8A5";
    public int Order => 1;

    public async Task OnNavigatedAsync()
    {
        if (Document.Id == 0 && string.IsNullOrWhiteSpace(Document.DocumentNumber) && Items.Count == 0)
        {
            await NewDocumentAsync();
        }
    }

    #endregion

    #region Document

    public SalesDocument Document
    {
        get => _document;
        private set
        {
            if (SetProperty(ref _document, value))
            {
                OnPropertyChanged(nameof(DocumentDateJalali));
                OnPropertyChanged(nameof(TaxReceiptDateJalali));
                OnPropertyChanged(nameof(TollReceiptDateJalali));
                OnPropertyChanged(nameof(InsuranceIssueDateJalali));
                OnPropertyChanged(nameof(InsuranceExpiryDateJalali));
                OnPropertyChanged(nameof(BuyerBirthDateJalali));
                OnPropertyChanged(nameof(BuyerIssueDateJalali));
                OnPropertyChanged(nameof(ClearanceDateJalali));
                OnPropertyChanged(nameof(IsSaved));
                ((RelayCommand)PrintCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsSaved => Document.Id > 0;

    public string? StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public bool UseAutoNumbering
    {
        get => _useAutoNumbering;
        set => SetProperty(ref _useAutoNumbering, value);
    }

    /// <summary>Jalali proxy for <see cref="SalesDocument.DocumentDate"/>.</summary>
    public string DocumentDateJalali
    {
        get => JalaliDate.Format(Document.DocumentDate);
        set
        {
            if (JalaliDate.TryParse(value, out var date))
            {
                Document.DocumentDate = date;
                Validate(nameof(DocumentDateJalali), true, string.Empty);
            }
            else
            {
                Validate(nameof(DocumentDateJalali), false, "تاریخ سند الزامی است (نمونه: 1403/05/01).");
            }
            OnPropertyChanged(nameof(DocumentDateJalali));
        }
    }

    public string TaxReceiptDateJalali
    {
        get => JalaliDate.Format(Document.TaxReceiptDate);
        set => SetOptionalJalaliDate(value, d => Document.TaxReceiptDate = d, nameof(TaxReceiptDateJalali));
    }

    public string TollReceiptDateJalali
    {
        get => JalaliDate.Format(Document.TollReceiptDate);
        set => SetOptionalJalaliDate(value, d => Document.TollReceiptDate = d, nameof(TollReceiptDateJalali));
    }

    public string InsuranceIssueDateJalali
    {
        get => JalaliDate.Format(Document.InsuranceIssueDate);
        set => SetOptionalJalaliDate(value, d => Document.InsuranceIssueDate = d, nameof(InsuranceIssueDateJalali));
    }

    public string InsuranceExpiryDateJalali
    {
        get => JalaliDate.Format(Document.InsuranceExpiryDate);
        set => SetOptionalJalaliDate(value, d => Document.InsuranceExpiryDate = d, nameof(InsuranceExpiryDateJalali));
    }

    /// <summary>تاریخ صدور شناسنامه خریدار (شمسی)</summary>
    public string BuyerIssueDateJalali
    {
        get => JalaliDate.Format(Document.BuyerIssueDate);
        set => SetOptionalJalaliDate(value, d => Document.BuyerIssueDate = d, nameof(BuyerIssueDateJalali));
    }

    /// <summary>تاریخ تولد خریدار (شمسی)</summary>
    public string BuyerBirthDateJalali
    {
        get => JalaliDate.Format(Document.BuyerBirthDate);
        set => SetOptionalJalaliDate(value, d => Document.BuyerBirthDate = d, nameof(BuyerBirthDateJalali));
    }

    /// <summary>تاریخ ترخیص (شمسی)</summary>
    public string ClearanceDateJalali
    {
        get => JalaliDate.Format(Document.ClearanceDate);
        set => SetOptionalJalaliDate(value, d => Document.ClearanceDate = d, nameof(ClearanceDateJalali));
    }

    private void SetOptionalJalaliDate(string? text, Action<DateTime?> apply, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            apply(null);
            Validate(propertyName, true, string.Empty);
        }
        else if (JalaliDate.TryParse(text, out var date))
        {
            apply(date);
            Validate(propertyName, true, string.Empty);
        }
        else
        {
            Validate(propertyName, false, "قالب تاریخ صحیح نیست (نمونه: 1403/05/01).");
        }
        OnPropertyChanged(propertyName);
    }

    #endregion

    #region Wizard navigation

    public ObservableCollection<WizardStepItem> Steps { get; }

    public int StepIndex
    {
        get => _stepIndex;
        set
        {
            if (SetProperty(ref _stepIndex, Math.Clamp(value, 0, Steps.Count - 1)))
            {
                OnPropertyChanged(nameof(CurrentStep));
                OnPropertyChanged(nameof(IsFirstStep));
                OnPropertyChanged(nameof(IsLastStep));
                ((RelayCommand)NextCommand).RaiseCanExecuteChanged();
                ((RelayCommand)BackCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public WizardStepItem CurrentStep => Steps[StepIndex];

    public bool IsFirstStep => StepIndex == 0;

    public bool IsLastStep => StepIndex == Steps.Count - 1;

    public ICommand NextCommand { get; }
    public ICommand BackCommand { get; }
    public ICommand GoToStepCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand SaveAndPrintCommand { get; }
    public ICommand SaveAndExportCommand { get; }
    public ICommand NewDocumentCommand { get; }
    public ICommand GenerateNumberCommand { get; }
    public ICommand PreviewCommand { get; }
    public ICommand PrintCommand { get; }
    public ICommand ExportPdfCommand { get; }

    private void Next()
    {
        if (!ValidateStep(StepIndex))
        {
            ShowStepError();
            return;
        }
        StepIndex++;
    }

    private void Back() => StepIndex--;

    private void GoToStep(int? index)
    {
        if (!index.HasValue) return;

        var target = Math.Clamp(index.Value, 0, Steps.Count - 1);
        if (target <= StepIndex)
        {
            StepIndex = target;
            return;
        }

        // Moving forward requires every intermediate step to be valid.
        for (var step = StepIndex; step < target; step++)
        {
            if (!ValidateStep(step))
            {
                StepIndex = step;
                ShowStepError();
                return;
            }
        }
        StepIndex = target;
    }

    private void ShowStepError()
        => _dialogs.ShowWarning("لطفاً فیلدهای ضروری این مرحله را تکمیل کنید.", "اعتبارسنجی");

    #endregion

    #region Step 2 - vehicle

    private string? _vehicleSearchTerm;
    public string? VehicleSearchTerm
    {
        get => _vehicleSearchTerm;
        set => SetProperty(ref _vehicleSearchTerm, value);
    }

    public ObservableCollection<Vehicle> VehicleResults { get; }

    private Vehicle? _selectedVehicle;
    public Vehicle? SelectedVehicle
    {
        get => _selectedVehicle;
        set
        {
            if (SetProperty(ref _selectedVehicle, value) && value is not null) ApplyVehicle(value);
        }
    }

    public ICommand SearchVehiclesCommand { get; }
    public ICommand ClearVehicleCommand { get; }

    private async Task SearchVehiclesAsync()
    {
        IsBusy = true;
        try
        {
            var results = await _vehicles.SearchVehiclesAsync(VehicleSearchTerm, null, null, 200);
            VehicleResults.Clear();
            foreach (var vehicle in results) VehicleResults.Add(vehicle);

            if (results.Count == 0)
            {
                StatusMessage = "خودرویی با این مشخصات یافت نشد. می‌توانید مشخصات را به صورت دستی وارد کنید.";
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Copies the fixed specs of a saved vehicle into the document (still editable afterwards).</summary>
    private void ApplyVehicle(Vehicle vehicle)
    {
        Document.VehicleId = vehicle.Id;
        Document.VehicleMake = vehicle.MakeName;
        Document.VehicleModel = vehicle.ModelName;
        Document.VehicleTrim = vehicle.Trim;
        Document.VehicleColor = vehicle.Color;
        Document.VehicleVin = vehicle.Vin;
        Document.VehicleChassisNo = vehicle.ChassisNumber;
        Document.VehicleEngineNo = vehicle.EngineNumber;
        Document.VehiclePlate = vehicle.PlateNumber;
        Document.VehicleFuelType = vehicle.FuelType;
        Document.VehicleBodyType = vehicle.BodyType;
        Document.VehicleMileage = vehicle.Mileage;
        Document.VehicleYear = vehicle.ModelYear?.ToString() ?? string.Empty;
        Document.VehicleSystem = vehicle.System;
        Document.VehicleUsageType = vehicle.UsageType;
        Document.VehicleCylinders = vehicle.Cylinders;
        Document.VehicleAxles = vehicle.Axles;
        Document.VehicleWheels = vehicle.Wheels;
        Document.VehicleCabinNo = vehicle.CabinNo;
        Document.VehicleCapacity = vehicle.Capacity;
        Document.VehicleDisplacement = vehicle.EngineDisplacement;
        Document.VehicleCountry = vehicle.Country;

        StatusMessage = $"مشخصات خودرو {vehicle.FullTitle}در سند درج شد.";
    }

    private void ClearVehicle()
    {
        SelectedVehicle = null;
        Document.VehicleId = null;
        Document.VehicleMake = Document.VehicleModel = Document.VehicleTrim = Document.VehicleColor = null;
        Document.VehicleVin = Document.VehicleChassisNo = Document.VehicleEngineNo = Document.VehiclePlate = null;
        Document.VehicleFuelType = Document.VehicleBodyType = Document.VehicleYear = null;
        Document.VehicleMileage = null;
        Document.VehicleSystem = Document.VehicleUsageType = Document.VehicleCylinders = null;
        Document.VehicleAxles = Document.VehicleWheels = Document.VehicleCabinNo = Document.VehicleCapacity = null;
        Document.VehicleDisplacement = Document.VehicleCountry = null;
    }

    #endregion

    #region Step 3 - owner

    private string? _ownerSearchTerm;
    public string? OwnerSearchTerm
    {
        get => _ownerSearchTerm;
        set => SetProperty(ref _ownerSearchTerm, value);
    }

    public ObservableCollection<Owner> OwnerResults { get; }

    private Owner? _selectedOwner;
    public Owner? SelectedOwner
    {
        get => _selectedOwner;
        set
        {
            if (SetProperty(ref _selectedOwner, value) && value is not null) ApplyOwner(value);
        }
    }

    public ICommand SearchOwnersCommand { get; }
    public ICommand ClearOwnerCommand { get; }

    private async Task SearchOwnersAsync()
    {
        IsBusy = true;
        try
        {
            var results = await _owners.SearchAsync(OwnerSearchTerm, null, 200);
            OwnerResults.Clear();
            foreach (var owner in results) OwnerResults.Add(owner);

            if (results.Count == 0)
            {
                StatusMessage = "مالکی با این مشخصات یافت نشد. می‌توانید مشخصات را به صورت دستی وارد کنید.";
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyOwner(Owner owner)
    {
        Document.OwnerId = owner.Id;
        Document.BuyerType = owner.Type;
        Document.BuyerName = owner.DisplayName;
        Document.BuyerNationalCode = owner.NationalOrEconomicCode;
        Document.BuyerPhone = owner.Phone;
        Document.BuyerMobile = owner.Mobile;
        Document.BuyerAddress = owner.Address;
        Document.BuyerPostalCode = owner.PostalCode;
        Document.BuyerJob = owner.JobTitle;
        Document.BuyerBirthDate = owner.BirthDate;
        Document.BuyerBirthPlace = owner.BirthPlace;
        Document.BuyerIssueDate = owner.IssueDate;
        Document.BuyerIssuePlace = owner.IssuePlace;
        Document.BuyerHomeAddress = owner.Address;
        Document.BuyerWorkAddress = owner.WorkAddress;

        if (owner.Type == OwnerType.LegalEntity)
        {
            Document.BuyerRegistrationNumber = owner.RegistrationNumber;
            Document.BuyerRepresentativeName = owner.RepresentativeName;
            Document.BuyerEconomicCode = owner.EconomicCode;
            Document.BuyerFatherName = null;
            Document.BuyerIdNumber = null;
        }
        else
        {
            Document.BuyerFatherName = owner.FatherName;
            Document.BuyerIdNumber = owner.IdNumber;
            Document.BuyerRegistrationNumber = null;
            Document.BuyerRepresentativeName = null;
        }

        StatusMessage = $"مشخصات {owner.DisplayName} در سند درج شد.";
    }

    private void ClearOwner()
    {
        SelectedOwner = null;
        Document.OwnerId = null;
        Document.BuyerName = Document.BuyerNationalCode = Document.BuyerIdNumber = null;
        Document.BuyerFatherName = Document.BuyerPhone = Document.BuyerMobile = null;
        Document.BuyerAddress = Document.BuyerPostalCode = null;
        Document.BuyerEconomicCode = Document.BuyerRegistrationNumber = Document.BuyerRepresentativeName = null;
        Document.BuyerPosition = Document.BuyerJob = Document.BuyerIssuePlace = null;
        Document.BuyerBirthDate = Document.BuyerIssueDate = null;
        Document.BuyerBirthPlace = Document.BuyerHomeAddress = Document.BuyerWorkAddress = null;
    }

    #endregion

    #region Step 5 - invoice

    public ObservableCollection<DocumentLineItem> Items { get; }

    private decimal _totalAmount;
    public decimal TotalAmount
    {
        get => _totalAmount;
        private set => SetProperty(ref _totalAmount, value);
    }

    private string? _newItemTitle;
    public string? NewItemTitle
    {
        get => _newItemTitle;
        set => SetProperty(ref _newItemTitle, value);
    }

    public ICommand AddInvoiceItemCommand { get; }
    public ICommand RemoveInvoiceItemCommand { get; }
    public ICommand AddDefaultInvoiceItemsCommand { get; }

    private void AddInvoiceItem()
    {
        var title = (NewItemTitle ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(title)) title = "ردیف جدید";

        var item = new DocumentLineItem { Title = title, Amount = 0m, SortOrder = Items.Count };
        AttachItem(item);
        Items.Add(item);
        NewItemTitle = null;
        RecalculateTotal();
    }

    private void AddDefaultInvoiceItems()
    {
        foreach (var title in _settings.App.DefaultInvoiceItems)
        {
            if (Items.Any(i => string.Equals(i.Title, title, StringComparison.OrdinalIgnoreCase))) continue;
            var item = new DocumentLineItem { Title = title, Amount = 0m, SortOrder = Items.Count };
            AttachItem(item);
            Items.Add(item);
        }
        RecalculateTotal();
    }

    private void RemoveInvoiceItem(DocumentLineItem? item)
    {
        if (item is null) return;
        item.PropertyChanged -= OnItemPropertyChanged;
        Items.Remove(item);
        Resequence();
        RecalculateTotal();
    }

    private void AttachItem(DocumentLineItem item) => item.PropertyChanged += OnItemPropertyChanged;

    private void OnItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DocumentLineItem.Amount)) RecalculateTotal();
    }

    private void Resequence()
    {
        for (var i = 0; i < Items.Count; i++) Items[i].SortOrder = i;
    }

    private void RecalculateTotal() => TotalAmount = Items.Sum(i => i.Amount);

    #endregion

    #region Load / save

    public async Task NewDocumentAsync()
    {
        var draft = await _documents.CreateDraftAsync();
        UseAutoNumbering = _settings.App.AutoNumbering;
        if (UseAutoNumbering)
        {
            draft.DocumentNumber = await _numbering.SuggestAsync(draft.DocumentDate);
        }

        LoadDocument(draft);
        StatusMessage = "سند جدید آماده شد.";
    }

    /// <summary>Loads an existing document (from the saved documents grid) into the wizard.</summary>
    public async Task LoadAsync(SalesDocument document)
    {
        SalesDocument loaded = document;
        if (loaded.Id > 0)
        {
            var fresh = await _documents.GetAsync(loaded.Id);
            if (fresh is not null) loaded = fresh;
        }

        LoadDocument(loaded);
        StatusMessage = $"سند شماره {loaded.DocumentNumber} برای ویرایش باز شد.";
    }

    private void LoadDocument(SalesDocument document)
    {
        Items.Clear();
        Document = document;

        foreach (var item in document.Items.OrderBy(i => i.SortOrder))
        {
            AttachItem(item);
            Items.Add(item);
        }

        UseAutoNumbering = _settings.App.AutoNumbering;
        StepIndex = 0;
        RecalculateTotal();
        RaiseErrorsChanged(nameof(DocumentDateJalali));
    }

    private async Task GenerateNumberAsync()
    {
        Document.DocumentNumber = await _numbering.SuggestAsync(Document.DocumentDate);
        StatusMessage = $"شماره پیشنهادی: {Document.DocumentNumber}";
    }

    private async Task ExportPdfAsync()
    {
        if (!ValidateAll()) { ShowStepError(); return; }

        var path = await _printService.ExportPdfAsync(Document);
        if (!string.IsNullOrEmpty(path))
        {
            _dialogs.ShowInfo($"فایل PDF با موفقیت ایجاد شد:\n{path}", "خروجی PDF");
        }
    }

    private async Task SaveAsync(bool printAfterSave, bool exportAfterSave)
    {
        if (!ValidateAll())
        {
            ShowStepError();
            return;
        }

        IsBusy = true;
        BusyMessage = "در حال ذخیره‌سازی...";
        try
        {
            if (UseAutoNumbering && string.IsNullOrWhiteSpace(Document.DocumentNumber))
            {
                Document.DocumentNumber = await _numbering.ReserveAsync(Document.DocumentDate);
            }
            else
            {
                await _numbering.SyncSequenceAsync();
            }

            await _documents.SaveAsync(Document, Items);
            StatusMessage = $"سند شماره {Document.DocumentNumber} با موفقیت ذخیره شد.";
            OnPropertyChanged(nameof(IsSaved));
            ((RelayCommand)PrintCommand).RaiseCanExecuteChanged();

            if (printAfterSave) _printService.Print(Document);

            if (exportAfterSave)
            {
                var path = await _printService.ExportPdfAsync(Document);
                if (!string.IsNullOrEmpty(path))
                {
                    _dialogs.ShowInfo($"فایل PDF با موفقیت ایجاد شد:\n{path}", "خروجی PDF");
                }
            }
        }
        catch (Exception exception)
        {
            _dialogs.ShowError(exception, "خطا در ذخیره‌سازی سند");
        }
        finally
        {
            IsBusy = false;
            BusyMessage = null;
        }
    }

    #endregion

    #region Validation

    protected override void ValidateProperty(string propertyName)
    {
        switch (propertyName)
        {
            case nameof(DocumentDateJalali):
                Validate(nameof(DocumentDateJalali), JalaliDate.TryParse(DocumentDateJalali, out _), "تاریخ سند الزامی است (نمونه: 1403/05/01).");
                break;
        }
    }

    public override bool ValidateAll()
    {
        ClearAllErrors();

        var isValid = true;
        for (var step = 0; step < Steps.Count; step++)
        {
            if (!ValidateStep(step))
            {
                isValid = false;
                if (StepIndex != step) StepIndex = step;
                break;
            }
        }

        return isValid;
    }

    private bool ValidateStep(int step)
    {
        switch (step)
        {
            case 0:
                var dateOk = JalaliDate.TryParse(DocumentDateJalali, out _);
                var numberOk = !string.IsNullOrWhiteSpace(Document.DocumentNumber);
                Validate(nameof(DocumentDateJalali), dateOk, "تاریخ سند الزامی است (نمونه: 1403/05/01).");
                Validate("DocumentNumber", numberOk, "شماره سند الزامی است. می‌توانید از دکمه «شماره خودکار» استفاده کنید.");
                return dateOk && numberOk;

            case 1:
                var vehicleOk = !string.IsNullOrWhiteSpace(Document.VehicleMake)
                                || !string.IsNullOrWhiteSpace(Document.VehicleModel)
                                || !string.IsNullOrWhiteSpace(Document.VehicleVin);
                Validate("VehicleMake", vehicleOk, "حداقل یکی از فیلدهای برند، مدل یا VIN باید وارد شود.");
                return vehicleOk;

            case 2:
                var ownerOk = !string.IsNullOrWhiteSpace(Document.BuyerName);
                Validate("BuyerName", ownerOk, "نام خریدار الزامی است.");
                return ownerOk;

            default:
                return true;
        }
    }

    #endregion
}
