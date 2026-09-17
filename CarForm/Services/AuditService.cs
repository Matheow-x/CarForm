using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CarForm.Data;
using CarForm.Models;
using Microsoft.EntityFrameworkCore;

namespace CarForm.Services;

public class AuditService : IAuditService
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public AuditService(IDbContextFactory<AppDbContext> factory)
    {
        _factory = factory;
    }

    public async Task LogAsync(AuditEntityType entityType, AuditAction action, int? entityId, string summary, string? details = null)
    {
        try
        {
            await using var db = await _factory.CreateDbContextAsync();
            db.AuditEntries.Add(new AuditEntry
            {
                EntityType = entityType,
                Action = action,
                EntityId = entityId,
                Summary = summary,
                Details = details,
                Timestamp = DateTime.Now
            });
            await db.SaveChangesAsync();
        }
        catch
        {
            // Auditing must never break the primary operation.
        }
    }

    public void Log(AuditEntityType entityType, AuditAction action, int? entityId, string summary, string? details = null)
    {
        try
        {
            using var db = _factory.CreateDbContext();
            db.AuditEntries.Add(new AuditEntry
            {
                EntityType = entityType,
                Action = action,
                EntityId = entityId,
                Summary = summary,
                Details = details,
                Timestamp = DateTime.Now
            });
            db.SaveChanges();
        }
        catch
        {
            // Auditing must never break the primary operation.
        }
    }

    public async Task<List<AuditEntry>> SearchAsync(AuditEntityType? entityType, AuditAction? action, DateTime? from, DateTime? to, string? text, int maxResults = 1000)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var query = db.AuditEntries.AsNoTracking().AsQueryable();

        if (entityType.HasValue) query = query.Where(a => a.EntityType == entityType.Value);
        if (action.HasValue) query = query.Where(a => a.Action == action.Value);
        if (from.HasValue) query = query.Where(a => a.Timestamp >= from.Value);
        if (to.HasValue) query = query.Where(a => a.Timestamp <= to.Value);

        if (!string.IsNullOrWhiteSpace(text))
        {
            var term = text.Trim();
            query = query.Where(a => EF.Functions.Like(a.Summary, $"%{term}%") || EF.Functions.Like(a.Details ?? string.Empty, $"%{term}%"));
        }

        return await query.OrderByDescending(a => a.Timestamp).Take(maxResults).ToListAsync();
    }

    public async Task<List<AuditEntry>> GetLatestAsync(int count = 50)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.AuditEntries.AsNoTracking().OrderByDescending(a => a.Timestamp).Take(count).ToListAsync();
    }

    public async Task<int> CountAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.AuditEntries.CountAsync();
    }
}
