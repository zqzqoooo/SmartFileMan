using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace SmartFileMan.Plugins.MovieCollection.Converters;

/// <summary>
/// 日期格式转换器
/// Date format converter
/// </summary>
public class DateFormatConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DateTime date)
        {
            var format = parameter as string ?? "yyyy-MM-dd";
            return date.ToString(format);
        }

        return string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string str && DateTime.TryParse(str, out var date))
        {
            return date;
        }

        return DateTime.MinValue;
    }
}
