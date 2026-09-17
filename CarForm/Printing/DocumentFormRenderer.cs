using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CarForm.Core.Helpers;
using CarForm.Core.Persian;
using CarForm.Models;
using CarForm.Services;

namespace CarForm.Printing;

/// <summary>
/// Builds the printed / exported form as a WPF <see cref="FixedDocument"/>.
/// The document renders right-to-left Persian text with full shaping support and is used
/// for print preview (DocumentViewer), for printing (PrintDialog) and for PDF export
/// (rasterised by <see cref="FixedDocumentExporter"/>), so all three outputs match exactly.
/// </summary>
public sealed class DocumentFormRenderer
{
    private readonly SalesDocument _document;
    private readonly Company _company;
    private readonly PrintSettings _settings;
    private readonly IImageStore _images;

    private readonly FontFamily _font;
    private readonly SolidColorBrush _textBrush;
    private readonly SolidColorBrush _mutedBrush;
    private readonly SolidColorBrush _borderBrush;
    private readonly SolidColorBrush _primaryBrush;
    private readonly SolidColorBrush _accentBrush;
    private readonly SolidColorBrush _headerTextBrush;
    private readonly bool _persianDigits;
    private readonly double _gap;

    public DocumentFormRenderer(SalesDocument document, Company company, PrintSettings settings, IImageStore images)
    {
        _document = document;
        _company = company;
        _settings = settings;
        _images = images;

        _font = new FontFamily(settings.FontFamily);
        _textBrush = ColorHelper.Brush(settings.TextColor, Colors.Black);
        _mutedBrush = ColorHelper.Brush(settings.MutedTextColor, Colors.Gray);
        _borderBrush = ColorHelper.Brush(settings.BorderColor, Colors.Gray);
        _primaryBrush = ColorHelper.Brush(settings.PrimaryColor, Colors.SteelBlue);
        _accentBrush = ColorHelper.Brush(settings.AccentColor, Colors.Goldenrod);
        _headerTextBrush = new SolidColorBrush(ColorHelper.Contrast(ColorHelper.Parse(settings.PrimaryColor)));
        _persianDigits = settings.UsePersianDigits;
        _gap = PrintLayout.Mm(settings.SectionGapMm);

        foreach (var brush in new[] { _textBrush, _mutedBrush, _borderBrush, _primaryBrush, _accentBrush, _headerTextBrush })
        {
            brush.Freeze();
        }
    }

    #region Public API

    public Size PageSize => PrintLayout.A4;

    public FixedDocument Build()
    {
        var document = new FixedDocument();
        var page = BuildPage();

        var pageContent = new PageContent();
        ((IAddChild)pageContent).AddChild(page);
        document.Pages.Add(pageContent);

        return document;
    }

    public FixedPage BuildPage()
    {
        var page = new FixedPage
        {
            Width = PageSize.Width,
            Height = PageSize.Height,
            Background = Brushes.White,
            FlowDirection = FlowDirection.RightToLeft,
            Language = XmlLanguage.GetLanguage("fa-IR")
        };

        var margin = PrintLayout.Mm(_settings.PageMarginMm);
        var availableWidth = PageSize.Width - (2 * margin);
        var availableHeight = PageSize.Height - (2 * margin);

        var background = LoadTemplate();
        if (background is not null && _settings.PrintTemplateBackground)
        {
            page.Background = new ImageBrush(background) { Stretch = Stretch.Fill };
        }

        var content = new StackPanel
        {
            Width = availableWidth,
            Orientation = Orientation.Vertical,
            FlowDirection = FlowDirection.RightToLeft
        };

        foreach (var section in _settings.GetSectionOrder())
        {
            var element = section switch
            {
                PrintSection.Header => BuildHeader(availableWidth),
                PrintSection.Seller => BuildCompanySection(),
                PrintSection.Buyer => BuildBuyerSection(),
                PrintSection.Vehicle => BuildVehicleSection(),
                PrintSection.Invoice => BuildInvoiceSection(),
                PrintSection.Receipts => BuildReceiptsSection(),
                PrintSection.Notes => BuildNotesSection(),
                PrintSection.Signatures => BuildSignatureSection(),
                _ => null
            };

            if (element is null) continue;
            element.Margin = new Thickness(0, 0, 0, _gap);
            content.Children.Add(element);
        }

        // ---- overflow guard -------------------------------------------------
        // If the content grows beyond one page (long notes, many invoice rows, big font),
        // scale it down instead of clipping or spilling onto a second page.
        content.Measure(new Size(availableWidth, double.PositiveInfinity));
        var desired = content.DesiredSize.Height;
        if (double.IsFinite(desired) && desired > availableHeight + 0.5)
        {
            var scale = Math.Max(0.5, availableHeight / desired);
            content.LayoutTransform = new ScaleTransform(scale, scale);
        }

        FixedPage.SetLeft(content, margin);
        FixedPage.SetTop(content, margin);
        page.Children.Add(content);

        return page;
    }

