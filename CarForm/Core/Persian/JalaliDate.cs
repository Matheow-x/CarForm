using System;
using System.Globalization;
using System.Text;

namespace CarForm.Core.Persian;

/// <summary>
/// Robust Jalali (Shamsi / Persian) calendar helpers.
/// Dates are always stored internally as Gregorian <see cref="DateTime"/> (for sorting / filtering),
/// and converted to / from "YYYY/MM/DD" Jalali strings at the UI boundary.
/// </summary>
public static class JalaliDate
{
    private static readonly PersianCalendar Calendar = new();

    private const int MinYear = 1200;
    private const int MaxYear = 1600;

    public static DateTime Today => DateTime.Today;

    public static string TodayString => Format(DateTime.Today);

    #region Formatting

    /// <summary>Formats a Gregorian date as a Jalali "yyyy/MM/dd" string.</summary>
    public static string Format(DateTime? date, bool persianDigits = false)
    {
        if (date is null) return string.Empty;
        var d = date.Value;
        var text = $"{Calendar.GetYear(d):0000}/{Calendar.GetMonth(d):00}/{Calendar.GetDayOfMonth(d):00}";
        return persianDigits ? PersianText.ToPersianDigits(text) : text;
    }

    /// <summary>Formats a Gregorian date-time as a Jalali "yyyy/MM/dd HH:mm" string.</summary>
    public static string FormatWithTime(DateTime? date, bool persianDigits = false)
    {
        if (date is null) return string.Empty;
        var d = date.Value;
        var text = $"{Calendar.GetYear(d):0000}/{Calendar.GetMonth(d):00}/{Calendar.GetDayOfMonth(d):00} " +
                   $"{Calendar.GetHour(d):00}:{Calendar.GetMinute(d):00}";
        return persianDigits ? PersianText.ToPersianDigits(text) : text;
    }

    public static (int Year, int Month, int Day) ToParts(DateTime date)
        => (Calendar.GetYear(date), Calendar.GetMonth(date), Calendar.GetDayOfMonth(date));

    #endregion

    #region Parsing

    public static bool TryParse(string? text, out DateTime result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(text)) return false;

        var normalized = PersianText.NormalizeDigits(text.Trim());
        normalized = normalized.Replace('\\', '/').Replace('-', '/').Replace('.', '/');
        var parts = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 3) return false;

        if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var year)) return false;
        if (!int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var month)) return false;
        if (!int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var day)) return false;

        if (year < MinYear || year > MaxYear) return false;
        if (year < 100) year += 1300; // tolerant: 02/01/01 -> 1302/01/01
        if (month < 1 || month > 12) return false;

        int daysInMonth;
        try
        {
            daysInMonth = Calendar.GetDaysInMonth(year, month);
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
        if (day < 1 || day > daysInMonth) return false;

        try
        {
            result = Calendar.ToDateTime(year, month, day, 0, 0, 0, 0);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    /// <summary>Parses a Jalali "yyyy/MM/dd" string; returns null when empty or invalid.</summary>
    public static DateTime? ParseOrNull(string? text)
        => TryParse(text, out var date) ? date : null;

    /// <summary>Parses a Jalali string, throwing a friendly exception when the value is invalid.</summary>
    public static DateTime Parse(string? text, string fieldName = "تاریخ")
    {
        if (TryParse(text, out var date)) return date;
        throw new FormatException($"{fieldName} وارد شده معتبر نیست. قالب صحیح: 1400/01/01");
    }

    #endregion

    #region Conversion / arithmetic

    public static DateTime FromParts(int year, int month, int day)
        => Calendar.ToDateTime(year, month, day, 0, 0, 0, 0);

    /// <summary>Gregorian date-time of the start of the current Jalali month / year helpers.</summary>
    public static DateTime StartOfMonth(int year, int month) => FromParts(year, month, 1);

    public static string MonthName(int month) => month switch
    {
        1 => "فروردین",
        2 => "اردیبهشت",
        3 => "خرداد",
        4 => "تیر",
        5 => "مرداد",
        6 => "شهریور",
        7 => "مهر",
        8 => "آبان",
        9 => "آذر",
        10 => "دی",
        11 => "بهمن",
        12 => "اسفند",
        _ => string.Empty
    };

    public static DateTime AddYears(DateTime date, int years) => Calendar.AddYears(date, years);

    public static DateTime AddMonths(DateTime date, int months) => Calendar.AddMonths(date, months);

    public static DateTime AddDays(DateTime date, int days) => date.AddDays(days);

    /// <summary>End of the given Jalali day (23:59:59) - useful for "to date" filters.</summary>
    public static DateTime EndOfDay(DateTime date) => date.Date.AddDays(1).AddSeconds(-1);

    /// <summary>Parses a Jalali date and returns the last instant of that day, or null when empty/invalid.</summary>
    public static DateTime? EndOfDayOrNull(string? text) => TryParse(text, out var date) ? EndOfDay(date) : null;

    #endregion
}

/// <summary>Persian digit / text helpers.</summary>
public static class PersianText
{
    public static string ToPersianDigits(string? input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        var builder = new StringBuilder(input.Length);
        foreach (var ch in input)
        {
            builder.Append(ch switch
            {
                >= '0' and <= '9' => (char)(ch - '0' + '۰'),
                '.' => '٫',
                _ => ch
            });
        }
        return builder.ToString();
    }

    /// <summary>Converts Persian / Arabic digits inside a string into ASCII digits (for parsing).</summary>
    public static string NormalizeDigits(string? input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        var builder = new StringBuilder(input.Length);
        foreach (var ch in input)
        {
            builder.Append(ch switch
            {
                >= '۰' and <= '۹' => (char)(ch - '۰' + '0'),
                >= '٠' and <= '٩' => (char)(ch - '٠' + '0'),
                '٫' or '٬' or ',' => '/',
                'ي' => 'ی',
                'ك' => 'ک',
                _ => ch
            });
        }
        return builder.ToString();
    }

    public static bool IsNullOrWhiteSpace(string? value) => string.IsNullOrWhiteSpace(value);
}
