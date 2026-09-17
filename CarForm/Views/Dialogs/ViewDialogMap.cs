using System;
using System.Collections.Generic;
using CarForm.Services;
using CarForm.ViewModels;

namespace CarForm.Views.Dialogs;

/// <summary>Maps editor view-models to their windows (keeps the service layer free of view types).</summary>
public sealed class ViewDialogMap : IViewDialogMap
{
    private static readonly Dictionary<Type, Type> Map = new()
    {
        [typeof(OwnerEditorViewModel)] = typeof(OwnerEditorWindow),
        [typeof(VehicleEditorViewModel)] = typeof(VehicleEditorWindow),
        [typeof(PrintLayoutEditorViewModel)] = typeof(PrintLayoutEditorWindow)
    };

    public Type? Resolve(Type viewModelType) => Map.TryGetValue(viewModelType, out var windowType) ? windowType : null;
}
