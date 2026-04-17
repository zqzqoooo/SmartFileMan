using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace SmartFileMan.Plugins.MovieCollection.Converters;

/// <summary>
/// 空值转布尔值转换器
/// Null to bool converter
/// </summary>
public class NullToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null)
            return false;

        if (value is string str)
            return !string.IsNullOrWhiteSpace(str);

        return true;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
