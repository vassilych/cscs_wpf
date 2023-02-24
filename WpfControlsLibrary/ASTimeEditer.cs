using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WpfControlsLibrary
{
    public class ASTimeEditer : TextBox
    {
        string textBeforeChange;
        public bool valueChanged;

        public static readonly DependencyProperty DisplaySizeProperty = DependencyProperty.Register("DisplaySize", typeof(int), typeof(ASTimeEditer));
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

        public static new readonly DependencyProperty FontWeightProperty = DependencyProperty.Register("FontWeight", typeof(FontWeight), typeof(ASTimeEditer));
        public new FontWeight FontWeight
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

        public static readonly DependencyProperty BackgroundProperty = DependencyProperty.Register("Background", typeof(Brush), typeof(ASTimeEditer));
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

        public static readonly DependencyProperty ForegroundProperty = DependencyProperty.Register("Foreground", typeof(Brush), typeof(ASTimeEditer));
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

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            base.FontWeight = FontWeight;
            base.Background = Background == null ? new SolidColorBrush() { Color = Colors.White } : Background;
            base.Foreground = Foreground == null ? new SolidColorBrush() { Color = Colors.Black } : Foreground;
        }

        public ASTimeEditer()
        {
            Loaded += TimeEditer_Loaded;
            PreviewTextInput += TimeEditer_PreviewTextInput;
            TextChanged += TimeEditer_TextChanged;

            PreviewKeyDown += TimeEditer_PreviewKeyDown;
            SelectionChanged += TimeEditer_SelectionChanged;

            GotKeyboardFocus += TimeEditer_GotKeyboardFocus;

            MouseDoubleClick += TimeEditer_MouseDoubleClick;
        }

        private void TimeEditer_Loaded(object sender, RoutedEventArgs e)
        {
            var te = (e.Source as ASTimeEditer);

            te.SelectionChanged -= TimeEditer_SelectionChanged;
            te.TextChanged -= TimeEditer_TextChanged;

            if (string.IsNullOrEmpty(te.Text))
            {
                if (DisplaySize == 5)
                    te.Text = "00:00";
                else if (DisplaySize == 8)
                    te.Text = "00:00:00";
            }
            

            

            te.SelectionChanged += TimeEditer_SelectionChanged;
            te.TextChanged += TimeEditer_TextChanged;
        }

        private void TimeEditer_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var te = (e.Source as ASTimeEditer);
            te.SelectionChanged -= TimeEditer_SelectionChanged;
            te.SelectAll();
            te.SelectionChanged += TimeEditer_SelectionChanged;
        }

        private void TimeEditer_GotKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
        {
            loaded = false;
            var te = (e.Source as ASTimeEditer);
            te.SelectionChanged -= TimeEditer_SelectionChanged;
            te.SelectAll();
            te.SelectionChanged += TimeEditer_SelectionChanged;
        }
        bool loaded = false;
        private void TimeEditer_SelectionChanged(object sender, RoutedEventArgs e)
        {
            var te = (e.Source as ASTimeEditer);

            try
            {
                te.SelectionChanged -= TimeEditer_SelectionChanged;

                if (loaded == false)
                {
                    te.SelectionStart = 0;
                    te.SelectionLength = DisplaySize;
                    loaded = true;
                    return;
                }

                if (te.SelectionLength == DisplaySize)
                    return;

                int[] allowedPositions = { 0, 1, 3, 4, 6, 7 };
                if (!allowedPositions.Contains(te.SelectionStart))
                {
                    if ((te.SelectionStart == 8 && DisplaySize == 8) || (te.SelectionStart == 5 && DisplaySize == 5))
                        te.SelectionStart--;
                    else
                        te.SelectionStart++;
                }

                te.SelectionLength = 1;
            }
            catch (Exception)
            {

            }
            finally
            {
                te.SelectionChanged += TimeEditer_SelectionChanged;
            }            
        }

        private void TimeEditer_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            var te = (e.Source as ASTimeEditer);

            if (e.Key == System.Windows.Input.Key.Delete || e.Key == System.Windows.Input.Key.Back)
            {

                if (te.SelectionLength == DisplaySize)
                {
                    var date111 = new TimeSpan(0,0,0);
                    te.Text = date111.ToString(GetFormat());
                }
                else
                {
                    var index = te.SelectionStart;
                    var textArray = te.Text.ToArray();
                    textArray[index] = '0';
                    te.Text = new string(textArray);
                    te.SelectionStart = index;
                }
                e.Handled = true;
            }

            if (te.SelectionLength != 1)
                te.SelectionLength = 1;

            textBeforeChange = te.Text;

            if (e.Key == System.Windows.Input.Key.Left || e.Key == System.Windows.Input.Key.Up || e.Key == System.Windows.Input.Key.Back)
            {
                e.Handled = true;
                if (te.SelectionStart == 0)
                    return;
                if (te.SelectionStart == 3 || te.SelectionStart == 6)
                    te.SelectionStart = te.SelectionStart - 2;
                else
                    te.SelectionStart--;
            }
        }

        
        private void TimeEditer_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            if (!int.TryParse(e.Text, out int x))
            {
                e.Handled = true;
            }
        }

        private void TimeEditer_TextChanged(object sender, TextChangedEventArgs e)
        {
            var te = (e.Source as ASTimeEditer);

            var text = te.Text;
            
            Regex rg = new Regex(GetPattern());
            if (!rg.IsMatch(text))
            {
                TextChanged -= TimeEditer_TextChanged;
                var selStart = te.SelectionStart;
                te.Text = textBeforeChange;
                te.SelectionStart = selStart - 1 >= 0 ? selStart - 1 : 0;
                TextChanged += TimeEditer_TextChanged;
            }

            textBeforeChange = text;
        }

        private string GetPattern()
        {
            if (DisplaySize == 8)
            {
                return @"^(?:2[0-3]|[01][0-9]):[0-5][0-9]:[0-5][0-9]$";
            }
            else if (DisplaySize == 5)
            {
                return @"^(?:2[0-3]|[01][0-9]):[0-5][0-9]$";
            }
            else return "";
        }
        private string GetFormat()
        {
            if (DisplaySize == 8)
            {
                return "hh\\:mm\\:ss";
            }
            else if (DisplaySize == 5)
            {
                return "hh\\:mm";
            }
            else return "";
        }

    }
}
