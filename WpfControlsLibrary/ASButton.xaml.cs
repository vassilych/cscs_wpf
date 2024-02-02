using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfControlsLibrary
{
    public enum ImagePosition
    {
        Left, Top, Right, Bottom
    }

    /// <summary>
    /// Interaction logic for ASButton.xaml
    /// </summary>
    public partial class ASButton : UserControl
    {
        public static readonly DependencyProperty ImagePositionProperty = DependencyProperty.Register("ImagePosition", typeof(ImagePosition), typeof(ASButton));
        public ImagePosition ImagePosition
        {
            get
            {
                return (ImagePosition)base.GetValue(ImagePositionProperty);
            }
            set
            {
                base.SetValue(ImagePositionProperty, value);
            }
        }
        
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(ASButton));
        public string Text
        {
            get
            {
                return (string)base.GetValue(TextProperty);
            }
            set
            {
                base.SetValue(TextProperty, value);
            }
        }
        
        public static readonly DependencyProperty ImageProperty = DependencyProperty.Register("Image", typeof(string), typeof(ASButton));
        public string Image
        {
            get
            {
                return (string)base.GetValue(ImageProperty);
            }
            set
            {
                ASButtonImage1.Source = new BitmapImage(new Uri(value));
                ASButtonImage2.Source = new BitmapImage(new Uri(value));
                base.SetValue(ImageProperty, value);
            }
        }

        public static readonly DependencyProperty ImageSizeProperty = DependencyProperty.Register("ImageSize", typeof(int), typeof(ASButton));
        public int ImageSize
        {
            get
            {
                return (int)base.GetValue(ImageSizeProperty);
            }
            set
            {
                base.SetValue(ImageSizeProperty, value);
            }
        }

        public static readonly DependencyProperty ImageMarginProperty = DependencyProperty.Register("Thickness", typeof(Thickness), typeof(ASButton));
        public Thickness ImageMargin
        {
            get
            {
                return (Thickness)base.GetValue(ImageMarginProperty);
            }
            set
            {
                base.SetValue(ImageMarginProperty, value);
            }
        }

        public ASButton()
        {
            InitializeComponent();
        }

        bool loaded = false;

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (!loaded)
            {
                ASButtonTextBlock.Text = Text;

                if (!string.IsNullOrEmpty(Image))
                {
                    ASButtonImage1.Source = new BitmapImage(new Uri(Image));
                    ASButtonImage2.Source = new BitmapImage(new Uri(Image));
                }

                switch (ImagePosition)
                {
                    case ImagePosition.Left:
                    case ImagePosition.Top:
                        ASButtonImage2.Visibility = Visibility.Collapsed;
                        break;
                    case ImagePosition.Right:
                    case ImagePosition.Bottom:
                        ASButtonImage1.Visibility = Visibility.Collapsed;
                        break;
                    default:
                        break;
                }

                if(ImagePosition == ImagePosition.Top || ImagePosition == ImagePosition.Bottom)
                {
                    ASButtonStackPanel.Orientation = Orientation.Vertical;
                }

                ASButtonImage1.Width = ImageSize;
                ASButtonImage1.Height = ImageSize;
                ASButtonImage2.Height = ImageSize;
                ASButtonImage2.Height = ImageSize;

                ASButtonImage1.Margin = ImageMargin;
                ASButtonImage2.Margin = ImageMargin;

                //var temp = this.Name;
                //this.Name += "_x";
                //ASButtonButton.Name = temp;


                //enterBoxTextBox.CharacterCasing = Case?.ToLower() == "up" ? CharacterCasing.Upper : (Case?.ToLower() == "down" ? CharacterCasing.Lower : CharacterCasing.Normal);
                //enterBoxTextBox.FontWeight = FontWeight;

                //enterBoxTextBox.IsReadOnly = IsReadOnly;

                //enterBoxTextBox.Background = Background == null ? new SolidColorBrush() { Color = Colors.White } : Background;
                //enterBoxTextBox.Foreground = Foreground == null ? new SolidColorBrush() { Color = Colors.Black } : Foreground;

                //enterBoxButton.Width = ButtonSize;

                //enterBoxButton.Background = ButtonBackground == null ? new SolidColorBrush() { Color = Colors.LightGray } : ButtonBackground;

                loaded = true;
            }
        }
    }
}
