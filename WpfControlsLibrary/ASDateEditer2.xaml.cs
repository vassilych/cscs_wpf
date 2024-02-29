using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Net.Mime.MediaTypeNames;

namespace WpfControlsLibrary
{
    /// <summary>
    /// Interaction logic for ASDateEditer2.xaml
    /// </summary>
    public partial class ASDateEditer2 : UserControl
    {

        string textBeforeChange;

        public ASDateEditer2()
        {
            InitializeComponent();

            this.Loaded += ASDateEditer2_Loaded;

            DataObject.AddCopyingHandler(asdeTextBox, (sender, e) => { if (e.IsDragDrop) e.CancelCommand(); });
        }

        private void ASDateEditer2_Loaded(object sender, RoutedEventArgs e)
        {
            asdeTextBox.FontWeight = FontWeight;
            asdeTextBox.Background = Background == null ? new SolidColorBrush() { Color = Colors.White } : Background;
            asdeTextBox.Foreground = Foreground == null ? new SolidColorBrush() { Color = Colors.Black } : Foreground;

            asdeTextBox.Margin = new Thickness(-2);
            asdeTextBox.Padding = new Thickness(0);

            asdeTextBox.Height = this.Height;
            asdeTextBox.Width = this.Width - this.ButtonWidth + 2;
            //asdeTextBox.HorizontalAlignment = HorizontalAlignment.Left;



            asdeTextBox.PreviewTextInput += dptb_PreviewTextInput;

            asdeTextBox.PreviewKeyDown += dptb_PreviewKeyDown;
            asdeTextBox.SelectionChanged += dptb_SelectionChanged;

            asdeTextBox.LostFocus += Dptb_LostFocus;

            asdeTextBox.GotFocus += Dptb_GotFocus;
            asdeTextBox.PreviewMouseUp += Dptb_PreviewMouseUp;

            asdeTextBox.Loaded += Dptb_Loaded;

            asdeTextBox.TextChanged += AsdeTextBox_TextChanged; ;

            asdeTextBox.PreviewDragLeave += AsdeTextBox_PreviewDragLeave;



            //asdeTextBox.IsReadOnly = IsReadOnly;

            ////dptb.Focus();
            //Binding bind = new Binding("Text");
            //bind.Source = dptb;
            ////bind.Converter = new ASDateEditerConverter3();
            ////bind.ConverterParameter = DisplaySize;
            //bind.StringFormat = "dd/MM/yyyy";
            ////bind.StringFormat = "dd..MM..yyyy";
            ////bind.RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(ASDateEditer), 1);
            //bind.Mode = BindingMode.OneWayToSource;
            //bind.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            ////bind.TargetNullValue = null;
            ////dptb.SetBinding(TextProperty, bind);
            //this.SetBinding(DisplayDateProperty, bind);
            ////dptb.DataContext = this;
            ////var alskdj2 = dptb.GetBindingExpression(TextProperty);

            asdeButton.Width = ButtonWidth;
            asdeButton.Margin = new Thickness(-2, 0, -4, 0);
            asdeButton.Padding = new Thickness(0);

            asdeButton.HorizontalContentAlignment = HorizontalAlignment.Center;
            asdeButton.VerticalContentAlignment = VerticalAlignment.Center;

            //asdeButton.HorizontalAlignment = HorizontalAlignment.Right;
            asdeButton.VerticalAlignment = VerticalAlignment.Stretch;

            //asdeButton.Background = Brushes.Red;

            //Style MyButtonStyle = new Style();

            //ControlTemplate templateButton = new ControlTemplate(typeof(Button));

            //FrameworkElementFactory elemFactory = new FrameworkElementFactory(typeof(Image));
            //elemFactory.SetValue(Image.SourceProperty, new BitmapImage(new Uri("settings.png", UriKind.Relative)));
            //templateButton.VisualTree = elemFactory;

            //asdeButton.Template = templateButton;

            asdeButton.Click += AsdeButton_Click;

            //----------------------

            asdeDatePicker.SelectedDateChanged += AsdeDatePicker_SelectedDateChanged;
            asdeDatePicker.CalendarOpened += AsdeDatePicker_CalendarOpened;
        }

        private void AsdeTextBox_PreviewDragLeave(object sender, DragEventArgs e)
        {
            e.Handled = true;
        }

        private void AsdeDatePicker_CalendarOpened(object sender, RoutedEventArgs e)
        {
            skipSelectedDateChangedHandler = true;
            if (DateTime.TryParseExact(asdeTextBox.Text, GetPattern(), CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime res))
            {
                asdeDatePicker.SelectedDate = res;
            }
            skipSelectedDateChangedHandler = false;
        }

