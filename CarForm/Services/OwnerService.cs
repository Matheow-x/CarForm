using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CarForm.Data;
using CarForm.Models;
using Microsoft.EntityFrameworkCore;

namespace CarForm.Services;

public class OwnerService : IOwnerService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly IAuditService _audit;

    public OwnerService(IDbContextFactory<AppDbContext> factory, IAuditService audit)
    {
        _factory = factory;
        _audit = audit;
    }

    public async Task<List<Owner>> SearchAsync(string? term = null, OwnerType? type = null, int maxResults = 500)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var query = db.Owners.AsNoTracking().AsQueryable();
        if (type.HasValue) query = query.Where(o => o.Type == type.Value);

        if (!string.IsNullOrWhiteSpace(term))
        {
            var like = $"%{term.Trim()}%";
            query = query.Where(o =>
                EF.Functions.Like(o.FirstName ?? string.Empty, like) ||
                EF.Functions.Like(o.LastName ?? string.Empty, like) ||
                EF.Functions.Like(o.CompanyName ?? string.Empty, like) ||
                EF.Functions.Like(o.NationalCode ?? string.Empty, like) ||
                EF.Functions.Like(o.CompanyNationalId ?? string.Empty, like) ||
                EF.Functions.Like(o.Mobile ?? string.Empty, like) ||
                EF.Functions.Like(o.Phone ?? string.Empty, like) ||
                EF.Functions.Like(o.RepresentativeName ?? string.Empty, like));
        }

        return await query
            .OrderBy(o => o.Type)
            .ThenBy(o => o.LastName)
            .ThenBy(o => o.CompanyName)
            .Take(maxResults)
            .ToListAsync();
    }

    public async Task<List<Owner>> GetAllAsync() => await SearchAsync(null, null);

    public async Task<Owner?> GetAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Owners.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<Owner> SaveAsync(Owner owner)
    {
        await using var db = await _factory.CreateDbContextAsync();

        Owner entity;
        var isNew = owner.Id == 0;
        if (isNew)
        {
            entity = new Owner();
            db.Owners.Add(entity);
        }
        else
        {
            entity = await db.Owners.FirstAsync(o => o.Id == owner.Id);
        }

        db.Entry(entity).CurrentValues.SetValues(owner);
        await db.SaveChangesAsync();

        owner.Id = entity.Id;
        owner.CreatedAt = entity.CreatedAt;
        owner.UpdatedAt = entity.UpdatedAt;

        await _audit.LogAsync(AuditEntityType.Owner, isNew ? AuditAction.Create : AuditAction.Update, entity.Id,
            $"{(isNew ? "ایجاد" : "ویرایش")} {(entity.Type == OwnerType.LegalEntity ? "شخص حقوقی" : "شخص حقیقی")}: {entity.DisplayName}",
            $"کد ملی/شناسه: {entity.NationalOrEconomicCode} | تلفن: {entity.Mobile ?? entity.Phone}");

        return owner;
    }

    public async Task DeleteAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var entity = await db.Owners.FirstOrDefaultAsync(o => o.Id == id);
        if (entity is null) return;

        var summary = $"حذف {(entity.Type == OwnerType.LegalEntity ? "شخص حقوقی" : "شخص حقیقی")}: {entity.DisplayName}";
        db.Owners.Remove(entity);
        await db.SaveChangesAsync();

        await _audit.LogAsync(AuditEntityType.Owner, AuditAction.Delete, id, summary);
    }

    public async Task<bool> ExistsAsync(string? nationalCode, int? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(nationalCode)) return false;

        await using var db = await _factory.CreateDbContextAsync();
        var code = nationalCode.Trim();
        return await db.Owners.AnyAsync(o =>
            (o.NationalCode == code || o.CompanyNationalId == code) && (!excludeId.HasValue || o.Id != excludeId.Value));
    }
}
