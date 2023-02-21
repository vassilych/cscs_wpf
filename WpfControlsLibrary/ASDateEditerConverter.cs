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
    public class ASDateEditerConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int size = (int)parameter;
            DateTime dateTime = new DateTime(1,1,1);
            try
            {
                dateTime = (DateTime)value;
                if (size == 8)
                    return dateTime.ToString("dd/MM/yy");
                else if (size == 10)
                    return dateTime.ToString("dd/MM/yyyy");
            }
            catch
            {
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string strValue = value as string;
            DateTime resultTimeSpan;

            int size = (int)parameter;
            if (size == 8)
            {
                if (DateTime.TryParseExact(strValue, "dd/MM/yy", CultureInfo.InvariantCulture, DateTimeStyles.None, out resultTimeSpan))
                {
                    return resultTimeSpan;
                }
            }
            else if (size == 10)
            {
                if (DateTime.TryParseExact(strValue, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out resultTimeSpan))
                {
                    return resultTimeSpan;
                }
            }
            return DependencyProperty.UnsetValue;
        }
    }
}