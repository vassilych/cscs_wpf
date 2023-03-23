using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace WpfControlsLibrary
{
    public class ASTextBoxDoubleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double doubleValue = 0;
            try
            {
                doubleValue = (double)value;
            }
            catch
            {

            }
            return doubleValue.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string strValue = value as string;
            double resultDouble;
            if (double.TryParse(strValue, out resultDouble))
            {
                return resultDouble;
            }
            return DependencyProperty.UnsetValue;
        }
    }
}