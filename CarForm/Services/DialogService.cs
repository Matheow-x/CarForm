using System;
using System.IO;
using System.Windows;
using CarForm.Core.Helpers;
using CarForm.Core.Mvvm;
using Microsoft.Win32;

namespace CarForm.Services;

public class DialogService : IDialogService
{
    private readonly IViewDialogMap _map;

    public DialogService(IViewDialogMap map)
    {
        _map = map;
    }

    private static Window? Owner => Application.Current?.MainWindow;

    public void ShowInfo(string message, string title = "اطلاعات")
        => MessageBox.Show(Owner, message, title, MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);

    public void ShowWarning(string message, string title = "توجه")
        => MessageBox.Show(Owner, message, title, MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.OK, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);

    public void ShowError(string message, string title = "خطا")
        => MessageBox.Show(Owner, message, title, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);

    public void ShowError(Exception exception, string title = "خطا")
        => ShowError($"{exception.Message}\n\n{exception.GetType().Name}", title);

    public bool ShowConfirm(string message, string title = "تأیید")
        => MessageBox.Show(Owner, message, title, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No,
            MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign) == MessageBoxResult.Yes;

    public bool ShowWarningConfirm(string message, string title = "تأیید")
        => MessageBox.Show(Owner, message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No,
            MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign) == MessageBoxResult.Yes;

    public string? OpenImageFile(string title = "انتخاب تصویر")
        => OpenFile("تصاویر|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff|همه فایل‌ها|*.*", title);

    public string? OpenFile(string filter, string title = "انتخاب فایل")
    {
        var dialog = new OpenFileDialog { Filter = filter, Title = title, CheckFileExists = true, Multiselect = false };
        return dialog.ShowDialog(Owner) == true ? dialog.FileName : null;
    }

    public string? SaveFile(string filter, string defaultName, string title = "ذخیره فایل")
    {
        var dialog = new SaveFileDialog
        {
            Filter = filter,
            Title = title,
            FileName = defaultName,
            AddExtension = true,
            InitialDirectory = Directory.Exists(AppPaths.ExportsFolder) ? AppPaths.ExportsFolder : null
        };
        return dialog.ShowDialog(Owner) == true ? dialog.FileName : null;
    }

    public string? PickFolder(string title, string? initialDirectory = null)
    {
        var dialog = new OpenFolderDialog
        {
            Title = title,
            InitialDirectory = string.IsNullOrWhiteSpace(initialDirectory) ? AppPaths.AppDataFolder : initialDirectory
        };
        return dialog.ShowDialog(Owner) == true ? dialog.FolderName : null;
    }

    public bool? ShowDialog(ViewModelBase viewModel)
    {
        var windowType = _map.Resolve(viewModel.GetType());
        if (windowType is null) return null;

        if (Activator.CreateInstance(windowType) is not Window window) return null;

        window.Owner = Owner;
        window.DataContext = viewModel;
        viewModel.RequestClose += (_, result) =>
        {
            try
            {
                window.DialogResult = result;
            }
            catch
            {
                window.Close();
            }
        };

        return window.ShowDialog();
    }
}
