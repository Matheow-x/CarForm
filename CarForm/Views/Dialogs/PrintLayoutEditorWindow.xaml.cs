using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CarForm.Core.Mvvm;
using CarForm.Printing;
using CarForm.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace CarForm.Views.Dialogs;

public partial class PrintLayoutEditorWindow : Window
{
    private const double DisplayScale = 1.0;   // 1 template px = 1 DIP in the editor

    private readonly PrintLayoutEditorViewModel _vm;

    public PrintLayoutEditorWindow(PrintLayoutEditorViewModel viewModel)
    {
        _vm = viewModel;
        DataContext = viewModel;
        InitializeComponent();

        Loaded += (_, _) => BuildStage();
        KeyDown += OnKeyDown;
    }

    private void BuildStage()
    {
        BitmapSource? template = null;
        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri("pack://application:,,,/Resources/PrintTemplate.png", UriKind.Absolute);
            image.EndInit();
            image.Freeze();
            template = image;
        }
        catch
        {
            // no template image - the editor still works, just without a background
        }

        if (template is not null)
        {
            TemplateImage.Source = template;
            Stage.Width = template.PixelWidth * DisplayScale;
            Stage.Height = template.PixelHeight * DisplayScale;
            BoxLayer.Width = Stage.Width;
            BoxLayer.Height = Stage.Height;
        }
        else
        {
            Stage.Width = FormLayout.TemplateWidth;
            Stage.Height = FormLayout.TemplateHeight;
            BoxLayer.Width = Stage.Width;
            BoxLayer.Height = Stage.Height;
        }

        BoxLayer.Children.Clear();
        foreach (var item in _vm.Items) BoxLayer.Children.Add(CreateBox(item));
    }

    private UIElement CreateBox(FieldBoxItem item)
    {
        var border = new Border
        {
            BorderBrush = new SolidColorBrush(Color.FromArgb(200, 220, 20, 60)),
            BorderThickness = new Thickness(1.5),
            Background = new SolidColorBrush(Color.FromArgb(28, 220, 20, 60)),
            Cursor = Cursors.SizeAll,
            Tag = item,
            SnapsToDevicePixels = true
        };

        var grid = new Grid();
        var caption = new TextBlock
        {
            Text = item.Field,
            FontSize = 9,
            Foreground = new SolidColorBrush(Color.FromArgb(230, 150, 10, 35)),
            Margin = new Thickness(2, 0, 0, 0),
            IsHitTestVisible = false
        };
        grid.Children.Add(caption);

        // resize grip (bottom-right)
        var gripRight = new Thumb { Width = 10, Height = 10, Cursor = Cursors.SizeNWSE, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom };
        // resize grip (bottom-left) - useful for right aligned (RTL) values
        var gripLeft = new Thumb { Width = 10, Height = 10, Cursor = Cursors.SizeNESW, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Bottom };
        grid.Children.Add(gripRight);
        grid.Children.Add(gripLeft);

        border.Child = grid;
        Canvas.SetLeft(border, item.X * DisplayScale);
        Canvas.SetTop(border, item.Y * DisplayScale);
        border.Width = item.W * DisplayScale;
        border.Height = item.H * DisplayScale;

        border.MouseLeftButtonDown += (_, e) =>
        {
            _vm.Selected = item;
            border.CaptureMouse();
            e.Handled = true;
        };
        border.MouseMove += (_, e) =>
        {
            if (!border.IsMouseCaptured || e.LeftButton != MouseButtonState.Pressed) return;
            var pos = e.GetPosition(BoxLayer);
            var w = border.Width;
            var h = border.Height;
            item.X = Math.Round(Math.Max(0, Math.Min(pos.X - w / 2, BoxLayer.Width - w)) / DisplayScale, 1);
            item.Y = Math.Round(Math.Max(0, Math.Min(pos.Y - h / 2, BoxLayer.Height - h)) / DisplayScale, 1);
            Sync(border, item);
        };
        border.MouseLeftButtonUp += (_, _) => border.ReleaseMouseCapture();

        DragDeltaHandler(gripRight, (dx, dy) =>
        {
            item.W = Math.Round(Math.Max(6, item.W + dx / DisplayScale), 1);
            item.H = Math.Round(Math.Max(5, item.H + dy / DisplayScale), 1);
            Sync(border, item);
        });

        DragDeltaHandler(gripLeft, (dx, dy) =>
        {
            var newW = Math.Max(6, item.W - dx / DisplayScale);
            item.X = Math.Round(Math.Max(0, item.X + (item.W - newW)), 1);
            item.W = Math.Round(newW, 1);
            item.H = Math.Round(Math.Max(5, item.H + dy / DisplayScale), 1);
            Sync(border, item);
        });

        item.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(FieldBoxItem.X) or nameof(FieldBoxItem.Y)
                or nameof(FieldBoxItem.W) or nameof(FieldBoxItem.H))
            {
                Sync(border, item);
            }
        };

        return border;
    }

    private static void Sync(Border border, FieldBoxItem item)
    {
        Canvas.SetLeft(border, item.X * DisplayScale);
        Canvas.SetTop(border, item.Y * DisplayScale);
        border.Width = item.W * DisplayScale;
        border.Height = item.H * DisplayScale;
    }

    private static void DragDeltaHandler(Thumb thumb, Action<double, double> apply)
    {
        thumb.DragDelta += (_, e) => apply(e.HorizontalChange, e.VerticalChange);
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (_vm.Selected is not { } item) return;

        var step = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? 5 : 1;
        switch (e.Key)
        {
            case Key.Left: item.X -= step; break;
            case Key.Right: item.X += step; break;
            case Key.Up: item.Y -= step; break;
            case Key.Down: item.Y += step; break;
            default: return;
        }

        item.X = Math.Max(0, item.X);
        item.Y = Math.Max(0, item.Y);
        e.Handled = true;
    }

    private void CloseClick(object sender, RoutedEventArgs e) => Close();
}
