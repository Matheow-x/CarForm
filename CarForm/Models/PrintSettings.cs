using System.Collections.Generic;
using System.Linq;
using CarForm.Core.Mvvm;

namespace CarForm.Models;

/// <summary>
/// Everything that controls how the printed form is rendered. Persisted as JSON in the Settings table.
/// Defaults are tuned for A4 + Persian text.
/// </summary>
public class PrintSettings : ObservableObject
{
    private string _fontFamily = "Tahoma";
    private double _fontSize = 11.5;
    private double _titleFontSize = 18;
    private double _headerFontSize = 11;
    private string _primaryColor = "#1F3A5F";
    private string _accentColor = "#C9A227";
    private string _textColor = "#1A1A1A";
    private string _mutedTextColor = "#6B7280";
    private string _borderColor = "#9AA5B1";
    private string _headerText = "فرم فروش خودرو";
    private string _subHeaderText = "قرارداد فروش و فاکتور رسمی";
    private string? _templateImagePath;
    private bool _printTemplateBackground = true;
    private bool _useTemplateOverlay = true;
    private bool _showFieldFrames;
    private string _templateStretch = "Uniform";
    private string _templateVerticalAlignment = "Top";
    private bool _usePersianDigits = true;
    private int _pdfDpi = 300;
    private double _pageMarginMm = 12;
    private int _maxInvoiceRows = 12;
    private bool _autoShrinkText = true;
    private bool _showSignatureBoxes = true;
    private bool _showStampBox = true;
    private double _sectionGapMm = 3;
    private double _rowHeightMm = 9;
    private string _sectionOrder = "Header,Seller,Buyer,Vehicle,Invoice,Receipts,Notes,Signatures";
    private bool _showEmptyValuePlaceholder = true;
    private string _currencyLabel = "ریال";

    #region Typography

    public string FontFamily { get => _fontFamily; set => SetProperty(ref _fontFamily, value); }

    public double FontSize { get => _fontSize; set => SetProperty(ref _fontSize, value); }

    public double TitleFontSize { get => _titleFontSize; set => SetProperty(ref _titleFontSize, value); }

    public double HeaderFontSize { get => _headerFontSize; set => SetProperty(ref _headerFontSize, value); }

    #endregion

    #region Colors

    public string PrimaryColor { get => _primaryColor; set => SetProperty(ref _primaryColor, value); }

    public string AccentColor { get => _accentColor; set => SetProperty(ref _accentColor, value); }

    public string TextColor { get => _textColor; set => SetProperty(ref _textColor, value); }

    public string MutedTextColor { get => _mutedTextColor; set => SetProperty(ref _mutedTextColor, value); }

    public string BorderColor { get => _borderColor; set => SetProperty(ref _borderColor, value); }

    #endregion

    #region Content

    public string HeaderText { get => _headerText; set => SetProperty(ref _headerText, value); }

    public string SubHeaderText { get => _subHeaderText; set => SetProperty(ref _subHeaderText, value); }

    public string CurrencyLabel { get => _currencyLabel; set => SetProperty(ref _currencyLabel, value); }

    public bool ShowEmptyValuePlaceholder { get => _showEmptyValuePlaceholder; set => SetProperty(ref _showEmptyValuePlaceholder, value); }

    public bool ShowSignatureBoxes { get => _showSignatureBoxes; set => SetProperty(ref _showSignatureBoxes, value); }

    public bool ShowStampBox { get => _showStampBox; set => SetProperty(ref _showStampBox, value); }

    #endregion

    #region Layout

    /// <summary>Optional full-page background template (PNG/JPG) drawn under the fields.</summary>
    public string? TemplateImagePath { get => _templateImagePath; set => SetProperty(ref _templateImagePath, value); }

    public bool PrintTemplateBackground { get => _printTemplateBackground; set => SetProperty(ref _printTemplateBackground, value); }

    /// <summary>
    /// When true the values are printed into pre-measured boxes on top of the form image
    /// (the labels, lines, stamp and signature placeholders come from the template itself).
    /// When false the built-in vector form is used instead.
    /// </summary>
    public bool UseTemplateOverlay { get => _useTemplateOverlay; set => SetProperty(ref _useTemplateOverlay, value); }

    /// <summary>Draws the outline of every value box - used to fine tune the layout.</summary>
    public bool ShowFieldFrames { get => _showFieldFrames; set => SetProperty(ref _showFieldFrames, value); }

    /// <summary>"Uniform" keeps the aspect ratio of the template image, "Fill" stretches it to the page.</summary>
    public string TemplateStretch { get => _templateStretch; set => SetProperty(ref _templateStretch, value); }

    public string TemplateVerticalAlignment { get => _templateVerticalAlignment; set => SetProperty(ref _templateVerticalAlignment, value); }

    public double PageMarginMm { get => _pageMarginMm; set => SetProperty(ref _pageMarginMm, value); }

    public double SectionGapMm { get => _sectionGapMm; set => SetProperty(ref _sectionGapMm, value); }

    public double RowHeightMm { get => _rowHeightMm; set => SetProperty(ref _rowHeightMm, value); }

    public bool UsePersianDigits { get => _usePersianDigits; set => SetProperty(ref _usePersianDigits, value); }

    public bool AutoShrinkText { get => _autoShrinkText; set => SetProperty(ref _autoShrinkText, value); }

    public int MaxInvoiceRows { get => _maxInvoiceRows; set => SetProperty(ref _maxInvoiceRows, value); }

    public int PdfDpi { get => _pdfDpi; set => SetProperty(ref _pdfDpi, value); }

    /// <summary>
    /// Order of the printed sections. Re-order the comma separated keys to change the printed form
    /// without touching the renderer. Unknown keys are ignored; missing keys keep their default slot.
    /// </summary>
    public string SectionOrder { get => _sectionOrder; set => SetProperty(ref _sectionOrder, value); }

    #endregion

    public IReadOnlyList<PrintSection> GetSectionOrder()
    {
        var parsed = new List<PrintSection>();
        if (!string.IsNullOrWhiteSpace(SectionOrder))
        {
            foreach (var part in SectionOrder.Split(',', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries))
            {
                if (System.Enum.TryParse<PrintSection>(part, true, out var section) && !parsed.Contains(section))
                    parsed.Add(section);
            }
        }

        foreach (var section in System.Enum.GetValues<PrintSection>())
        {
            if (!parsed.Contains(section)) parsed.Add(section);
        }

        return parsed;
    }

    public PrintSettings Clone()
    {
        var copy = (PrintSettings)MemberwiseClone();
        return copy;
    }
}
