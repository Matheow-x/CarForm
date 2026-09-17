using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using CarForm.Core.Mvvm;
using CarForm.Printing;
using CarForm.Services;

namespace CarForm.ViewModels;

/// <summary>Editable wrapper around a <see cref="FieldBox"/> (values are kept in template pixels).</summary>
public sealed class FieldBoxItem : ObservableObject
{
    private double _x, _y, _w, _h;
    private bool _isSelected;

    public FieldBoxItem(FieldBox box)
    {
        Field = box.Field;
        _x = box.X * FormLayout.TemplateWidth;
        _y = box.Y * FormLayout.TemplateHeight;
        _w = box.W * FormLayout.TemplateWidth;
        _h = box.H * FormLayout.TemplateHeight;
        Align = box.Align;
        FontScale = box.FontScale;
        Wrap = box.Wrap;
    }

    public string Field { get; }

    public double X { get => _x; set => SetProperty(ref _x, value); }
    public double Y { get => _y; set => SetProperty(ref _y, value); }
    public double W { get => _w; set => SetProperty(ref _w, value); }
    public double H { get => _h; set => SetProperty(ref _h, value); }
    public string Align { get => _align; set => SetProperty(ref _align, value); }
    public double FontScale { get => _fontScale; set => SetProperty(ref _fontScale, value); }
    public bool Wrap { get => _wrap; set => SetProperty(ref _wrap, value); }

    private string _align = "Right";
    private double _fontScale = 1.0;
    private bool _wrap = true;

    public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }

    public FieldBox ToBox() => new()
    {
        Field = Field,
        X = X / FormLayout.TemplateWidth,
        Y = Y / FormLayout.TemplateHeight,
        W = W / FormLayout.TemplateWidth,
        H = H / FormLayout.TemplateHeight,
        Align = Align,
        FontScale = FontScale,
        Wrap = Wrap
    };
}

/// <summary>
/// Visual editor for the printed form: drag / resize every value box on top of the form image
/// and store the result in <c>form-layout.json</c>. No recompilation needed.
/// </summary>
public sealed class PrintLayoutEditorViewModel : ViewModelBase
{
    private readonly ISettingsService _settings;
    private readonly IDialogService _dialogs;
    private FieldBoxItem? _selected;

    public PrintLayoutEditorViewModel(ISettingsService settings, IDialogService dialogs)
    {
        _settings = settings;
        _dialogs = dialogs;

        Items = new ObservableCollection<FieldBoxItem>(FormLayout.Load().Select(b => new FieldBoxItem(b)));
        Selected = Items.FirstOrDefault();

        SaveCommand = new RelayCommand(Save);
        ResetCommand = new RelayCommand(Reset);
    }

    public ObservableCollection<FieldBoxItem> Items { get; }

    public string Title => $"ویرایشگر چیدمان چاپ ({Items.Count} جعبه)";

    public string FilePath => FormLayout.FilePath;

    public FieldBoxItem? Selected
    {
        get => _selected;
        set
        {
            if (_selected is not null) _selected.IsSelected = false;
            if (!SetProperty(ref _selected, value)) return;
            if (value is not null) value.IsSelected = true;
        }
    }

    public ICommand SaveCommand { get; }
    public ICommand ResetCommand { get; }

    private void Save()
    {
        FormLayout.Save(Items.Select(i => i.ToBox()).ToList());

        // apply immediately so the next preview uses the new layout
        _ = _settings.ReloadAsync();

        _dialogs.ShowInfo($"چیدمان در فایل زیر ذخیره شد:\n{FormLayout.FilePath}", "ذخیره چیدمان");
    }

    private void Reset()
    {
        if (!_dialogs.ShowConfirm("همهٔ جعبه‌ها به حالت پیش‌فرض بازمی‌گردند. ادامه می‌دهید؟", "بازنشانی چیدمان")) return;

        Items.Clear();
        foreach (var box in FormLayout.Reset()) Items.Add(new FieldBoxItem(box));
        Selected = Items.FirstOrDefault();
    }
}
