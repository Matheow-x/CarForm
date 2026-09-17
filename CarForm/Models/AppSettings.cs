using System.Collections.Generic;
using System.Linq;
using CarForm.Core.Mvvm;

namespace CarForm.Models;

/// <summary>Application wide settings (persisted as JSON in the Settings table).</summary>
public class AppSettings : ObservableObject
{
    private ThemeMode _theme = ThemeMode.Light;
    private bool _autoNumbering = true;
    private string _numberFormat = "{yyyy}/{seq:D4}";
    private int _lastNumber;
    private bool _usePersianDigitsInUi = true;
    private string _lastBackupFolder = string.Empty;
    private List<string> _defaultInvoiceItems = new();

    public ThemeMode Theme { get => _theme; set => SetProperty(ref _theme, value); }

    public bool AutoNumbering { get => _autoNumbering; set => SetProperty(ref _autoNumbering, value); }

    /// <summary>
    /// Document number template. Supported tokens: {seq}, {seq:D4}, {yyyy}, {yy}, {mm}, {dd} (Jalali).
    /// </summary>
    public string NumberFormat { get => _numberFormat; set => SetProperty(ref _numberFormat, value); }

    public int LastNumber { get => _lastNumber; set => SetProperty(ref _lastNumber, value); }

    public bool UsePersianDigitsInUi { get => _usePersianDigitsInUi; set => SetProperty(ref _usePersianDigitsInUi, value); }

    public string LastBackupFolder { get => _lastBackupFolder; set => SetProperty(ref _lastBackupFolder, value); }

    /// <summary>Titles offered by default on every new invoice (editable in App/Document settings).</summary>
    public List<string> DefaultInvoiceItems
    {
        get => _defaultInvoiceItems;
        set => SetProperty(ref _defaultInvoiceItems, value);
    }

    public static List<string> CreateDefaultInvoiceItems() => new()
    {
        "حق‌الثبت و نقل و انتقال",
        "عوارض شهرداری",
        "مالیات بر ارزش افزوده",
        "بیمه شخص ثالث",
        "معاینه فنی",
        "شماره‌گذاری و پلاک",
        "کارت هوشمند خودرو",
        "حق‌الزحمه و خدمات"
    };
}
