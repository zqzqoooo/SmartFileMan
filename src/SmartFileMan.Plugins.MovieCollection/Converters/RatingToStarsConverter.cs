using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace SmartFileMan.Plugins.MovieCollection.Converters;

/// <summary>
/// 评分转星星转换器
/// Rating to stars converter
/// </summary>
public class RatingToStarsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double rating)
        {
            int fullStars = (int)(rating / 2);
            return new string('⭐', fullStars);
        }

        return string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
