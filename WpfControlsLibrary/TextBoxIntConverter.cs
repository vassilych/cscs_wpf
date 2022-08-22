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
    public class TextBoxIntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int intValue = 0;
            try
            {
                intValue = (int)value;

            }
            catch
            {

            }
            return intValue.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string strValue = value as string;
            int resultInt;
            if (int.TryParse(strValue, out resultInt))
            {
                return resultInt;
            }
            return DependencyProperty.UnsetValue;
        }
    }
}