using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace SmartFileMan.Plugins.MovieCollection.Converters;

/// <summary>
/// 布尔值反转转换器
/// Bool inverse converter
/// </summary>
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
            return !boolValue;

        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
            return !boolValue;

        return false;
    }
}
