using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CarForm.Data;
using CarForm.Models;
using Microsoft.EntityFrameworkCore;

namespace CarForm.Services;

public class DocumentService : IDocumentService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly IAuditService _audit;
    private readonly ISettingsService _settings;

    public DocumentService(IDbContextFactory<AppDbContext> factory, IAuditService audit, ISettingsService settings)
    {
        _factory = factory;
        _audit = audit;
        _settings = settings;
    }

    public async Task<List<SalesDocument>> SearchAsync(DocumentSearchCriteria criteria, int maxResults = 500)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var query = db.SalesDocuments.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(criteria.Number))
        {
            var like = $"%{criteria.Number.Trim()}%";
            query = query.Where(d => EF.Functions.Like(d.DocumentNumber, like));
        }

        if (!string.IsNullOrWhiteSpace(criteria.BuyerName))
        {
            var like = $"%{criteria.BuyerName.Trim()}%";
            query = query.Where(d =>
                EF.Functions.Like(d.BuyerName ?? string.Empty, like) ||
                EF.Functions.Like(d.BuyerNationalCode ?? string.Empty, like));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Vehicle))
        {
            var like = $"%{criteria.Vehicle.Trim()}%";
            query = query.Where(d =>
                EF.Functions.Like(d.VehicleMake ?? string.Empty, like) ||
                EF.Functions.Like(d.VehicleModel ?? string.Empty, like) ||
                EF.Functions.Like(d.VehicleTrim ?? string.Empty, like) ||
                EF.Functions.Like(d.VehiclePlate ?? string.Empty, like));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Vin))
        {
            var like = $"%{criteria.Vin.Trim()}%";
            query = query.Where(d =>
                EF.Functions.Like(d.VehicleVin ?? string.Empty, like) ||
                EF.Functions.Like(d.VehicleChassisNo ?? string.Empty, like) ||
                EF.Functions.Like(d.VehicleEngineNo ?? string.Empty, like));
        }

        if (criteria.FromDate.HasValue) query = query.Where(d => d.DocumentDate >= criteria.FromDate.Value);
        if (criteria.ToDate.HasValue) query = query.Where(d => d.DocumentDate <= criteria.ToDate.Value);

        return await query
            .OrderByDescending(d => d.DocumentDate)
            .ThenByDescending(d => d.Id)
            .Take(maxResults)
            .ToListAsync();
    }

    public async Task<SalesDocument?> GetAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.SalesDocuments
            .Include(d => d.Items)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public Task<SalesDocument> CreateDraftAsync()
    {
        var document = new SalesDocument
        {
            DocumentDate = DateTime.Today,
            DocumentNumber = string.Empty
        };

        var order = 0;
        foreach (var title in _settings.App.DefaultInvoiceItems)
        {
            document.Items.Add(new DocumentLineItem { Title = title, Amount = 0m, SortOrder = order++ });
        }

        return Task.FromResult(document);
    }

    public async Task<SalesDocument> SaveAsync(SalesDocument document, IEnumerable<DocumentLineItem>? items = null)
    {
        await using var db = await _factory.CreateDbContextAsync();

        SalesDocument entity;
        var isNew = document.Id == 0;
        if (isNew)
        {
            entity = new SalesDocument();
            db.SalesDocuments.Add(entity);
        }
        else
        {
            entity = await db.SalesDocuments.Include(d => d.Items).FirstAsync(d => d.Id == document.Id);
        }

        var lineItems = (items ?? document.Items)
            .Where(i => !string.IsNullOrWhiteSpace(i.Title))
            .OrderBy(i => i.SortOrder)
            .ToList();

        db.Entry(entity).CurrentValues.SetValues(document);
        await db.SaveChangesAsync();

        // Replace the invoice rows: explicit delete first, then insert (keeps ordering deterministic).
        if (!isNew)
        {
            var existingItems = await db.DocumentLineItems.Where(i => i.SalesDocumentId == entity.Id).ToListAsync();
            if (existingItems.Count > 0)
            {
                db.DocumentLineItems.RemoveRange(existingItems);
                await db.SaveChangesAsync();
            }
            entity.Items.Clear();
        }

        var order = 0;
        foreach (var item in lineItems)
        {
            db.DocumentLineItems.Add(new DocumentLineItem
            {
                SalesDocumentId = entity.Id,
                Title = item.Title.Trim(),
                Amount = item.Amount,
                SortOrder = order++
            });
        }
        await db.SaveChangesAsync();

        document.Id = entity.Id;
        document.CreatedAt = entity.CreatedAt;
        document.UpdatedAt = entity.UpdatedAt;

        await _audit.LogAsync(AuditEntityType.SalesDocument, isNew ? AuditAction.Create : AuditAction.Update, entity.Id,
            $"{(isNew ? "ایجاد" : "ویرایش")} سند فروش شماره {entity.DocumentNumber}",
            $"خریدار: {entity.BuyerName} | خودرو: {entity.VehicleTitle} | مبلغ کل: {entity.TotalAmount:N0}");

        return document;
    }

    public async Task DeleteAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var entity = await db.SalesDocuments.FirstOrDefaultAsync(d => d.Id == id);
        if (entity is null) return;

        db.DocumentLineItems.RemoveRange(await db.DocumentLineItems.Where(i => i.SalesDocumentId == id).ToListAsync());
        db.SalesDocuments.Remove(entity);
        await db.SaveChangesAsync();

        await _audit.LogAsync(AuditEntityType.SalesDocument, AuditAction.Delete, id,
            $"حذف سند فروش شماره {entity.DocumentNumber}");
    }

    public async Task<bool> NumberExistsAsync(string number, int? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(number)) return false;

        await using var db = await _factory.CreateDbContextAsync();
        var value = number.Trim();
        return await db.SalesDocuments.AnyAsync(d =>
            d.DocumentNumber == value && (!excludeId.HasValue || d.Id != excludeId.Value));
    }
}
