using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CarForm.Core.Mvvm;
using CarForm.Core.Persian;
using CarForm.Models;
using CarForm.Services;

namespace CarForm.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly INavigationService _navigation;
    private readonly IEnumerable<IPageViewModel> _pages;
    private readonly ISettingsService _settings;

    private IPageViewModel? _currentPage;
    private NavigationItem? _selectedItem;

    public MainViewModel(INavigationService navigation, IEnumerable<IPageViewModel> pages, ISettingsService settings)
    {
        _navigation = navigation;
        _pages = pages;
        _settings = settings;

        Items = new ObservableCollection<NavigationItem>(
            pages.OrderBy(p => p.Order).Select(p => new NavigationItem(p.PageKey, p.Title, p.Icon)));

        NavigateCommand = new AsyncRelayCommand<string?>(NavigateAsync);
        _navigation.Register(NavigateAsync, OpenDocumentAsync);

        TodayJalali = JalaliDate.Format(DateTime.Today, settings.App.UsePersianDigitsInUi);
    }

    public ObservableCollection<NavigationItem> Items { get; }

    public IPageViewModel? CurrentPage
    {
        get => _currentPage;
        private set
        {
            if (SetProperty(ref _currentPage, value)) OnPropertyChanged(nameof(PageTitle));
        }
    }

    public NavigationItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (SetProperty(ref _selectedItem, value) && value is not null)
            {
                _ = NavigateAsync(value.Key);
            }
        }
    }

    public string PageTitle => CurrentPage?.Title ?? string.Empty;

    public string TodayJalali { get; }

    public AsyncRelayCommand<string?> NavigateCommand { get; }

    private async Task NavigateAsync(string? pageKey)
    {
        if (string.IsNullOrWhiteSpace(pageKey)) return;

        var page = _pages.FirstOrDefault(p => p.PageKey == pageKey);
        if (page is null) return;

        var item = Items.FirstOrDefault(i => i.Key == pageKey);
        if (item is not null && !ReferenceEquals(_selectedItem, item))
        {
            _selectedItem = item;
            OnPropertyChanged(nameof(SelectedItem));
        }

        CurrentPage = page;
        await page.OnNavigatedAsync();
    }

    private async Task OpenDocumentAsync(SalesDocument document)
    {
        await NavigateAsync(PageKeys.NewDocument);
        if (CurrentPage is NewDocumentWizardViewModel wizard)
        {
            await wizard.LoadAsync(document);
        }
    }
}
