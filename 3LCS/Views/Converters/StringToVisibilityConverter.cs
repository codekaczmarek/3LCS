using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ThreeLCS.Views.Converters
{
    /// <summary>Converts a string to Visibility: non-empty → Visible, null/empty → Collapsed.</summary>
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            string.IsNullOrEmpty(value as string) ? Visibility.Collapsed : Visibility.Visible;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
