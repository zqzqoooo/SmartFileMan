using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

// 命名空间必须是 SmartFileMan.App，这样 AppShell.xaml 里的 local: 才能找到它
namespace SmartFileMan.App
{
    public class BoolToColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // 如果 IsEnabled 为 true，返回绿色，否则返回灰色
            if (value is bool isEnabled && isEnabled)
                return Colors.Green;

            return Colors.Gray;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}