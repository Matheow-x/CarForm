using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using CarForm.Models;

namespace CarForm.Views;

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var flag = value is bool b && b;
        if (string.Equals(parameter as string, "inverse", StringComparison.OrdinalIgnoreCase)) flag = !flag;
        return flag ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is Visibility visibility && visibility == Visibility.Visible;
}

public class InverseBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is not true;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => value is not true;
}

/// <summary>Collapses when the string is null or empty. Use parameter "inverse" for the opposite.</summary>
public class EmptyToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isEmpty = string.IsNullOrWhiteSpace(value as string);
        if (string.Equals(parameter as string, "inverse", StringComparison.OrdinalIgnoreCase)) isEmpty = !isEmpty;
        return isEmpty ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => value!;
}

public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isNull = value is null;
        if (string.Equals(parameter as string, "inverse", StringComparison.OrdinalIgnoreCase)) isNull = !isNull;
        return isNull ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => value!;
}

/// <summary>Visible when the bound int equals the converter parameter (used by the wizard steps).</summary>
public class IntEqualsToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var expected = parameter?.ToString();
        return value?.ToString() == expected ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => value!;
}

public class IntEqualsToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value?.ToString() == parameter?.ToString();

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => value!;
}

/// <summary>Persian display names for enums (used in combo boxes and grids).</summary>
public class EnumDisplayConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null) return "همه";

        return value switch
        {
            OwnerType.Individual => "شخص حقیقی",
            OwnerType.LegalEntity => "شخص حقوقی",
            ThemeMode.Light => "روشن (روز)",
            ThemeMode.Dark => "تیره (شب)",
            AuditAction.Create => "ایجاد",
            AuditAction.Update => "ویرایش",
            AuditAction.Delete => "حذف",
            AuditEntityType.Owner => "مالک / طرف حساب",
            AuditEntityType.Vehicle => "خودرو",
            AuditEntityType.SalesDocument => "سند فروش",
            AuditEntityType.Company => "شرکت",
            AuditEntityType.Settings => "تنظیمات",
            AuditEntityType.Database => "پشتیبان / پایگاه داده",
            _ => value.ToString() ?? string.Empty
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => value!;
}

public class FileSizeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not long bytes) return string.Empty;
        return bytes > 1024 * 1024
            ? $"{bytes / 1024.0 / 1024.0:0.00} مگابایت"
            : $"{bytes / 1024.0:0.0} کیلوبایت";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => value!;
}

/// <summary>Converts a nullable bool to a tri-state checkbox value (unset = null).</summary>
public class NullableBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value ?? false;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value as bool? ?? false;
}

/// <summary>True when the bound <see cref="OwnerType"/> matches the expected value (radio buttons).</summary>
public sealed class OwnerTypeToBoolConverter : IValueConverter
{
    private readonly OwnerType _expected;

    public OwnerTypeToBoolConverter(OwnerType expected) => _expected = expected;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is OwnerType type && type == _expected;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? _expected : Binding.DoNothing;
}

public static class BuyerTypeConverters
{
    public static readonly IValueConverter IsIndividual = new OwnerTypeToBoolConverter(OwnerType.Individual);

    public static readonly IValueConverter IsLegal = new OwnerTypeToBoolConverter(OwnerType.LegalEntity);
}
