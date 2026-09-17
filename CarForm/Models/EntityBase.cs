using CarForm.Core.Mvvm;

namespace CarForm.Models;

/// <summary>
/// Base class for persisted entities. Entities raise property-change notifications so that
/// editor view-models can bind straight to them (auto-fill in the wizard must refresh the UI).
/// EF Core is completely agnostic to this - change tracking is done by DbContext snapshotting.
/// </summary>
public abstract class EntityBase : ObservableObject
{
    public int Id { get; set; }
}
