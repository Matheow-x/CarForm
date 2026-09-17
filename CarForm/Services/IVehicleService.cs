using System.Collections.Generic;
using System.Threading.Tasks;
using CarForm.Models;

namespace CarForm.Services;

public interface IVehicleService
{
    // ---- catalogue browsing: make -> model -> vehicles ----
    Task<List<VehicleMake>> SearchMakesAsync(string? term = null);

    Task<List<VehicleModel>> GetModelsAsync(int makeId);

    Task<VehicleMake> EnsureMakeAsync(string name);

    Task<VehicleModel> EnsureModelAsync(int makeId, string name);

    // ---- vehicles ----
    Task<List<Vehicle>> SearchVehiclesAsync(string? term = null, int? makeId = null, int? modelId = null, int maxResults = 500);

    Task<Vehicle?> GetAsync(int id);

    Task<Vehicle> SaveAsync(Vehicle vehicle);

    Task DeleteAsync(int id);

    Task<bool> VinExistsAsync(string? vin, int? excludeId = null);
}
