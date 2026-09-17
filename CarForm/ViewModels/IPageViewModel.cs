using System.ComponentModel;
using System.Threading.Tasks;

namespace CarForm.ViewModels;

/// <summary>A page shown in the main content area.</summary>
public interface IPageViewModel : INotifyPropertyChanged
{
    string PageKey { get; }

    string Title { get; }

    /// <summary>A glyph from "Segoe MDL2 Assets".</summary>
    string Icon { get; }

    int Order { get; }

    /// <summary>Called by the shell each time the page becomes visible.</summary>
    Task OnNavigatedAsync();
}

public sealed class NavigationItem
{
    public NavigationItem(string key, string title, string icon)
    {
        Key = key;
        Title = title;
        Icon = icon;
    }

    public string Key { get; }

    public string Title { get; }

    public string Icon { get; }

    public override string ToString() => Title;
}
