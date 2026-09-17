using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CarForm.Data;
using CarForm.Models;
using Microsoft.EntityFrameworkCore;

namespace CarForm.Services;

public class VehicleService : IVehicleService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly IAuditService _audit;

    public VehicleService(IDbContextFactory<AppDbContext> factory, IAuditService audit)
    {
        _factory = factory;
        _audit = audit;
    }

    public async Task<List<VehicleMake>> SearchMakesAsync(string? term = null)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var query = db.VehicleMakes.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(term))
        {
            var like = $"%{term.Trim()}%";
            // Searching a make also matches its models: "Corolla" must surface Toyota.
            query = query.Where(m =>
                EF.Functions.Like(m.Name, like) ||
                m.Models.Any(mo => EF.Functions.Like(mo.Name, like)));
        }

        return await query.OrderBy(m => m.Name).ToListAsync();
    }

    public async Task<List<VehicleModel>> GetModelsAsync(int makeId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.VehicleModels.AsNoTracking()
            .Where(m => m.MakeId == makeId)
            .OrderBy(m => m.Name)
            .ToListAsync();
    }

    public async Task<VehicleMake> EnsureMakeAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("نام سازنده نمی‌تواند خالی باشد.", nameof(name));

        await using var db = await _factory.CreateDbContextAsync();
        var trimmed = name.Trim();
        var existing = await db.VehicleMakes.FirstOrDefaultAsync(m => m.Name == trimmed);
        if (existing is not null) return existing;

        var make = new VehicleMake { Name = trimmed };
        db.VehicleMakes.Add(make);
        await db.SaveChangesAsync();
        return make;
    }

    public async Task<VehicleModel> EnsureModelAsync(int makeId, string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("نام مدل نمی‌تواند خالی باشد.", nameof(name));

        await using var db = await _factory.CreateDbContextAsync();
        var trimmed = name.Trim();
        var existing = await db.VehicleModels.FirstOrDefaultAsync(m => m.MakeId == makeId && m.Name == trimmed);
        if (existing is not null) return existing;

        var model = new VehicleModel { MakeId = makeId, Name = trimmed };
        db.VehicleModels.Add(model);
        await db.SaveChangesAsync();
        return model;
    }

    public async Task<List<Vehicle>> SearchVehiclesAsync(string? term = null, int? makeId = null, int? modelId = null, int maxResults = 500)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var query = db.Vehicles.AsNoTracking()
            .Include(v => v.Model).ThenInclude(m => m!.Make)
            .AsQueryable();

        if (makeId.HasValue) query = query.Where(v => v.Model!.MakeId == makeId.Value);
        if (modelId.HasValue) query = query.Where(v => v.ModelId == modelId.Value);

        if (!string.IsNullOrWhiteSpace(term))
        {
            var like = $"%{term.Trim()}%";
            query = query.Where(v =>
                EF.Functions.Like(v.Vin ?? string.Empty, like) ||
                EF.Functions.Like(v.ChassisNumber ?? string.Empty, like) ||
                EF.Functions.Like(v.EngineNumber ?? string.Empty, like) ||
                EF.Functions.Like(v.PlateNumber ?? string.Empty, like) ||
                EF.Functions.Like(v.Color ?? string.Empty, like) ||
                EF.Functions.Like(v.Trim ?? string.Empty, like) ||
                EF.Functions.Like(v.Model!.Name, like) ||
                EF.Functions.Like(v.Model!.Make!.Name, like));
        }

        return await query
            .OrderBy(v => v.Model!.Make!.Name)
            .ThenBy(v => v.Model!.Name)
            .Take(maxResults)
            .ToListAsync();
    }

    public async Task<Vehicle?> GetAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Vehicles.AsNoTracking()
            .Include(v => v.Model).ThenInclude(m => m!.Make)
            .FirstOrDefaultAsync(v => v.Id == id);
    }

    public async Task<Vehicle> SaveAsync(Vehicle vehicle)
    {
        await using var db = await _factory.CreateDbContextAsync();

        Vehicle entity;
        var isNew = vehicle.Id == 0;
        if (isNew)
        {
            entity = new Vehicle();
            db.Vehicles.Add(entity);
        }
        else
        {
            entity = await db.Vehicles.FirstAsync(v => v.Id == vehicle.Id);
        }

        db.Entry(entity).CurrentValues.SetValues(vehicle);
        await db.SaveChangesAsync();

        vehicle.Id = entity.Id;
        vehicle.CreatedAt = entity.CreatedAt;
        vehicle.UpdatedAt = entity.UpdatedAt;

        var title = $"{await MakeNameAsync(db, entity.ModelId)} {(await db.VehicleModels.FindAsync(entity.ModelId))?.Name}";
        await _audit.LogAsync(AuditEntityType.Vehicle, isNew ? AuditAction.Create : AuditAction.Update, entity.Id,
            $"{(isNew ? "ایجاد" : "ویرایش")} خودرو: {title}",
            $"VIN: {entity.Vin} | پلاک: {entity.PlateNumber} | رنگ: {entity.Color}");

        return vehicle;
    }

    public async Task DeleteAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var entity = await db.Vehicles.Include(v => v.Model).FirstOrDefaultAsync(v => v.Id == id);
        if (entity is null) return;

        var summary = $"حذف خودرو: {entity.Model?.Name} ({entity.Vin ?? entity.PlateNumber ?? "بدون شناسه"})";
        db.Vehicles.Remove(entity);
        await db.SaveChangesAsync();

        await _audit.LogAsync(AuditEntityType.Vehicle, AuditAction.Delete, id, summary);
    }

    public async Task<bool> VinExistsAsync(string? vin, int? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(vin)) return false;

        await using var db = await _factory.CreateDbContextAsync();
        var value = vin.Trim();
        return await db.Vehicles.AnyAsync(v => v.Vin == value && (!excludeId.HasValue || v.Id != excludeId.Value));
    }

    private static async Task<string> MakeNameAsync(AppDbContext db, int modelId)
    {
        var model = await db.VehicleModels.AsNoTracking()
            .Include(m => m.Make)
            .FirstOrDefaultAsync(m => m.Id == modelId);
        return model?.Make?.Name ?? string.Empty;
    }
}
