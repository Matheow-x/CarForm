using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CarForm.Core.Helpers;
using CarForm.Models;
using CarForm.Services;

namespace CarForm.Printing;

/// <summary>
/// Renders the document on top of the delivered form image ("template overlay" mode):
/// the image provides every label, line, box and placeholder (stamp / signature / police box),
/// the application only prints the VALUES inside pre-measured boxes.
/// <para>
/// Because it produces a <see cref="FixedDocument"/> like the vector renderer, print preview,
/// printing and PDF export stay pixel identical.
/// </para>
/// </summary>
public sealed class TemplateFormRenderer
{
    private readonly SalesDocument _document;
    private readonly Company _company;
    private readonly PrintSettings _settings;
    private readonly IImageStore _images;
    private readonly List<FieldBox> _layout;
    private readonly Func<string, string?> _resolve;

    private readonly FontFamily _font;
    private readonly SolidColorBrush _textBrush;
    private readonly SolidColorBrush _debugBrush;
    private readonly bool _persianDigits;

    public TemplateFormRenderer(SalesDocument document, Company company, PrintSettings settings,
        IImageStore images, List<FieldBox> layout, Func<string, string?> resolve)
    {
        _document = document;
        _company = company;
        _settings = settings;
        _images = images;
        _layout = layout;
        _resolve = resolve;

        _font = new FontFamily(settings.FontFamily);
        _textBrush = ColorHelper.Brush(settings.TextColor, Colors.Black);
        _debugBrush = new SolidColorBrush(Color.FromArgb(160, 220, 20, 60));
        _persianDigits = settings.UsePersianDigits;

        _textBrush.Freeze();
        _debugBrush.Freeze();
    }

    public Size PageSize => PrintLayout.A4;

    public FixedDocument Build()
    {
        var document = new FixedDocument();
        var pageContent = new PageContent();
        ((IAddChild)pageContent).AddChild(BuildPage());
        document.Pages.Add(pageContent);
        return document;
    }

    public FixedPage BuildPage()
    {
        var page = new FixedPage
        {
            Width = PageSize.Width,
            Height = PageSize.Height,
            Background = Brushes.White
        };

        var margin = PrintLayout.Mm(_settings.PageMarginMm);
        var available = new Size(PageSize.Width - 2 * margin, PageSize.Height - 2 * margin);

        var template = LoadTemplate();
        var imageRect = ComputeImageRect(template, available, margin);

        var canvas = new Canvas { Width = PageSize.Width, Height = PageSize.Height };

        if (template is not null)
        {
            var image = new Image
            {
                Source = template,
                Width = imageRect.Width,
                Height = imageRect.Height,
                Stretch = Stretch.Fill,
                SnapsToDevicePixels = true,
                UseLayoutRounding = true
            };
            Canvas.SetLeft(image, imageRect.X);
            Canvas.SetTop(image, imageRect.Y);
            canvas.Children.Add(image);
        }

        var scale = imageRect.Width / FormLayout.TemplateWidth;

        foreach (var box in _layout)
        {
            var value = _resolve(box.Field);
            DrawBox(canvas, box, value, imageRect, scale);
        }

        page.Children.Add(canvas);
        return page;
    }

