using System.Collections.Generic;

namespace CarForm.Models;

/// <summary>A vehicle manufacturer (Toyota, Kia, ...). Browsing starts here.</summary>
public class VehicleMake : EntityBase
{
    private string _name = string.Empty;
    private string? _country;
    private string? _logoPath;

    public string Name { get => _name; set => SetProperty(ref _name, value); }

    public string? Country { get => _country; set => SetProperty(ref _country, value); }

    public string? LogoPath { get => _logoPath; set => SetProperty(ref _logoPath, value); }

    public List<VehicleModel> Models { get; set; } = new();
}
