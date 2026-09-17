using System;

namespace CarForm.Core.Helpers;

/// <summary>Runs an action when disposed - used for temporary overrides.</summary>
public sealed class DisposableAction : IDisposable
{
    private Action? _action;

    public DisposableAction(Action action) => _action = action;

    public void Dispose()
    {
        _action?.Invoke();
        _action = null;
    }
}