    private void DrawBox(Canvas canvas, FieldBox box, string? value, Rect imageRect, double scale)
    {
        var (boxX, boxY, boxWidth, boxHeight) = box.ToRect(imageRect.Width, imageRect.Height);
        var rect = new Rect(boxX, boxY, boxWidth, boxHeight);
        rect.Offset(imageRect.X, imageRect.Y);

        if (_settings.ShowFieldFrames)
        {
            var frame = new System.Windows.Shapes.Rectangle
            {
                Width = rect.Width,
                Height = rect.Height,
                Stroke = _debugBrush,
                StrokeThickness = 0.5,
                StrokeDashArray = new DoubleCollection { 2, 2 },
                Fill = Brushes.Transparent,
                SnapsToDevicePixels = true
            };
            Canvas.SetLeft(frame, rect.X);
            Canvas.SetTop(frame, rect.Y);
            canvas.Children.Add(frame);

            var caption = new TextBlock
            {
                Text = box.Field,
                FontSize = 4.5,
                Foreground = _debugBrush,
                Opacity = 0.9
            };
            Canvas.SetLeft(caption, rect.X);
            Canvas.SetTop(caption, rect.Y - 5);
            canvas.Children.Add(caption);
        }

        if (string.IsNullOrWhiteSpace(value)) return;

        var fontSize = Math.Min(_settings.FontSize * scale * box.FontScale, rect.Height * 0.86);
        if (fontSize < 4) fontSize = 4;

        var block = new TextBlock
        {
            Text = value,
            FontFamily = _font,
            FontSize = fontSize,
            Foreground = _textBrush,
            FlowDirection = FlowDirection.RightToLeft,
            TextAlignment = box.Align.Equals("Center", StringComparison.OrdinalIgnoreCase)
                ? TextAlignment.Center
                : box.Align.Equals("Left", StringComparison.OrdinalIgnoreCase) ? TextAlignment.Left : TextAlignment.Right,
            TextWrapping = box.Wrap ? TextWrapping.Wrap : TextWrapping.NoWrap,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Width = rect.Width,
            MaxHeight = rect.Height * 2.2,
            LineHeight = fontSize * 1.25,
            LineStackingStrategy = LineStackingStrategy.BlockLineHeight,
            SnapsToDevicePixels = true
        };

        if (_settings.AutoShrinkText)
        {
            block.FontSize = Fit(value, fontSize, rect.Width, block.TextWrapping == TextWrapping.Wrap ? 3 : 1);
        }

        Canvas.SetLeft(block, rect.X);
        Canvas.SetTop(block, rect.Y + (rect.Height - block.FontSize * 1.25) / 4);
        canvas.Children.Add(block);
    }

    /// <summary>Shrinks the font until the text fits into the box.</summary>
    private double Fit(string text, double startSize, double maxWidth, int maxLines)
    {
        var size = startSize;
        var min = Math.Max(4d, startSize * 0.55);
        var budget = maxWidth * maxLines;

        while (size > min)
        {
            var formatted = new FormattedText(
                text,
                CultureInfo.GetCultureInfo("fa-IR"),
                FlowDirection.RightToLeft,
                new Typeface(_font, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
                size,
                Brushes.Black,
                1.0);

            if (formatted.Width <= budget) break;
            size -= 0.4;
        }

        return size;
    }

    private Rect ComputeImageRect(BitmapSource? template, Size available, double margin)
    {
        if (template is null || template.PixelWidth <= 0)
        {
            return new Rect(margin, margin, available.Width, available.Height);
        }

        if (string.Equals(_settings.TemplateStretch, "Fill", StringComparison.OrdinalIgnoreCase))
        {
            return new Rect(margin, margin, available.Width, available.Height);
        }

        var scale = Math.Min(available.Width / template.PixelWidth, available.Height / template.PixelHeight);
        var width = template.PixelWidth * scale;
        var height = template.PixelHeight * scale;

        var left = margin + (available.Width - width) / 2;
        var top = string.Equals(_settings.TemplateVerticalAlignment, "Bottom", StringComparison.OrdinalIgnoreCase)
            ? margin + (available.Height - height)
            : margin;

        return new Rect(left, top, width, height);
    }

    private BitmapSource? LoadTemplate()
    {
        if (!string.IsNullOrWhiteSpace(_settings.TemplateImagePath))
        {
            var custom = _images.Load(_settings.TemplateImagePath);
            if (custom is not null) return custom;
        }

        // Built-in template shipped with the application.
        try
        {
            var uri = new Uri("pack://application:,,,/Resources/PrintTemplate.png", UriKind.Absolute);
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = uri;
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch
        {
            return null;
        }
    }
}
