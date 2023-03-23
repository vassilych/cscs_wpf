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
    public class ASNumericBoxConverter : IValueConverter
    {
        int? Dec;
        string FormatType;
        public ASNumericBoxConverter(string formatType, int? dec)
        {
            Dec = dec;
            FormatType = formatType;
        }
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
            string returnStringValue = doubleValue.ToString(FormatType + Dec);//.Replace(" ", "");
            return returnStringValue;
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
    
    public class ASNumericBoxEditConverter : IValueConverter
    {
        int? Dec;
        public ASNumericBoxEditConverter(int? dec)
        {
            Dec = dec;
        }
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
            string returnStringValue = doubleValue.ToString("F" + Dec).Replace(" ", "");
            return returnStringValue;
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