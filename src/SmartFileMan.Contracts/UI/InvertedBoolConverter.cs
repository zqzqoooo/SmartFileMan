using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace SmartFileMan.Contracts.UI
{
    public class InvertedBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b) return !b;
            if (value is int i) return i == 0; // Treat 0 count as "true" (Show Empty View)
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b) return !b;
            return false;
        }
    }
}
