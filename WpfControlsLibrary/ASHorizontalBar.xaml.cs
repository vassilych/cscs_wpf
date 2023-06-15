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
    /// Interaction logic for ASHorizontalBar.xaml
    /// </summary>
    public partial class ASHorizontalBar : UserControl
    {
        public static readonly DependencyProperty BarWidthProperty = DependencyProperty.Register("BarWidth", typeof(int), typeof(ASHorizontalBar));
        public int BarWidth
        {
            get
            {
                return (int)base.GetValue(BarWidthProperty);
            }
            set
            {
                base.SetValue(BarWidthProperty, value);

                if(value < 0)
                {
                    BarFill.HorizontalAlignment = HorizontalAlignment.Right;
                }
                else
                {
                    BarFill.HorizontalAlignment = HorizontalAlignment.Left;
                }
                BarFill.Width = Math.Abs(Width * value / 100);
            }
        }
        
        public static readonly DependencyProperty FontSizeProperty = DependencyProperty.Register("FontSize", typeof(int), typeof(ASHorizontalBar));
        public int FontSize
        {
            get
            {
                return (int)base.GetValue(FontSizeProperty);
            }
            set
            {
                base.SetValue(FontSizeProperty, value);
                InsideText.FontSize = value;
            }
        }
        
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(ASHorizontalBar));
        public string Text
        {
            get
            {
                return (string)base.GetValue(TextProperty);
            }
            set
            {
                base.SetValue(TextProperty, value);
                InsideText.Text = value;
            }
        }
        
        public static readonly DependencyProperty BarColorProperty = DependencyProperty.Register("BarColor", typeof(Brush), typeof(ASHorizontalBar));
        public Brush BarColor
        {
            get
            {
                return (Brush)base.GetValue(BarColorProperty);
            }
            set
            {
                base.SetValue(BarColorProperty, value);
                BarFill.Background = value;
            }
        }

        public ASHorizontalBar()
        {
            InitializeComponent();
        }

        bool loaded = false;

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (!loaded)
            {
                InsideText.IsReadOnly = true;
                InsideText.IsTabStop = false;
                InsideText.IsHitTestVisible = false;

                BarFill.Width = Math.Abs(Width * BarWidth / 100);
                InsideText.Text = Text;
                if(FontSize != 0)
                    InsideText.FontSize = FontSize;



                loaded = true;
            }
        }
    }
}