        private void AsdeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!DateTime.TryParseExact(asdeTextBox.Text, GetPattern(), CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime res))
            {
                skipSelectedDateChangedHandler = true;
                asdeDatePicker.SelectedDate = DateTime.Now;
            }
            else
            {
                skipSelectedDateChangedHandler = true;
                asdeDatePicker.SelectedDate = res;
            }
        }

        public bool skipSelectedDateChangedHandler;

        private void AsdeDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (skipSelectedDateChangedHandler)
            {
                return;
            }

            asdeTextBox.Text = asdeDatePicker.SelectedDate?.ToString(GetPattern());
            asdeTextBox.Focus();
        }

        private void AsdeButton_Click(object sender, RoutedEventArgs e)
        {
            asdeDatePicker.IsDropDownOpen = !asdeDatePicker.IsDropDownOpen;
        }


        //private void ASDateEditer_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    //var asd = e.Source;
        //}

        public static readonly DependencyProperty FieldNameProperty = DependencyProperty.Register("FieldName", typeof(string), typeof(ASDateEditer2));
        public string FieldName
        {
            get
            {
                return (string)base.GetValue(FieldNameProperty);
            }
            set
            {
                base.SetValue(FieldNameProperty, value);
            }
        }

        public static readonly DependencyProperty TempDateProperty = DependencyProperty.Register("TempDate", typeof(DateTime), typeof(ASDateEditer2));
        public DateTime TempDate
        {
            get
            {
                return (DateTime)base.GetValue(TempDateProperty);
            }
            set
            {
                base.SetValue(TempDateProperty, value);

                //asdeDatePicker.SelectedDateChanged -= AsdeDatePicker_SelectedDateChanged;
                asdeDatePicker.SelectedDate = value;
                //asdeDatePicker.SelectedDateChanged += AsdeDatePicker_SelectedDateChanged;
            }
        }

        public static readonly DependencyProperty DisplaySizeProperty = DependencyProperty.Register("DisplaySize", typeof(int), typeof(ASDateEditer2));
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

        public static readonly DependencyProperty FontWeightProperty = DependencyProperty.Register("FontWeight", typeof(FontWeight), typeof(ASDateEditer2));
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

        public static readonly DependencyProperty ButtonWidthProperty = DependencyProperty.Register("ButtonWidth", typeof(int), typeof(ASDateEditer2));
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

        public static readonly DependencyProperty BackgroundProperty = DependencyProperty.Register("Background", typeof(Brush), typeof(ASDateEditer2));
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

        public static readonly DependencyProperty ForegroundProperty = DependencyProperty.Register("Foreground", typeof(Brush), typeof(ASDateEditer2));
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

        public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(ASDateEditer2));
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
            //var dptb = (e.Source as TextBox);

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
            var dptb = (e.Source as TextBox);

            dptb.SelectionChanged -= dptb_SelectionChanged;

            textBeforeChange = dptb.Text;

            dptb.SelectAll();

            if (dptb.IsMouseOver)
            {
                firstClick = true;
            }

            dptb.SelectionChanged += dptb_SelectionChanged;
        }

        private void Dptb_Loaded(object sender, RoutedEventArgs e)
        {
            var dptb = (e.Source as TextBox);

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
            var dptb = (e.Source as TextBox);

            dptb.SelectionChanged -= dptb_SelectionChanged;

            if (dptb.SelectionStart == 0 && dptb.SelectionLength == dptb.Text.Length)
            {
                dptb.SelectionChanged += dptb_SelectionChanged;
                return;
            }

            int[] allowedPositions = { 0, 1, 3, 4, 6, 7, 8, 9 };
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
            var dptb = (e.Source as TextBox);

            if (e.Key == System.Windows.Input.Key.Delete || e.Key == System.Windows.Input.Key.Back)
            {

                if (dptb.SelectionLength == DisplaySize)
                {
                    var date111 = new DateTime(1, 1, 1);
                    //dptb.Text = date111.ToString(GetPattern());
                    dptb.Text = "00/00/00" + (DisplaySize == 10 ? "00" : "");
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
                if (DateTime.TryParseExact(dptb.Text, GetPattern(), CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
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
            var dptb = (e.Source as TextBox);

            var text = dptb.Text;

            if (!DateTime.TryParseExact(text, GetPattern(), CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime res))
            {
                if (dptb.Text == "00/00/00" || dptb.Text == "00/00/0000")
                    return;

                var selStart = dptb.SelectionStart;
                dptb.Text = textBeforeChange;
                var newPosition = selStart - 1;
                dptb.SelectionStart = (newPosition < 0) ? 0 : newPosition;
            }
            else
            {
                skipSelectedDateChangedHandler = true;
                asdeDatePicker.SelectedDate = res;
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
