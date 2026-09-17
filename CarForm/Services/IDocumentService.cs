using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CarForm.Models;

namespace CarForm.Services;

/// <summary>Filter used by the "Saved documents" page.</summary>
public class DocumentSearchCriteria
{
    public string? Number { get; set; }

    public string? BuyerName { get; set; }

    /// <summary>Make / model / trim / plate.</summary>
    public string? Vehicle { get; set; }

    public string? Vin { get; set; }

    /// <summary>Inclusive Jalali "from" date (already converted to Gregorian).</summary>
    public DateTime? FromDate { get; set; }

    /// <summary>Inclusive Jalali "to" date (already converted to Gregorian, end of day).</summary>
    public DateTime? ToDate { get; set; }

    public bool IsEmpty => string.IsNullOrWhiteSpace(Number)
                           && string.IsNullOrWhiteSpace(BuyerName)
                           && string.IsNullOrWhiteSpace(Vehicle)
                           && string.IsNullOrWhiteSpace(Vin)
                           && !FromDate.HasValue
                           && !ToDate.HasValue;
}

public interface IDocumentService
{
    Task<List<SalesDocument>> SearchAsync(DocumentSearchCriteria criteria, int maxResults = 500);

    Task<SalesDocument?> GetAsync(int id);

    /// <summary>Creates a new in-memory draft including the default invoice rows.</summary>
    Task<SalesDocument> CreateDraftAsync();

    Task<SalesDocument> SaveAsync(SalesDocument document, IEnumerable<DocumentLineItem>? items = null);

    Task DeleteAsync(int id);

    Task<bool> NumberExistsAsync(string number, int? excludeId = null);
}
