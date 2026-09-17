using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CarForm.Core.Mvvm;
using CarForm.Models;
using CarForm.Services;

namespace CarForm.ViewModels;

/// <summary>Add / edit dialog for a vehicle. Make and model can also be created on the fly.</summary>
public sealed class VehicleEditorViewModel : ViewModelBase
{
    private readonly IVehicleService _vehicles;
    private readonly IDialogService _dialogs;

    private VehicleMake? _selectedMake;
    private VehicleModel? _selectedModel;

    public VehicleEditorViewModel(IVehicleService vehicles, IDialogService dialogs, Vehicle? vehicle, int? preselectedModelId = null)
    {
        _vehicles = vehicles;
        _dialogs = dialogs;

        Vehicle = vehicle ?? new Vehicle { ModelId = preselectedModelId ?? 0 };
        Title = vehicle is null ? "خودرو جدید" : $"ویرایش خودرو";

        Makes = new ObservableCollection<VehicleMake>();
        Models = new ObservableCollection<VehicleModel>();

        AddMakeCommand = new RelayCommand(AddMake);
        AddModelCommand = new RelayCommand(AddModel, () => SelectedMake is not null);
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        CancelCommand = new RelayCommand(() => Close(false));

        _ = InitializeAsync(preselectedModelId);
    }

    public Vehicle Vehicle { get; }

    private string _title = string.Empty;
    public string Title { get => _title; set => SetProperty(ref _title, value); }

    public ObservableCollection<VehicleMake> Makes { get; }
    public ObservableCollection<VehicleModel> Models { get; }

    private string? _newMakeName;
    public string? NewMakeName { get => _newMakeName; set => SetProperty(ref _newMakeName, value); }

    private string? _newModelName;
    public string? NewModelName { get => _newModelName; set => SetProperty(ref _newModelName, value); }

    public VehicleMake? SelectedMake
    {
        get => _selectedMake;
        set
        {
            if (!SetProperty(ref _selectedMake, value)) return;
            ((RelayCommand)AddModelCommand).RaiseCanExecuteChanged();
            _ = LoadModelsAsync(keepSelection: false);
        }
    }

    public VehicleModel? SelectedModel
    {
        get => _selectedModel;
        set
        {
            if (!SetProperty(ref _selectedModel, value)) return;
            if (value is not null) Vehicle.ModelId = value.Id;
        }
    }

    public ICommand AddMakeCommand { get; }
    public ICommand AddModelCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    private async Task InitializeAsync(int? preselectedModelId)
    {
        IsBusy = true;
        try
        {
            var makes = await _vehicles.SearchMakesAsync(null);
            Makes.Clear();
            foreach (var make in makes) Makes.Add(make);

            if (Vehicle.ModelId > 0)
            {
                var model = await FindModelAsync(Vehicle.ModelId);
                if (model is not null)
                {
                    _selectedMake = Makes.FirstOrDefault(m => m.Id == model.MakeId);
                    OnPropertyChanged(nameof(SelectedMake));
                    await LoadModelsAsync(keepSelection: false);
                    SelectedModel = Models.FirstOrDefault(m => m.Id == model.Id);
                }
            }
            else if (Makes.Count > 0)
            {
                _selectedMake = Makes.FirstOrDefault(m => m.Id == preselectedModelId) ?? Makes[0];
                OnPropertyChanged(nameof(SelectedMake));
                await LoadModelsAsync(keepSelection: false);
            }
        }
        catch (Exception exception)
        {
            _dialogs.ShowError(exception);
        }
        finally
        {
            IsBusy = false;
            ((RelayCommand)AddModelCommand).RaiseCanExecuteChanged();
        }
    }

    private async Task LoadModelsAsync(bool keepSelection)
    {
        var previousId = SelectedModel?.Id;
        Models.Clear();
        if (SelectedMake is null) return;

        var models = await _vehicles.GetModelsAsync(SelectedMake.Id);
        foreach (var model in models) Models.Add(model);

        if (keepSelection && previousId.HasValue)
        {
            SelectedModel = Models.FirstOrDefault(m => m.Id == previousId.Value);
        }
    }

    private async Task<VehicleModel?> FindModelAsync(int modelId)
    {
        foreach (var make in Makes)
        {
            var models = await _vehicles.GetModelsAsync(make.Id);
            var match = models.FirstOrDefault(m => m.Id == modelId);
            if (match is not null) return match;
        }
        return null;
    }

    private async void AddMake()
    {
        if (string.IsNullOrWhiteSpace(NewMakeName)) return;

        try
        {
            var make = await _vehicles.EnsureMakeAsync(NewMakeName);
            NewMakeName = null;
            Makes.Add(make);
            SelectedMake = make;
        }
        catch (Exception exception)
        {
            _dialogs.ShowError(exception);
        }
    }

    private async void AddModel()
    {
        if (SelectedMake is null || string.IsNullOrWhiteSpace(NewModelName)) return;

        try
        {
            var model = await _vehicles.EnsureModelAsync(SelectedMake.Id, NewModelName);
            NewModelName = null;
            await LoadModelsAsync(keepSelection: false);
            SelectedModel = Models.FirstOrDefault(m => m.Id == model.Id);
        }
        catch (Exception exception)
        {
            _dialogs.ShowError(exception);
        }
    }

    public override bool ValidateAll()
    {
        ClearAllErrors();
        Validate("ModelId", Vehicle.ModelId > 0, "انتخاب مدل خودرو الزامی است.");
        return !HasErrors;
    }

    private async Task SaveAsync()
    {
        if (!ValidateAll())
        {
            _dialogs.ShowWarning("لطفاً سازنده و مدل خودرو را انتخاب کنید.", "اعتبارسنجی");
            return;
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(Vehicle.Vin) && await _vehicles.VinExistsAsync(Vehicle.Vin, Vehicle.Id == 0 ? null : Vehicle.Id))
            {
                if (!_dialogs.ShowWarningConfirm("خودروی دیگری با این VIN ثبت شده است. آیا مایل به ادامه هستید؟", "VIN تکراری"))
                {
                    return;
                }
            }

            await _vehicles.SaveAsync(Vehicle);
            Close(true);
        }
        catch (Exception exception)
        {
            _dialogs.ShowError(exception, "خطا در ذخیره‌سازی خودرو");
        }
    }
}
