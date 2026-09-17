using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace CarForm.Core.Mvvm;

/// <summary>
/// Base class for all view models: observable properties + <see cref="INotifyDataErrorInfo"/> validation.
/// Subclasses override <see cref="ValidateProperty"/> (per-property) and <see cref="ValidateAll"/> (before save).
/// Errors are surfaced to the UI through the string indexer, e.g. <c>{Binding [FirstName]}</c>.
/// </summary>
public abstract class ViewModelBase : ObservableObject, INotifyDataErrorInfo
{
    private readonly Dictionary<string, List<string>> _errors = new(StringComparer.Ordinal);

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    /// <summary>Raised by a dialog view-model to close its window with the given dialog result.</summary>
    public event EventHandler<bool?>? RequestClose;

    /// <summary>Asks the host window to close (used by dialogs).</summary>
    protected void Close(bool? dialogResult = true) => RequestClose?.Invoke(this, dialogResult);

    public bool HasErrors => _errors.Values.Any(v => v.Count > 0);

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    private string? _busyMessage;
    public string? BusyMessage
    {
        get => _busyMessage;
        set => SetProperty(ref _busyMessage, value);
    }

    /// <summary>First validation error for a property, or null. Used by the UI for inline messages.</summary>
    public string? this[string propertyName]
    {
        get
        {
            if (string.IsNullOrEmpty(propertyName)) return null;
            return _errors.TryGetValue(propertyName, out var list) && list.Count > 0 ? list[0] : null;
        }
    }

    public IEnumerable GetErrors(string? propertyName)
    {
        if (string.IsNullOrEmpty(propertyName)) return Array.Empty<string>();
        return _errors.TryGetValue(propertyName!, out var list) ? list : (IEnumerable)Array.Empty<string>();
    }

    protected override void OnPropertyChanged(string? propertyName)
    {
        base.OnPropertyChanged(propertyName);
        if (!string.IsNullOrEmpty(propertyName)) ValidateProperty(propertyName!);
    }

    protected virtual void ValidateProperty(string propertyName) { }

    /// <summary>Runs full validation. Returns true when the model is valid.</summary>
    public virtual bool ValidateAll()
    {
        ClearAllErrors();
        return !HasErrors;
    }

    protected void ClearErrors(string propertyName)
    {
        if (_errors.Remove(propertyName)) ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
    }

    protected void ClearAllErrors()
    {
        var keys = _errors.Keys.ToList();
        _errors.Clear();
        foreach (var key in keys) ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(key));
    }

    protected void SetError(string propertyName, string message)
    {
        if (!_errors.TryGetValue(propertyName, out var list))
        {
            list = new List<string>();
            _errors[propertyName] = list;
        }
        if (!list.Contains(message)) list.Add(message);
        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
    }

    /// <summary>Sets a single error for a property (replacing any previous error) or clears it.</summary>
    protected void Validate(string propertyName, bool condition, string message)
    {
        ClearErrors(propertyName);
        if (!condition) SetError(propertyName, message);
    }

    protected void RaiseErrorsChanged(string propertyName)
        => ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
}
