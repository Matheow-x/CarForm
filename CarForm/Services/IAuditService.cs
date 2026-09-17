using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CarForm.Models;

namespace CarForm.Services;

public interface IAuditService
{
    Task LogAsync(AuditEntityType entityType, AuditAction action, int? entityId, string summary, string? details = null);

    /// <summary>Synchronous audit log - safe to call from the UI thread (used by the print path).</summary>
    void Log(AuditEntityType entityType, AuditAction action, int? entityId, string summary, string? details = null);

    Task<List<AuditEntry>> SearchAsync(AuditEntityType? entityType, AuditAction? action, DateTime? from, DateTime? to, string? text, int maxResults = 1000);

    Task<List<AuditEntry>> GetLatestAsync(int count = 50);

    Task<int> CountAsync();
}
