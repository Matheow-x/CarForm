using System.Windows;
using CarForm.Core.Mvvm;

namespace CarForm.Printing;

public partial class PrintPreviewWindow : Window
{
    public PrintPreviewWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is ViewModelBase oldViewModel) oldViewModel.RequestClose -= OnRequestClose;
        if (e.NewValue is ViewModelBase newViewModel) newViewModel.RequestClose += OnRequestClose;
    }

    private void OnRequestClose(object? sender, bool? dialogResult) => Close();
}
