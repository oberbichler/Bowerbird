using System;
using System.Windows;
using System.Windows.Data;

namespace Bowerbird.Dialogs.Converters
{
    public class ParamToIntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value.Equals(int.Parse((string) parameter));
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var parameterString = parameter as string;

            if (parameterString == null)
                return DependencyProperty.UnsetValue;

            return int.Parse(parameterString);
        }
    }
}