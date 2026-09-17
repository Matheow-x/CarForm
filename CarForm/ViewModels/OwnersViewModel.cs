using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using CarForm.Core.Mvvm;
using CarForm.Models;
using CarForm.Services;

namespace CarForm.ViewModels;

/// <summary>Owners (buyers / sellers): search, professional profile card and CRUD.</summary>
public sealed class OwnersViewModel : ViewModelBase, IPageViewModel
{
    private readonly IOwnerService _owners;
    private readonly IDialogService _dialogs;
    private readonly IImageStore _images;

    private Owner? _selectedOwner;

    public OwnersViewModel(IOwnerService owners, IDialogService dialogs, IImageStore images)
    {
        _owners = owners;
        _dialogs = dialogs;
        _images = images;

        Owners = new ObservableCollection<Owner>();
        TypeOptions = new ObservableCollection<OwnerType?>(new OwnerType?[] { null }
            .Concat(Enum.GetValues<OwnerType>().Cast<OwnerType?>()).ToList());

        SearchCommand = new AsyncRelayCommand(SearchAsync);
        AddCommand = new RelayCommand(Add);
        EditCommand = new RelayCommand(Edit, () => SelectedOwner is not null);
        DeleteCommand = new AsyncRelayCommand(DeleteAsync, () => SelectedOwner is not null);
        RefreshCommand = new AsyncRelayCommand(SearchAsync);
    }

    public string PageKey => PageKeys.Owners;
    public string Title => "مالکان";
    public string Icon => "\uE77B";
    public int Order => 4;

    private string? _searchTerm;
    public string? SearchTerm { get => _searchTerm; set => SetProperty(ref _searchTerm, value); }

    private OwnerType? _typeFilter;
    public OwnerType? TypeFilter
    {
        get => _typeFilter;
        set
        {
            if (SetProperty(ref _typeFilter, value)) _ = SearchAsync();
        }
    }

    public ObservableCollection<Owner> Owners { get; }

    /// <summary>Type filter options - the null entry means "all".</summary>
    public ObservableCollection<OwnerType?> TypeOptions { get; }

    public Owner? SelectedOwner
    {
        get => _selectedOwner;
        set
        {
            if (!SetProperty(ref _selectedOwner, value)) return;
            OnPropertyChanged(nameof(ProfileImage));
            OnPropertyChanged(nameof(HasSelection));
            ((RelayCommand)EditCommand).RaiseCanExecuteChanged();
            ((AsyncRelayCommand)DeleteCommand).RaiseCanExecuteChanged();
        }
    }

    public bool HasSelection => SelectedOwner is not null;

    public BitmapImage? ProfileImage => SelectedOwner is null ? null : _images.Load(SelectedOwner.ImagePath, 320);

    public ICommand SearchCommand { get; }
    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand RefreshCommand { get; }

    public Task OnNavigatedAsync() => SearchAsync();

    private async Task SearchAsync()
    {
        IsBusy = true;
        try
        {
            var results = await _owners.SearchAsync(SearchTerm, TypeFilter, 500);
            Owners.Clear();
            foreach (var owner in results) Owners.Add(owner);
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

    private void Add()
    {
        var editor = new OwnerEditorViewModel(_owners, _dialogs, _images, null);
        if (_dialogs.ShowDialog(editor) == true) _ = SearchAsync();
    }

    private void Edit()
    {
        if (SelectedOwner is null) return;

        var editor = new OwnerEditorViewModel(_owners, _dialogs, _images, SelectedOwner);
        if (_dialogs.ShowDialog(editor) == true) _ = SearchAsync();
    }

    private async Task DeleteAsync()
    {
        if (SelectedOwner is null) return;

        if (!_dialogs.ShowWarningConfirm($"آیا از حذف «{SelectedOwner.DisplayName}» اطمینان دارید؟", "حذف مالک"))
        {
            return;
        }

        try
        {
            await _owners.DeleteAsync(SelectedOwner.Id);
            await SearchAsync();
        }
        catch (Exception exception)
        {
            _dialogs.ShowError(exception, "خطا در حذف مالک");
        }
    }
}
