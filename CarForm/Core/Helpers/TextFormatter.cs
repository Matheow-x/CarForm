using System;
using System.Globalization;
using CarForm.Core.Persian;

namespace CarForm.Core.Helpers;

/// <summary>Display formatting (money, safe strings, Persian digits) shared by UI and printing.</summary>
public static class TextFormatter
{
    public const string EmptyPlaceholder = "—";

    public static string Safe(string? value, bool persianDigits = false)
    {
        if (string.IsNullOrWhiteSpace(value)) return EmptyPlaceholder;
        return persianDigits ? PersianText.ToPersianDigits(value) : value;
    }

    public static string Money(decimal value, bool persianDigits = false)
    {
        var text = value.ToString("N0", CultureInfo.InvariantCulture);
        return persianDigits ? PersianText.ToPersianDigits(text) : text;
    }

    public static string Number(long? value, bool persianDigits = false)
    {
        if (value is null) return EmptyPlaceholder;
        var text = value.Value.ToString("N0", CultureInfo.InvariantCulture);
        return persianDigits ? PersianText.ToPersianDigits(text) : text;
    }

    public static string Number(int? value, bool persianDigits = false)
        => Number(value.HasValue ? (long?)value.Value : null, persianDigits);

    public static string Date(DateTime? value, bool persianDigits = false)
        => value is null ? EmptyPlaceholder : JalaliDate.Format(value, persianDigits);

    /// <summary>Trims long text so that it can never break a fixed print layout.</summary>
    public static string Clamp(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Length <= maxLength ? value : value[..maxLength] + "…";
    }

    public static decimal ParseMoney(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0m;
        var normalized = PersianText.NormalizeDigits(text).Replace(",", string.Empty).Replace("٬", string.Empty).Trim();
        return decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var value) ? value : 0m;
    }

    public static long? ParseLong(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var normalized = PersianText.NormalizeDigits(text).Replace(",", string.Empty).Trim();
        return long.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var value) ? value : null;
    }
}
