using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarForm.Models;

/// <summary>One row of the audit log (reports module).</summary>
public class AuditEntry
{
    public int Id { get; set; }

    public AuditEntityType EntityType { get; set; }

    public int? EntityId { get; set; }

    public AuditAction Action { get; set; }

    /// <summary>Short Persian summary shown in the grid.</summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>Free-form details (changed fields, old/new values, ...).</summary>
    public string? Details { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.Now;

    [NotMapped]
    public string JalaliTimestamp => Core.Persian.JalaliDate.FormatWithTime(Timestamp, true);

    [NotMapped]
    public string EntityTypeName => EntityType switch
    {
        AuditEntityType.Owner => "مالک / طرف حساب",
        AuditEntityType.Vehicle => "خودرو",
        AuditEntityType.SalesDocument => "سند فروش",
        AuditEntityType.Company => "شرکت",
        AuditEntityType.Settings => "تنظیمات",
        AuditEntityType.Database => "پشتیبان / پایگاه داده",
        _ => "نامشخص"
    };

    [NotMapped]
    public string ActionName => Action switch
    {
        AuditAction.Create => "ایجاد",
        AuditAction.Update => "ویرایش",
        AuditAction.Delete => "حذف",
        _ => "نامشخص"
    };
}
