using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CarForm.Core.Mvvm;
using CarForm.Models;
using CarForm.Services;

namespace CarForm.ViewModels;

/// <summary>
/// Vehicle catalogue browser + CRUD.
/// Browsing is three level: search a make ("Toyota") -> pick it -> pick a model -> see the saved vehicles.
/// </summary>
public sealed class VehiclesViewModel : ViewModelBase, IPageViewModel
{
    private readonly IVehicleService _vehicles;
    private readonly IDialogService _dialogs;

    private VehicleMake? _selectedMake;
    private VehicleModel? _selectedModel;
    private Vehicle? _selectedVehicle;

    public VehiclesViewModel(IVehicleService vehicles, IDialogService dialogs)
    {
        _vehicles = vehicles;
        _dialogs = dialogs;

        Makes = new ObservableCollection<VehicleMake>();
        Models = new ObservableCollection<VehicleModel>();
        Vehicles = new ObservableCollection<Vehicle>();

        SearchMakesCommand = new AsyncRelayCommand(LoadMakesAsync);
        SelectMakeCommand = new AsyncRelayCommand<VehicleMake?>(SelectMakeAsync);
        SelectModelCommand = new AsyncRelayCommand<VehicleModel?>(SelectModelAsync);
        SearchVehiclesCommand = new AsyncRelayCommand(LoadVehiclesAsync);
        AddMakeCommand = new AsyncRelayCommand(AddMakeAsync);
        AddModelCommand = new AsyncRelayCommand(AddModelAsync, () => SelectedMake is not null);
        AddVehicleCommand = new RelayCommand(AddVehicle);
        EditVehicleCommand = new RelayCommand(EditVehicle, () => SelectedVehicle is not null);
        DeleteVehicleCommand = new AsyncRelayCommand(DeleteVehicleAsync, () => SelectedVehicle is not null);
        RefreshCommand = new AsyncRelayCommand(LoadMakesAsync);
    }

    public string PageKey => PageKeys.Vehicles;
    public string Title => "خودروها";
    public string Icon => "\uE804";
    public int Order => 3;

    #region Browsing

    private string? _makeSearchTerm;
    public string? MakeSearchTerm
    {
        get => _makeSearchTerm;
        set => SetProperty(ref _makeSearchTerm, value);
    }

    private string? _vehicleSearchTerm;
    public string? VehicleSearchTerm
    {
        get => _vehicleSearchTerm;
        set => SetProperty(ref _vehicleSearchTerm, value);
    }

    private string? _newMakeName;
    public string? NewMakeName { get => _newMakeName; set => SetProperty(ref _newMakeName, value); }

    private string? _newModelName;
    public string? NewModelName { get => _newModelName; set => SetProperty(ref _newModelName, value); }

    public ObservableCollection<VehicleMake> Makes { get; }
    public ObservableCollection<VehicleModel> Models { get; }
    public ObservableCollection<Vehicle> Vehicles { get; }

    public VehicleMake? SelectedMake
    {
        get => _selectedMake;
        set
        {
            if (!SetProperty(ref _selectedMake, value)) return;
            OnPropertyChanged(nameof(HasSelectedMake));
            ((AsyncRelayCommand<VehicleModel?>)AddModelCommand).RaiseCanExecuteChanged();
            _ = LoadModelsAsync();
        }
    }

    public VehicleModel? SelectedModel
    {
        get => _selectedModel;
        set
        {
            if (SetProperty(ref _selectedModel, value)) _ = LoadVehiclesAsync();
        }
    }

    public Vehicle? SelectedVehicle
    {
        get => _selectedVehicle;
        set
        {
            if (!SetProperty(ref _selectedVehicle, value)) return;
            ((RelayCommand)EditVehicleCommand).RaiseCanExecuteChanged();
            ((AsyncRelayCommand)DeleteVehicleCommand).RaiseCanExecuteChanged();
        }
    }

    public bool HasSelectedMake => SelectedMake is not null;

    #endregion

