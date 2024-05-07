using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;
using System.Windows.Media.Imaging;

namespace WpfControlsLibrary
{
    public class ASGridCellImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                BitmapImage image = new BitmapImage(new Uri(value as string, UriKind.Absolute));
                return image;
            }
            catch
            {
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //string strValue = value as string;
            //int resultInt;
            //if (int.TryParse(strValue, out resultInt))
            //{
            //    return resultInt;
            //}
            return DependencyProperty.UnsetValue;
        }
    }
}
