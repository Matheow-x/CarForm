using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarForm.Models;

/// <summary>
/// A concrete, saved vehicle. Fixed specs (VIN, engine no, ...) live here and are copied into a
/// document when the vehicle is picked in the wizard, so only variable data must be typed.
/// </summary>
public class Vehicle : EntityBase
{
    private int _modelId;
    private VehicleModel? _model;
    private string? _vin;
    private string? _chassisNumber;
    private string? _engineNumber;
    private string? _plateNumber;
    private string? _color;
    private string? _trim;
    private string? _fuelType;
    private string? _bodyType;
    private string? _engineDisplacement;
    private string? _system;
    private string? _usageType;
    private string? _cylinders;
    private string? _axles;
    private string? _wheels;
    private string? _cabinNo;
    private string? _capacity;
    private string? _country;
    private int? _modelYear;
    private long? _mileage;
    private string? _notes;
    private string? _imagePath;
    private DateTime _createdAt;
    private DateTime _updatedAt;

    public int ModelId { get => _modelId; set => SetProperty(ref _modelId, value); }

    public VehicleModel? Model { get => _model; set => SetProperty(ref _model, value); }

    /// <summary>شماره شناسایی خودرو (VIN)</summary>
    public string? Vin { get => _vin; set => SetProperty(ref _vin, value); }

    /// <summary>شماره شاسی</summary>
    public string? ChassisNumber { get => _chassisNumber; set => SetProperty(ref _chassisNumber, value); }

    /// <summary>شماره موتور</summary>
    public string? EngineNumber { get => _engineNumber; set => SetProperty(ref _engineNumber, value); }

    /// <summary>شماره پلاک</summary>
    public string? PlateNumber { get => _plateNumber; set => SetProperty(ref _plateNumber, value); }

    public string? Color { get => _color; set => SetProperty(ref _color, value); }

    /// <summary>تیپ / کلاس خودرو</summary>
    public string? Trim { get => _trim; set => SetProperty(ref _trim, value); }

    public string? FuelType { get => _fuelType; set => SetProperty(ref _fuelType, value); }

    public string? BodyType { get => _bodyType; set => SetProperty(ref _bodyType, value); }

    /// <summary>سیستم (فنی)</summary>
    public string? System { get => _system; set => SetProperty(ref _system, value); }

    /// <summary>نوع کاربری</summary>
    public string? UsageType { get => _usageType; set => SetProperty(ref _usageType, value); }

    /// <summary>تعداد سیلندر</summary>
    public string? Cylinders { get => _cylinders; set => SetProperty(ref _cylinders, value); }

    /// <summary>تعداد محور</summary>
    public string? Axles { get => _axles; set => SetProperty(ref _axles, value); }

    /// <summary>تعداد چرخ</summary>
    public string? Wheels { get => _wheels; set => SetProperty(ref _wheels, value); }

    /// <summary>شماره اتاق</summary>
    public string? CabinNo { get => _cabinNo; set => SetProperty(ref _cabinNo, value); }

    /// <summary>ظرفیت</summary>
    public string? Capacity { get => _capacity; set => SetProperty(ref _capacity, value); }

    /// <summary>کشور سازنده</summary>
    public string? Country { get => _country; set => SetProperty(ref _country, value); }

    /// <summary>حجم موتور (سی‌سی)</summary>
    public string? EngineDisplacement { get => _engineDisplacement; set => SetProperty(ref _engineDisplacement, value); }

    /// <summary>سال ساخت</summary>
    public int? ModelYear { get => _modelYear; set => SetProperty(ref _modelYear, value); }

    /// <summary>کیلومتر کارکرد</summary>
    public long? Mileage { get => _mileage; set => SetProperty(ref _mileage, value); }

    public string? Notes { get => _notes; set => SetProperty(ref _notes, value); }

    public string? ImagePath { get => _imagePath; set => SetProperty(ref _imagePath, value); }

    public DateTime CreatedAt { get => _createdAt; set => SetProperty(ref _createdAt, value); }

    public DateTime UpdatedAt { get => _updatedAt; set => SetProperty(ref _updatedAt, value); }

    // Convenience (not mapped) - populated by the vehicle service when loading.
    [NotMapped]
    public string MakeName => Model?.Make?.Name ?? string.Empty;

    [NotMapped]
    public string ModelName => Model?.Name ?? string.Empty;

    [NotMapped]
    public string FullTitle => $"{MakeName} {ModelName} {Trim}".Trim();
}