    public ICommand SearchMakesCommand { get; }
    public ICommand SelectMakeCommand { get; }
    public ICommand SelectModelCommand { get; }
    public ICommand SearchVehiclesCommand { get; }
    public ICommand AddMakeCommand { get; }
    public ICommand AddModelCommand { get; }
    public ICommand AddVehicleCommand { get; }
    public ICommand EditVehicleCommand { get; }
    public ICommand DeleteVehicleCommand { get; }
    public ICommand RefreshCommand { get; }

    public async Task OnNavigatedAsync()
    {
        if (Makes.Count == 0) await LoadMakesAsync();
        if (Vehicles.Count == 0) await LoadVehiclesAsync();
    }

    private async Task LoadMakesAsync()
    {
        IsBusy = true;
        try
        {
            var makes = await _vehicles.SearchMakesAsync(MakeSearchTerm);
            Makes.Clear();
            foreach (var make in makes) Makes.Add(make);
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

    private async Task LoadModelsAsync()
    {
        Models.Clear();
        if (SelectedMake is null) return;

        IsBusy = true;
        try
        {
            var models = await _vehicles.GetModelsAsync(SelectedMake.Id);
            foreach (var model in models) Models.Add(model);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadVehiclesAsync()
    {
        IsBusy = true;
        try
        {
            var results = await _vehicles.SearchVehiclesAsync(VehicleSearchTerm,
                SelectedMake?.Id, SelectedModel?.Id, 300);

            Vehicles.Clear();
            foreach (var vehicle in results) Vehicles.Add(vehicle);
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

    private Task SelectMakeAsync(VehicleMake? make)
    {
        SelectedMake = make;
        return Task.CompletedTask;
    }

    private Task SelectModelAsync(VehicleModel? model)
    {
        SelectedModel = model;
        return Task.CompletedTask;
    }

    private async Task AddMakeAsync()
    {
        if (string.IsNullOrWhiteSpace(NewMakeName)) return;

        try
        {
            var make = await _vehicles.EnsureMakeAsync(NewMakeName);
            NewMakeName = null;
            await LoadMakesAsync();
            SelectedMake = Makes.FirstOrDefault(m => m.Id == make.Id);
        }
        catch (Exception exception)
        {
            _dialogs.ShowError(exception);
        }
    }

    private async Task AddModelAsync()
    {
        if (SelectedMake is null || string.IsNullOrWhiteSpace(NewModelName)) return;

        try
        {
            var model = await _vehicles.EnsureModelAsync(SelectedMake.Id, NewModelName);
            NewModelName = null;
            await LoadModelsAsync();
            SelectedModel = Models.FirstOrDefault(m => m.Id == model.Id);
        }
        catch (Exception exception)
        {
            _dialogs.ShowError(exception);
        }
    }

    #region Vehicle CRUD

    private void AddVehicle()
    {
        var editor = new VehicleEditorViewModel(_vehicles, _dialogs, null, SelectedModel?.Id ?? SelectedMake?.Id);
        if (_dialogs.ShowDialog(editor) == true)
        {
            _ = LoadVehiclesAsync();
        }
    }

    private void EditVehicle()
    {
        if (SelectedVehicle is null) return;

        var editor = new VehicleEditorViewModel(_vehicles, _dialogs, SelectedVehicle, SelectedModel?.Id);
        if (_dialogs.ShowDialog(editor) == true)
        {
            _ = LoadVehiclesAsync();
        }
    }

    private async Task DeleteVehicleAsync()
    {
        if (SelectedVehicle is null) return;

        if (!_dialogs.ShowWarningConfirm($"آیا از حذف خودرو «{SelectedVehicle.FullTitle}» اطمینان دارید؟", "حذف خودرو"))
        {
            return;
        }

        try
        {
            await _vehicles.DeleteAsync(SelectedVehicle.Id);
            await LoadVehiclesAsync();
        }
        catch (Exception exception)
        {
            _dialogs.ShowError(exception, "خطا در حذف خودرو");
        }
    }

    #endregion
}
