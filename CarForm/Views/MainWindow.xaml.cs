using System.Windows;
using CarForm.Services;
using CarForm.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace CarForm.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        ViewModel = viewModel;
        Loaded += OnLoaded;
    }

    public MainViewModel ViewModel { get; }

    private void OnLoaded(object sender, RoutedEventArgs e)
        => ViewModel.NavigateCommand.Execute(PageKeys.NewDocument);
}
