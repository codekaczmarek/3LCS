using System;
using System.Globalization;
using System.Windows.Data;

namespace ThreeLCS.Views.Converters
{
    public class CaptionHeightConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2
                || values[0] is not double height
                || values[1] is not double borderTop)
                return 48d;

            return height + borderTop;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null!;
        }
    }
}