    #endregion

    #region Sections

    private Border BuildHeader(double availableWidth)
    {
        var border = new Border
        {
            BorderBrush = _primaryBrush,
            BorderThickness = new Thickness(0, 0, 0, 2.5),
            Padding = new Thickness(0, 0, 0, 4),
            SnapsToDevicePixels = true
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });     // logo
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });        // doc info

        var logo = BuildLogo(80);
        Grid.SetColumn(logo, 0);
        grid.Children.Add(logo);

        var titles = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0, 6, 0) };
        titles.Children.Add(Text(_settings.HeaderText, _settings.TitleFontSize, FontWeights.Bold, _primaryBrush, TextAlignment.Center));
        if (!string.IsNullOrWhiteSpace(_settings.SubHeaderText))
        {
            titles.Children.Add(Text(_settings.SubHeaderText, _settings.FontSize, FontWeights.Normal, _mutedBrush, TextAlignment.Center));
        }
        if (!string.IsNullOrWhiteSpace(_company.Name))
        {
            titles.Children.Add(Text(_company.Name, _settings.FontSize + 1, FontWeights.SemiBold, _textBrush, TextAlignment.Center));
        }
        Grid.SetColumn(titles, 1);
        grid.Children.Add(titles);

        var info = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        info.Children.Add(InfoRow("شماره سند:", _document.DocumentNumber, emphasize: true));
        info.Children.Add(InfoRow("تاریخ سند:", JalaliDate.Format(_document.DocumentDate, _persianDigits)));
        Grid.SetColumn(info, 2);
        grid.Children.Add(info);

        border.Child = grid;
        return border;
    }

    private Border BuildCompanySection()
    {
        return Section("مشخصات فروشنده (شرکت)", 3,
            new FormCell("نام شرکت / نمایشگاه", D(_company.Name)),
            new FormCell("تلفن", D(_company.Phone)),
            new FormCell("کد پستی", D(_company.PostalCode)),
            new FormCell("نشانی", D(_company.Address), 3),
            new FormCell("کد اقتصادی", D(_company.EconomicCode)),
            new FormCell("شناسه ملی", D(_company.NationalId)),
            new FormCell("شماره پرونده مالیاتی", D(_company.TaxFileNumber)),
            new FormCell("شماره دستورالعمل کاردکس", D(_company.KardexInstructionNo)),
            new FormCell("شماره دستورالعمل شماره‌گذاری", D(_company.NumberingInstructionNo)));
    }

    private Border BuildBuyerSection()
    {
        var isCompany = _document.BuyerType == OwnerType.LegalEntity;

        return Section(isCompany ? "مشخصات خریدار (شخص حقوقی)" : "مشخصات خریدار (شخص حقیقی)", 3,
            new FormCell(isCompany ? "نام شرکت" : "نام خریدار", D(_document.BuyerName), 2, true),
            new FormCell(isCompany ? "شناسه ملی / کد اقتصادی" : "کد ملی", D(_document.BuyerNationalCode)),
            new FormCell(isCompany ? "شماره ثبت" : "شماره شناسنامه", D(isCompany ? _document.BuyerRegistrationNumber : _document.BuyerIdNumber)),
            new FormCell(isCompany ? "نام نماینده" : "نام پدر", D(isCompany ? _document.BuyerRepresentativeName : _document.BuyerFatherName)),
            new FormCell("تلفن ثابت", D(_document.BuyerPhone)),
            new FormCell("تلفن همراه", D(_document.BuyerMobile)),
            new FormCell("کد پستی", D(_document.BuyerPostalCode)),
            new FormCell("نشانی", D(_document.BuyerAddress), 3));
    }

    private Border BuildVehicleSection()
    {
        return Section("مشخصات خودرو", 4,
            new FormCell("سازنده / برند", D(_document.VehicleMake)),
            new FormCell("مدل", D(_document.VehicleModel)),
            new FormCell("تیپ / کلاس", D(_document.VehicleTrim)),
            new FormCell("سال ساخت", D(_document.VehicleYear)),
            new FormCell("رنگ", D(_document.VehicleColor)),
            new FormCell("شماره پلاک", D(_document.VehiclePlate)),
            new FormCell("نوع سوخت", D(_document.VehicleFuelType)),
            new FormCell("کارکرد (کیلومتر)", TextFormatter.Number(_document.VehicleMileage, _persianDigits)),
            new FormCell("شماره شناسایی خودرو (VIN)", D(_document.VehicleVin), 2),
            new FormCell("شماره موتور", D(_document.VehicleEngineNo)),
            new FormCell("شماره شاسی", D(_document.VehicleChassisNo)),
            new FormCell("نوع بدنه", D(_document.VehicleBodyType), 2));
    }

    private Border BuildReceiptsSection()
    {
        var panel = new StackPanel();

        panel.Children.Add(Section("رسید مالیات و عوارض", 3,
            new FormCell("شماره فیش", D(_document.TaxReceiptNo)),
            new FormCell("تاریخ فیش", JalaliDate.Format(_document.TaxReceiptDate, _persianDigits)),
            new FormCell("شناسه پرداخت", D(_document.TaxPaymentId))));

        panel.Children.Add(Section("رسید عوارض", 3,
            new FormCell("شماره فیش", D(_document.TollReceiptNo)),
            new FormCell("تاریخ فیش", JalaliDate.Format(_document.TollReceiptDate, _persianDigits)),
            new FormCell("شناسه پرداخت", D(_document.TollPaymentId))));

        panel.Children.Add(Section("بیمه شخص ثالث", 4,
            new FormCell("شماره بیمه‌نامه", D(_document.InsurancePolicyNo)),
            new FormCell("شرکت بیمه", D(_document.InsuranceCompany)),
            new FormCell("تاریخ صدور", JalaliDate.Format(_document.InsuranceIssueDate, _persianDigits)),
            new FormCell("تاریخ انقضا", JalaliDate.Format(_document.InsuranceExpiryDate, _persianDigits))));

        var wrapper = new Border { Child = panel };
        return wrapper;
    }

    private Border BuildInvoiceSection()
    {
        var border = new Border
        {
            BorderBrush = _borderBrush,
            BorderThickness = new Thickness(1),
            SnapsToDevicePixels = true
        };

        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });   // title
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });   // table header
        border.Child = grid;

        var title = new Border
        {
            Background = _primaryBrush,
            Padding = new Thickness(6, 3, 6, 3),
            Child = Text("صورت‌حساب فروش", _settings.HeaderFontSize, FontWeights.SemiBold, _headerTextBrush)
        };
        Grid.SetRow(title, 0);
        grid.Children.Add(title);

        var table = new Grid();
        table.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(46) });
        table.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        table.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });

        int row = 0;
        table.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        AddCell(table, "ردیف", row, 0, header: true, alignment: TextAlignment.Center);
        AddCell(table, "شرح", row, 1, header: true, alignment: TextAlignment.Center);
        AddCell(table, "مبلغ (" + _settings.CurrencyLabel + ")", row, 2, header: true, alignment: TextAlignment.Center);
        row++;

        var items = _document.Items.Where(i => !string.IsNullOrWhiteSpace(i.Title)).OrderBy(i => i.SortOrder).ToList();
        var maxRows = Math.Max(3, _settings.MaxInvoiceRows);

        var shown = items;
        decimal remainder = 0m;
        var remainderCount = 0;
        if (items.Count > maxRows)
        {
            shown = items.Take(maxRows - 1).ToList();
            remainder = items.Skip(maxRows - 1).Sum(i => i.Amount);
            remainderCount = items.Count - shown.Count;
        }

        var index = 1;
        foreach (var item in shown)
        {
            table.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            AddCell(table, TextFormatter.Number(index++, _persianDigits), row, 0, alignment: TextAlignment.Center);
            AddCell(table, item.Title, row, 1);
            AddCell(table, TextFormatter.Money(item.Amount, _persianDigits), row, 2, alignment: TextAlignment.Center);
            row++;
        }

        if (remainderCount > 0)
        {
            table.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            AddCell(table, TextFormatter.Number(index++, _persianDigits), row, 0, alignment: TextAlignment.Center);
            AddCell(table, $"سایر موارد ({TextFormatter.Number(remainderCount, _persianDigits)} مورد)", row, 1);
            AddCell(table, TextFormatter.Money(remainder, _persianDigits), row, 2, alignment: TextAlignment.Center);
            row++;
        }

        // keep the table looking like the template even when there are only a few rows
        while (index <= 5)
        {
            table.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            AddCell(table, TextFormatter.Number(index++, _persianDigits), row, 0, alignment: TextAlignment.Center);
            AddCell(table, string.Empty, row, 1);
            AddCell(table, string.Empty, row, 2);
            row++;
        }

        // total row
        table.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        AddCell(table, "جمع کل", row, 0, header: true, alignment: TextAlignment.Center, columnSpan: 2);
        AddCell(table, TextFormatter.Money(_document.Items.Sum(i => i.Amount), _persianDigits) + " " + _settings.CurrencyLabel,
            row, 2, header: true, alignment: TextAlignment.Center, emphasis: true);

        Grid.SetRow(table, 1);
        grid.Children.Add(table);

        return border;
    }

    private Border BuildNotesSection()
    {
        var border = new Border
        {
            BorderBrush = _borderBrush,
            BorderThickness = new Thickness(1),
            SnapsToDevicePixels = true
        };

        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var title = new Border
        {
            Background = _primaryBrush,
            Padding = new Thickness(6, 3, 6, 3),
            Child = Text("توضیحات", _settings.HeaderFontSize, FontWeights.SemiBold, _headerTextBrush)
        };
        Grid.SetRow(title, 0);
        grid.Children.Add(title);

        var notesText = string.IsNullOrWhiteSpace(_document.CombinedNotes) ? " " : _document.CombinedNotes;
        var body = new Border
        {
            Padding = new Thickness(6),
            MinHeight = PrintLayout.Mm(18),
            Child = new TextBlock
            {
                Text = notesText,
                FontFamily = _font,
                FontSize = _settings.FontSize,
                Foreground = _textBrush,
                TextWrapping = TextWrapping.Wrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
                FlowDirection = FlowDirection.RightToLeft,
                LineHeight = _settings.FontSize * 1.6
            }
        };
        Grid.SetRow(body, 1);
        grid.Children.Add(body);

        border.Child = grid;
        return border;
    }

    private Border BuildSignatureSection()
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        if (!_settings.ShowSignatureBoxes)
        {
            return new Border { Child = grid, Height = 0 };
        }

        AddSignatureBox(grid, "مهر و امضای فروشنده", 0);
        AddSignatureBox(grid, "امضای خریدار", 1);

        if (_settings.ShowStampBox)
        {
            AddSignatureBox(grid, "محل الصاق تمبر / اثر انگشت", 2);
        }

        return new Border
        {
            Child = grid,
            BorderBrush = _borderBrush,
            BorderThickness = new Thickness(0),
            SnapsToDevicePixels = true
        };
    }

    private void AddSignatureBox(Grid grid, string title, int column)
    {
        var box = new Border
        {
            BorderBrush = _borderBrush,
            BorderThickness = new Thickness(1),
            Margin = new Thickness(2, 0, 2, 0),
            MinHeight = PrintLayout.Mm(22),
            SnapsToDevicePixels = true
        };

        var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Bottom };
        stack.Children.Add(new Border { Height = PrintLayout.Mm(14) });
        stack.Children.Add(Text(title, _settings.FontSize - 1, FontWeights.Normal, _mutedBrush, TextAlignment.Center));
        box.Child = stack;

        Grid.SetColumn(box, column);
        grid.Children.Add(box);
    }

    #endregion

    #region Building blocks

    private Border Section(string title, int columns, params FormCell[] cells)
    {
        var border = new Border
        {
            BorderBrush = _borderBrush,
            BorderThickness = new Thickness(1),
            SnapsToDevicePixels = true
        };

        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        for (var i = 0; i < columns; i++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        }

        var header = new Border
        {
            Background = _primaryBrush,
            Padding = new Thickness(6, 3, 6, 3),
            Child = Text(title, _settings.HeaderFontSize, FontWeights.SemiBold, _headerTextBrush)
        };
        Grid.SetRow(header, 0);
        Grid.SetColumnSpan(header, columns);
        grid.Children.Add(header);

        var row = 1;
        var column = 0;
        var cellWidth = 0d;
        var contentWidth = PageSize.Width - 2 * PrintLayout.Mm(_settings.PageMarginMm);

        foreach (var cell in cells)
        {
            var span = Math.Max(1, Math.Min(cell.ColumnSpan, columns));
            if (column + span > columns)
            {
                column = 0;
                row++;
            }

            while (grid.RowDefinitions.Count <= row)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }

            cellWidth = contentWidth / columns * span;
            var element = BuildCell(cell, cellWidth);

            Grid.SetRow(element, row);
            Grid.SetColumn(element, column);
            Grid.SetColumnSpan(element, span);
            grid.Children.Add(element);

            column += span;
            if (column >= columns)
            {
                column = 0;
                row++;
            }
        }

        border.Child = grid;
        return border;
    }

    private Border BuildCell(FormCell cell, double availableWidth)
    {
        var border = new Border
        {
            BorderBrush = _borderBrush,
            BorderThickness = new Thickness(0.5),
            Padding = new Thickness(5, 3, 5, 3),
            SnapsToDevicePixels = true
        };

        var stack = new StackPanel { Orientation = Orientation.Vertical };

        var label = new TextBlock
        {
            Text = cell.Label,
            FontFamily = _font,
            FontSize = Math.Max(6.5, _settings.FontSize - 2.5),
            Foreground = _mutedBrush,
            FlowDirection = FlowDirection.RightToLeft,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Margin = new Thickness(0)
        };
        stack.Children.Add(label);

        var value = new TextBlock
        {
            Text = cell.Value,
            FontFamily = _font,
            FontSize = _settings.FontSize,
            Foreground = _textBrush,
            FontWeight = cell.Emphasis ? FontWeights.SemiBold : FontWeights.Normal,
            TextWrapping = TextWrapping.Wrap,
            TextTrimming = TextTrimming.CharacterEllipsis,
            FlowDirection = FlowDirection.RightToLeft,
            TextAlignment = TextAlignment.Right,
            LineHeight = _settings.FontSize * 1.45,
            Margin = new Thickness(0, 1, 0, 0)
        };

        if (_settings.AutoShrinkText)
        {
            value.FontSize = FitFontSize(cell.Value, _settings.FontSize, availableWidth - 12, _settings.FontSize - 2.5, 3);
        }

        stack.Children.Add(value);
        border.Child = stack;
        return border;
    }

    private void AddCell(Grid grid, string text, int row, int column, bool header = false, TextAlignment alignment = TextAlignment.Right, int columnSpan = 1, bool emphasis = false)
    {
        var border = new Border
        {
            BorderBrush = _borderBrush,
            BorderThickness = new Thickness(0.5),
            Padding = new Thickness(5, 3, 5, 3),
            Background = header ? _accentBrush : Brushes.Transparent,
            SnapsToDevicePixels = true
        };

        var block = new TextBlock
        {
            Text = text,
            FontFamily = _font,
            FontSize = _settings.FontSize,
            Foreground = header ? new SolidColorBrush(ColorHelper.Contrast(ColorHelper.Parse(_settings.AccentColor))) : _textBrush,
            FontWeight = header || emphasis ? FontWeights.SemiBold : FontWeights.Normal,
            TextWrapping = TextWrapping.Wrap,
            TextTrimming = TextTrimming.CharacterEllipsis,
            TextAlignment = alignment,
            FlowDirection = FlowDirection.RightToLeft
        };

        if (_settings.AutoShrinkText && !string.IsNullOrEmpty(text))
        {
            block.FontSize = FitFontSize(text, _settings.FontSize, 180, _settings.FontSize - 2.5, 2);
        }

        border.Child = block;
        Grid.SetRow(border, row);
        Grid.SetColumn(border, column);
        Grid.SetColumnSpan(border, columnSpan);
        grid.Children.Add(border);
    }

    private TextBlock Text(string text, double fontSize, FontWeight weight, Brush foreground, TextAlignment alignment = TextAlignment.Right)
        => new()
        {
            Text = text,
            FontFamily = _font,
            FontSize = fontSize,
            FontWeight = weight,
            Foreground = foreground,
            TextAlignment = alignment,
            FlowDirection = FlowDirection.RightToLeft,
            TextTrimming = TextTrimming.CharacterEllipsis
        };

    private StackPanel InfoRow(string label, string value, bool emphasize = false)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 1, 0, 1) };
        panel.Children.Add(Text(label, _settings.FontSize - 1, FontWeights.Normal, _mutedBrush));
        panel.Children.Add(Text(" " + value, emphasize ? _settings.FontSize + 1 : _settings.FontSize,
            emphasize ? FontWeights.Bold : FontWeights.SemiBold, _textBrush));
        return panel;
    }

    private Border BuildLogo(double size)
    {
        var border = new Border
        {
            Width = size + 6,
            Height = size + 6,
            BorderBrush = _borderBrush,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(2),
            SnapsToDevicePixels = true
        };

        var image = _images.Load(_company.LogoPath, 200);
        if (image is not null)
        {
            border.Child = new Image
            {
                Source = image,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        return border;
    }

    private BitmapSource? LoadTemplate()
    {
        if (!_settings.PrintTemplateBackground || string.IsNullOrWhiteSpace(_settings.TemplateImagePath)) return null;
        return _images.Load(_settings.TemplateImagePath);
    }

    /// <summary>Shrinks a font until the text fits into the given box (multi-line aware).</summary>
    private double FitFontSize(string text, double startSize, double maxWidth, double labelSize, int maxLines)
    {
        if (string.IsNullOrWhiteSpace(text) || maxWidth <= 0) return startSize;

        var size = startSize;
        var minSize = Math.Max(6d, startSize * 0.6);
        var budget = maxWidth * maxLines;

        while (size > minSize)
        {
            var width = MeasureWidth(text, size);
            if (width <= budget) break;
            size -= 0.5;
        }

        return size;
    }

    private double MeasureWidth(string text, double fontSize)
    {
        var formatted = new FormattedText(
            text,
            CultureInfo.GetCultureInfo("fa-IR"),
            FlowDirection.RightToLeft,
            new Typeface(_font, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
            fontSize,
            Brushes.Black,
            1.0);

        return Math.Max(formatted.Width, formatted.WidthIncludingTrailingWhitespace);
    }

    /// <summary>Display value helper: never returns null, optionally shows a placeholder dash.</summary>
    private string D(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? (_settings.ShowEmptyValuePlaceholder ? TextFormatter.EmptyPlaceholder : string.Empty)
            : value!;

    #endregion
}
