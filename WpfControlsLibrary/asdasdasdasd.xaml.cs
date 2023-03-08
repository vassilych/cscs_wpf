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
    /// <summary>
    /// Interaction logic for asdasdasdasd.xaml
    /// </summary>
    public partial class asdasdasdasd : UserControl
    {
        public static readonly DependencyProperty DisplaySizeProperty = DependencyProperty.Register("DisplaySize", typeof(int), typeof(asdasdasdasd));
        public int DisplaySize
        {
            get
            {
                return (int)base.GetValue(DisplaySizeProperty);
            }
            set
            {
                base.SetValue(DisplaySizeProperty, value);
            }
        }

        public static readonly DependencyProperty FontWeightProperty = DependencyProperty.Register("FontWeight", typeof(FontWeight), typeof(asdasdasdasd));
        public FontWeight FontWeight
        {
            get
            {
                return (FontWeight)base.GetValue(FontWeightProperty);
            }
            set
            {
                base.SetValue(FontWeightProperty, value);
            }
        }

        public static readonly DependencyProperty ButtonWidthProperty = DependencyProperty.Register("ButtonWidth", typeof(int), typeof(asdasdasdasd));
        public int ButtonWidth
        {
            get
            {
                return (int)base.GetValue(ButtonWidthProperty);
            }
            set
            {
                base.SetValue(ButtonWidthProperty, value);
            }
        }

        public static readonly DependencyProperty BackgroundProperty = DependencyProperty.Register("Background", typeof(Brush), typeof(asdasdasdasd));
        public Brush Background
        {
            get
            {
                return (Brush)base.GetValue(BackgroundProperty);
            }
            set
            {
                base.SetValue(BackgroundProperty, value);
            }
        }

        public static readonly DependencyProperty ForegroundProperty = DependencyProperty.Register("Foreground", typeof(Brush), typeof(asdasdasdasd));
        public Brush Foreground
        {
            get
            {
                return (Brush)base.GetValue(ForegroundProperty);
            }
            set
            {
                base.SetValue(ForegroundProperty, value);
            }
        }

        public asdasdasdasd()
        {
            InitializeComponent();

            


        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            dateEditer.DisplaySize = DisplaySize;
            dateEditer.FontWeight = FontWeight;
            dateEditer.ButtonWidth = ButtonWidth;
            dateEditer.Background = Background;
            dateEditer.Foreground = Foreground;

            dateEditer.Width = Width;
            dateEditer.Height = Height;

            dateEditer.HorizontalAlignment = HorizontalAlignment;
            dateEditer.VerticalAlignment = VerticalAlignment;

            dateEditer.Margin = Margin;

            dateEditer.Name = Name;
            dateEditer.DataContext = DataContext;
        }
    }
}
