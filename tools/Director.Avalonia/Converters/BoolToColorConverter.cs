using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Director.Avalonia.Converters
{
    public class BoolToColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                // Use WCAG compliant colors
                return boolValue ? new SolidColorBrush(Color.FromRgb(26, 127, 55)) : new SolidColorBrush(Color.FromRgb(209, 36, 47));
            }
            return new SolidColorBrush(Color.FromRgb(139, 148, 158));
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
