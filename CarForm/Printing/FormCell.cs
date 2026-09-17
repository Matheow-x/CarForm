namespace CarForm.Printing;

/// <summary>One labeled field of the printed form.</summary>
public readonly record struct FormCell(string Label, string Value, int ColumnSpan = 1, bool Emphasis = false);
