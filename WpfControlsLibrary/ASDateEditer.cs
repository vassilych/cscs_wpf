using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WpfControlsLibrary
{
    public class ASDateEditer : DatePicker
    {
        string textBeforeChange;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            //this.Padding = new Thickness(0, 0, 0, 0);
            //this.Resources.

            //this.DataContext = this;

            var root = this.Template.FindName("PART_Root", this) as Grid;
            if (root != null)
            {
            }

            var textBox = this.Template.FindName("PART_TextBox", this) as UIElement;
            if (textBox != null)
            {
                var dptb = textBox as DatePickerTextBox;

                dptb.FontWeight = FontWeight;
                dptb.Background = Background == null ? new SolidColorBrush() { Color = Colors.White } : Background;
                dptb.Foreground = Foreground == null ? new SolidColorBrush() { Color = Colors.Black } : Foreground;

                dptb.Margin = new Thickness(-2);
                dptb.Padding = new Thickness(0);

                dptb.IsReadOnly = IsReadOnly;

                dptb.Height = this.Height;
                dptb.Width = this.Width - this.ButtonWidth + 2;
                dptb.HorizontalAlignment = HorizontalAlignment.Left;

                dptb.PreviewTextInput += dptb_PreviewTextInput;

                dptb.PreviewKeyDown += dptb_PreviewKeyDown;
                dptb.SelectionChanged += dptb_SelectionChanged;

                dptb.LostFocus += Dptb_LostFocus;

                dptb.GotFocus += Dptb_GotFocus;
                dptb.PreviewMouseUp += Dptb_PreviewMouseUp;

                dptb.Loaded += Dptb_Loaded;

                //dptb.Focus();

                //Binding bind = new Binding("Date");
                //bind.Converter = new ASDateEditerConverter2();
                //bind.ConverterParameter = DisplaySize;
                ////bind.StringFormat = "dd..MM..yyyy";
                //bind.RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(ASDateEditer), 2);
                //bind.Mode = BindingMode.TwoWay;
                //bind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                //dptb.SetBinding(TextProperty, bind);
                //dptb.DataContext = this;

                //var alskdj2 = dptb.GetBindingExpression(TextProperty);
            }

            var button = this.Template.FindName("PART_Button", this) as Button;
            if (button != null)
            {
                button.Width = ButtonWidth;
                button.Margin = new Thickness(-2, 0, -4, 0);
                button.Padding = new Thickness(0);

                button.HorizontalContentAlignment = HorizontalAlignment.Center;
                button.VerticalContentAlignment = VerticalAlignment.Center;

                button.HorizontalAlignment = HorizontalAlignment.Right;
                button.VerticalAlignment = VerticalAlignment.Center;

                button.Background = Brushes.Red;

                Style MyButtonStyle = new Style();

                ControlTemplate templateButton = new ControlTemplate(typeof(Button));

                FrameworkElementFactory elemFactory = new FrameworkElementFactory(typeof(Image));
                elemFactory.SetValue(Image.SourceProperty, new BitmapImage(new Uri("settings.png", UriKind.Relative)));
                templateButton.VisualTree = elemFactory;

                button.Template = templateButton;
            }
        }

        //public static readonly DependencyProperty DateProperty = DependencyProperty.Register("Date", typeof(DateTime), typeof(ASDateEditer));
        //public DateTime Date
        //{
        //    get
        //    {
        //        return (DateTime)base.GetValue(DateProperty);
        //    }
        //    set
        //    {
        //        base.SetValue(DateProperty, value);
        //    }
        //}
        
        public static readonly DependencyProperty DisplaySizeProperty = DependencyProperty.Register("DisplaySize", typeof(int), typeof(ASDateEditer));
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
        
        public static readonly DependencyProperty FontWeightProperty = DependencyProperty.Register("FontWeight", typeof(FontWeight), typeof(ASDateEditer));
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
        
        public static readonly DependencyProperty ButtonWidthProperty = DependencyProperty.Register("ButtonWidth", typeof(int), typeof(ASDateEditer));
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

        public static readonly DependencyProperty BackgroundProperty = DependencyProperty.Register("Background", typeof(Brush), typeof(ASDateEditer));
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

        public static readonly DependencyProperty ForegroundProperty = DependencyProperty.Register("Foreground", typeof(Brush), typeof(ASDateEditer));
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
        
        public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(ASDateEditer));
        public bool IsReadOnly
        {
            get
            {
                return (bool)base.GetValue(IsReadOnlyProperty);
            }
            set
            {
                base.SetValue(IsReadOnlyProperty, value);
            }
        }

        private void Dptb_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            var dptb = (e.Source as DatePickerTextBox);

            if (firstClick)
            {
                firstClick = false;
                e.Handled = true;
                return;
            }
        }

        bool firstClick;

        private void Dptb_GotFocus(object sender, RoutedEventArgs e)
        {
            var dptb = (e.Source as DatePickerTextBox);

            dptb.SelectionChanged -= dptb_SelectionChanged;

            dptb.SelectAll();

            if (dptb.IsMouseOver)
            {
                firstClick = true;
            }

            dptb.SelectionChanged += dptb_SelectionChanged;
        }

        private void Dptb_Loaded(object sender, RoutedEventArgs e)
        {
            var dptb = (e.Source as DatePickerTextBox);

            dptb.SelectionChanged -= dptb_SelectionChanged;

            if (string.IsNullOrEmpty(dptb.Text))
            {
                if (DisplaySize == 8)
                    dptb.Text = "00/00/00";
                else if (DisplaySize == 10)
                    dptb.Text = "00/00/0000";

                dptb.SelectionStart = 0;
                dptb.SelectionLength = 1;
            }
            else
            {
                dptb.SelectAll();
            }

            dptb.SelectionChanged += dptb_SelectionChanged;
        }

        private void dptb_SelectionChanged(object sender, RoutedEventArgs e)
        {
            var dptb = (e.Source as DatePickerTextBox);

            dptb.SelectionChanged -= dptb_SelectionChanged;

            if(dptb.SelectionStart == 0 && dptb.SelectionLength == dptb.Text.Length)
            {
                dptb.SelectionChanged += dptb_SelectionChanged;
                return;
            }

            int[] allowedPositions = { 0, 1, 3, 4, 6, 7, 8, 9};
            if (!allowedPositions.Contains(dptb.SelectionStart))
            {
                    dptb.SelectionStart++;
            }
            
            if ((dptb.SelectionStart == 8 && DisplaySize == 8) || (dptb.SelectionStart == 10 && DisplaySize == 10))
                dptb.SelectionStart--;

            dptb.SelectionLength = 1;
            
            dptb.SelectionChanged += dptb_SelectionChanged;
        }
        private void dptb_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var dptb = (e.Source as DatePickerTextBox);

            if (e.Key == System.Windows.Input.Key.Delete || e.Key == System.Windows.Input.Key.Back)
            {

                if (dptb.SelectionLength == DisplaySize)
                {
                    var date111 = new DateTime(1, 1, 1);
                    dptb.Text = date111.ToString(GetPattern());
                }
                else
                {
                    var index = dptb.SelectionStart;
                    var textArray = dptb.Text.ToArray();
                    textArray[index] = '0';
                    dptb.Text = new string(textArray);
                    dptb.SelectionStart = index;
                }
                e.Handled = true;
            }
            else if (e.Key == Key.OemPlus || e.Key == Key.Add)
            {
                if(DateTime.TryParseExact(dptb.Text, GetPattern(), CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
                {
                    result = result.AddDays(1);
                    dptb.Text = result.ToString(GetPattern());
                }
            }
            else if (e.Key == Key.OemMinus || e.Key == Key.Subtract)
            {
                if (DateTime.TryParseExact(dptb.Text, GetPattern(), CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
                {
                    result = result.AddDays(-1);
                    dptb.Text = result.ToString(GetPattern());
                }
            }

            if (dptb.SelectionLength != 1)
                dptb.SelectionLength = 1;

            textBeforeChange = dptb.Text;

            if (e.Key == System.Windows.Input.Key.Left || e.Key == System.Windows.Input.Key.Up || e.Key == System.Windows.Input.Key.Back)
            {
                e.Handled = true;
                if (dptb.SelectionStart == 0)
                    return;
                if (dptb.SelectionStart == 3 || dptb.SelectionStart == 6)
                    dptb.SelectionStart = dptb.SelectionStart - 2;
                else
                    dptb.SelectionStart--;
            }
        }
        private void dptb_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!int.TryParse(e.Text, out int x))
            {
                e.Handled = true;
            }
        }

        private void Dptb_LostFocus(object sender, RoutedEventArgs e)
        {
            var dptb = (e.Source as DatePickerTextBox);

            var text = dptb.Text;

            if (!DateTime.TryParseExact(text, GetPattern(), CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime res))
            {
                var selStart = dptb.SelectionStart;
                dptb.Text = textBeforeChange;
                var newPosition = selStart - 1;
                dptb.SelectionStart = (newPosition < 0) ? 0 : newPosition;
            }
        }

        private string GetPattern()
        {
            if (DisplaySize == 8)
            {
                return "dd/MM/yy";
            }
            else if (DisplaySize == 10)
            {
                return "dd/MM/yyyy";
            }
            else return "";
        }

    }
}
