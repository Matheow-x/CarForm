using System;
using System.Windows;
using CarForm.Models;

namespace CarForm.Services;

public interface IThemeManager
{
    ThemeMode Current { get; }

    void Apply(ThemeMode mode);
}

/// <summary>Swaps the color resource dictionary at runtime (day / night theme).</summary>
public sealed class ThemeManager : IThemeManager
{
    public ThemeMode Current { get; private set; } = ThemeMode.Light;

    public void Apply(ThemeMode mode)
    {
        var application = Application.Current;
        if (application is null) return;

        var dictionaries = application.Resources.MergedDictionaries;

        // Remove every previously loaded palette. (WPF searches MergedDictionaries in reverse
        // order, so the palette that must win has to be the LAST one in the collection.)
        for (var i = dictionaries.Count - 1; i >= 0; i--)
        {
            var source = dictionaries[i].Source?.OriginalString ?? string.Empty;
            if (source.Contains("Colors.Light", StringComparison.OrdinalIgnoreCase) ||
                source.Contains("Colors.Dark", StringComparison.OrdinalIgnoreCase))
            {
                dictionaries.RemoveAt(i);
            }
        }

        var uri = mode == ThemeMode.Dark
            ? new Uri("Themes/Colors.Dark.xaml", UriKind.Relative)
            : new Uri("Themes/Colors.Light.xaml", UriKind.Relative);

        dictionaries.Add(new ResourceDictionary { Source = uri });
        Current = mode;
    }
}
