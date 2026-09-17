using System.Threading.Tasks;
using CarForm.Core.Mvvm;

namespace CarForm.Services;

public interface IDialogService
{
    void ShowInfo(string message, string title = "اطلاعات");

    void ShowWarning(string message, string title = "توجه");

    void ShowError(string message, string title = "خطا");

    bool ShowConfirm(string message, string title = "تأیید");

    bool ShowWarningConfirm(string message, string title = "تأیید");

    string? OpenImageFile(string title = "انتخاب تصویر");

    string? OpenFile(string filter, string title = "انتخاب فایل");

    string? SaveFile(string filter, string defaultName, string title = "ذخیره فایل");

    string? PickFolder(string title, string? initialDirectory = null);

    /// <summary>Shows a window whose type is resolved from the view-model type.</summary>
    bool? ShowDialog(ViewModelBase viewModel);

    void ShowError(Exception exception, string title = "خطا");
}

public interface IViewDialogMap
{
    Type? Resolve(System.Type viewModelType);
}
