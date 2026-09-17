using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CarForm.Core.Mvvm;

/// <summary>Minimal, dependency-free implementation of <see cref="INotifyPropertyChanged"/>.</summary>
public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    /// <summary>Sets a field and raises change notifications for the property and its dependents.</summary>
    protected bool SetProperty<T>(ref T field, T value, string[] dependentProperties, [CallerMemberName] string? propertyName = null)
    {
        if (!SetProperty(ref field, value, propertyName)) return false;
        foreach (var dependent in dependentProperties) OnPropertyChanged(dependent);
        return true;
    }
}
