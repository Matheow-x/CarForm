namespace CarForm.Models;

/// <summary>Kind of owner: a real person or a legal entity (company).</summary>
public enum OwnerType
{
    Individual = 0,
    LegalEntity = 1
}

/// <summary>Entities that are tracked in the audit log.</summary>
public enum AuditEntityType
{
    Owner = 0,
    Vehicle = 1,
    SalesDocument = 2,
    Company = 3,
    Settings = 4,
    Database = 5
}

public enum AuditAction
{
    Create = 0,
    Update = 1,
    Delete = 2
}

public enum ThemeMode
{
    Light = 0,
    Dark = 1
}

/// <summary>Sections of the printed form. The order is configurable in Document Settings.</summary>
public enum PrintSection
{
    Header,
    Seller,
    Buyer,
    Vehicle,
    Invoice,
    Receipts,
    Notes,
    Signatures
}
