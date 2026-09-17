using System.Collections.Generic;

namespace CarForm.Models;

/// <summary>A model that belongs to a <see cref="VehicleMake"/> (Corolla, Cerato, ...).</summary>
public class VehicleModel : EntityBase
{
    private string _name = string.Empty;
    private int _makeId;
    private VehicleMake? _make;
    private string? _bodyType;
    private int? _defaultYear;

    public string Name { get => _name; set => SetProperty(ref _name, value); }

    public int MakeId { get => _makeId; set => SetProperty(ref _makeId, value); }

    public VehicleMake? Make { get => _make; set => SetProperty(ref _make, value); }

    public string? BodyType { get => _bodyType; set => SetProperty(ref _bodyType, value); }

    public int? DefaultYear { get => _defaultYear; set => SetProperty(ref _defaultYear, value); }

    public List<Vehicle> Vehicles { get; set; } = new();
}
