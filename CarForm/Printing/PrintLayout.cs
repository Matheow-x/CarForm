using System.Windows;

namespace CarForm.Printing;

/// <summary>
/// Page metrics for the printed form. Everything is expressed in DIPs (96 DPI), which is the unit
/// WPF uses for FixedPage / FixedDocument - so what you see in the preview is what gets printed
/// and what ends up in the PDF.
/// </summary>
public static class PrintLayout
{
    public const double Dpi = 96.0;
    public const double MmPerInch = 25.4;

    public static double Mm(double millimeters) => millimeters * Dpi / MmPerInch;

    public static Size A4 => new(Mm(210), Mm(297));

    public static Size A5 => new(Mm(148), Mm(210));

    public static Size Letter => new(Mm(215.9), Mm(279.4));

    /// <summary>Points (1/72 inch) used by the PDF page box.</summary>
    public static Size ToPoints(Size dipSize) => new(dipSize.Width * 72.0 / Dpi, dipSize.Height * 72.0 / Dpi);
}
