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
    public class TimeEditerConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int size = (int)parameter;
            TimeSpan timeSpan = new TimeSpan(0, 0, 0);
            try
            {
                timeSpan = (TimeSpan)value;
                if (size == 5)
                    return timeSpan.ToString("hh\\:mm");
                else if (size == 8)
                    return timeSpan.ToString("hh\\:mm\\:ss");
            }
            catch
            {
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string strValue = value as string;
            TimeSpan resultTimeSpan;
            int size = (int)parameter;
            if (size == 5)
            {
                if (TimeSpan.TryParseExact(strValue, "hh\\:mm", CultureInfo.InvariantCulture, out resultTimeSpan))
                {
                    return resultTimeSpan;
                }
            }
            else if (size == 8)
            {
                if (TimeSpan.TryParseExact(strValue, "hh\\:mm\\:ss", CultureInfo.InvariantCulture, out resultTimeSpan))
                {
                    return resultTimeSpan;
                }
            }
            return DependencyProperty.UnsetValue;
        }
    }
}