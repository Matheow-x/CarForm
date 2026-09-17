using System;
using System.Globalization;
using System.Windows.Media;

namespace CarForm.Core.Helpers;

public static class ColorHelper
{
    public static Color Parse(string? hex, Color fallback)
    {
        try
        {
            var value = (hex ?? string.Empty).Trim().TrimStart('#');
            if (value.Length == 3) value = $"{value[0]}{value[0]}{value[1]}{value[1]}{value[2]}{value[2]}";
            if (value.Length != 6 && value.Length != 8) return fallback;
            var argb = uint.Parse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            if (value.Length == 6) argb |= 0xFF000000;
            return Color.FromArgb(
                (byte)((argb >> 24) & 0xFF),
                (byte)((argb >> 16) & 0xFF),
                (byte)((argb >> 8) & 0xFF),
                (byte)(argb & 0xFF));
        }
        catch
        {
            return fallback;
        }
    }

    public static Color Parse(string? hex) => Parse(hex, Colors.Black);

    public static SolidColorBrush Brush(string? hex, Color fallback) => new(Parse(hex, fallback));

    public static SolidColorBrush Brush(string? hex) => new(Parse(hex));

    public static string ToHex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

    /// <summary>A readable foreground (black or white) for the given background.</summary>
    public static Color Contrast(Color background)
    {
        var luminance = (0.299 * background.R + 0.587 * background.G + 0.114 * background.B) / 255.0;
        return luminance > 0.6 ? Colors.Black : Colors.White;
    }
}
