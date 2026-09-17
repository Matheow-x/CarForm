using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CarForm.Core.Persian;

namespace CarForm.Services;

/// <summary>
/// Builds document numbers such as "1403/0007" from a configurable template.
/// Tokens: {seq}, {seq:D4}, {yyyy}, {yy}, {mm}, {dd} (Jalali date of the document).
/// </summary>
public class NumberingService : INumberingService
{
    private readonly ISettingsService _settings;
    private readonly IDocumentService _documents;

    public NumberingService(ISettingsService settings, IDocumentService documents)
    {
        _settings = settings;
        _documents = documents;
    }

    public async Task<string> SuggestAsync(DateTime documentDate)
    {
        var sequence = Math.Max(_settings.App.LastNumber + 1, await MaxExistingSequenceAsync() + 1);
        return Format(_settings.App.NumberFormat, sequence, documentDate);
    }

    public async Task<string> ReserveAsync(DateTime documentDate)
    {
        var sequence = Math.Max(_settings.App.LastNumber + 1, await MaxExistingSequenceAsync() + 1);
        _settings.App.LastNumber = sequence;
        await _settings.SaveAppAsync(_settings.App);
        return Format(_settings.App.NumberFormat, sequence, documentDate);
    }

    public async Task SyncSequenceAsync()
    {
        var max = await MaxExistingSequenceAsync();
        if (max > _settings.App.LastNumber)
        {
            _settings.App.LastNumber = max;
            await _settings.SaveAppAsync(_settings.App);
        }
    }

    public static string Format(string? format, int sequence, DateTime documentDate)
    {
        if (string.IsNullOrWhiteSpace(format)) format = "{seq}";

        var (year, month, day) = JalaliDate.ToParts(documentDate);
        var text = format
            .Replace("{yyyy}", year.ToString("0000", CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
            .Replace("{yy}", (year % 100).ToString("00", CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
            .Replace("{mm}", month.ToString("00", CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
            .Replace("{dd}", day.ToString("00", CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase);

        text = Regex.Replace(text, @"\{seq(?::D(\d+))?\}", match =>
        {
            var digits = match.Groups[1].Success && int.TryParse(match.Groups[1].Value, out var d) ? d : 0;
            return digits > 0
                ? sequence.ToString("D" + digits, CultureInfo.InvariantCulture)
                : sequence.ToString(CultureInfo.InvariantCulture);
        }, RegexOptions.IgnoreCase);

        return text;
    }

    /// <summary>
    /// Derives the highest sequence currently used by parsing stored document numbers with a
    /// pattern built from the number template, so that "{yyyy}/{seq:D4}" reads the sequence (1)
    /// and not the year (1405). Falls back to the last digit group of the number.
    /// </summary>
    private async Task<int> MaxExistingSequenceAsync()
    {
        try
        {
            var all = await _documents.SearchAsync(new DocumentSearchCriteria(), 1000);
            var format = _settings.App.NumberFormat;
            var max = 0;
            foreach (var document in all)
            {
                var value = ExtractSequence(document.DocumentNumber ?? string.Empty, format);
                if (value > max && value < 1_000_000) max = value;
            }
            return max;
        }
        catch
        {
            return 0;
        }
    }

    public static int ExtractSequence(string documentNumber, string? format)
    {
        if (string.IsNullOrWhiteSpace(documentNumber)) return 0;

        var regex = BuildSequenceRegex(format);
        if (regex is not null)
        {
            var match = regex.Match(documentNumber.Trim());
            if (match.Success && int.TryParse(match.Groups[1].Value, out var parsed)) return parsed;
        }

        var groups = Regex.Matches(documentNumber, @"\d+");
        if (groups.Count > 0 && int.TryParse(groups[^1].Value, out var last)) return last;

        return 0;
    }

    /// <summary>Turns a number template such as "{yyyy}/{seq:D4}" into a regex capturing the sequence.</summary>
    private static Regex? BuildSequenceRegex(string? format)
    {
        if (string.IsNullOrWhiteSpace(format)) return null;

        var pattern = Regex.Escape(format)
            .Replace(Regex.Escape("{yyyy}"), @"\d{4}")
            .Replace(Regex.Escape("{yy}"), @"\d{2}")
            .Replace(Regex.Escape("{mm}"), @"\d{2}")
            .Replace(Regex.Escape("{dd}"), @"\d{2}");

        pattern = Regex.Replace(pattern, @"\\\{seq(?::D(\d+))?\\\}", match =>
            match.Groups[1].Success ? $@"(\d{{{match.Groups[1].Value}}})" : @"(\d+)");

        if (!pattern.Contains('(')) return null;

        try
        {
            return new Regex("^" + pattern + "$", RegexOptions.Compiled);
        }
        catch (RegexParseException)
        {
            return null;
        }
    }
}
