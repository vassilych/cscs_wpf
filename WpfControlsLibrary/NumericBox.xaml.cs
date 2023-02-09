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
    /// Interaction logic for NumericBox.xaml
    /// </summary>
    public partial class NumericBox : UserControl
    {

        public static readonly DependencyProperty SizeProperty = DependencyProperty.Register("Size", typeof(int), typeof(NumericBox));
        public int Size
        {
            get
            {
                return (int)base.GetValue(SizeProperty);
            }
            set
            {
                base.SetValue(SizeProperty, value);
            }
        }

        public static readonly DependencyProperty DecProperty = DependencyProperty.Register("Dec", typeof(int), typeof(NumericBox));
        public int Dec
        {
            get
            {
                return (int)base.GetValue(DecProperty);
            }
            set
            {
                base.SetValue(DecProperty, value);
            }
        }
        
        public static readonly DependencyProperty MinValueProperty = DependencyProperty.Register("MinValue", typeof(double?), typeof(NumericBox));
        public double? MinValue
        {
            get
            {
                return (double?)base.GetValue(MinValueProperty);
            }
            set
            {
                base.SetValue(MinValueProperty, value);
            }
        }

        public static readonly DependencyProperty MaxValueProperty = DependencyProperty.Register("MaxValue", typeof(double?), typeof(NumericBox));
        public double? MaxValue
        {
            get
            {
                return (double?)base.GetValue(MaxValueProperty);
            }
            set
            {
                base.SetValue(MaxValueProperty, value);
            }
        }

        public static readonly DependencyProperty ButtonSizeProperty = DependencyProperty.Register("ButtonSize", typeof(int), typeof(NumericBox));
        public int ButtonSize
        {
            get
            {
                return (int)base.GetValue(ButtonSizeProperty);
            }
            set
            {
                base.SetValue(ButtonSizeProperty, value);
            }
        }
        
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(NumericBox));
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

        public static readonly DependencyProperty ThousandsProperty = DependencyProperty.Register("Thousands", typeof(bool), typeof(NumericBox));
        public bool Thousands
        {
            get
            {
                return (bool)base.GetValue(ThousandsProperty);
            }
            set
            {
                base.SetValue(ThousandsProperty, value);
            }
        }

        //public static readonly DependencyProperty TextBoxDCProperty = DependencyProperty.Register("TextBoxDC", typeof(string), typeof(NumericBox));
        //public string TextBoxDC
        //{
        //    get
        //    {
        //        return (string)base.GetValue(TextBoxDCProperty);
        //    }
        //    set
        //    {
        //        base.SetValue(TextBoxDCProperty, value);
        //    }
        //}
        
        //public static readonly DependencyProperty TextBoxNameProperty = DependencyProperty.Register("TextBoxName", typeof(string), typeof(NumericBox));
        //public string TextBoxName
        //{
        //    get
        //    {
        //        return (string)base.GetValue(TextBoxNameProperty);
        //    }
        //    set
        //    {
        //        base.SetValue(TextBoxNameProperty, value);
        //    }
        //}
        
        public static readonly DependencyProperty FieldNameProperty = DependencyProperty.Register("FieldName", typeof(string), typeof(NumericBox));
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

        public static readonly DependencyProperty KeyTrapsProperty = DependencyProperty.Register("KeyTraps", typeof(string), typeof(NumericBox));
        public string KeyTraps
        {
            get
            {
                return (string)base.GetValue(KeyTrapsProperty);
            }
            set
            {
                base.SetValue(KeyTrapsProperty, value);
            }
        }

        //public static readonly DependencyProperty ButtonNameProperty = DependencyProperty.Register("ButtonName", typeof(string), typeof(NumericBox));
        //public string ButtonName
        //{
        //    get
        //    {
        //        return (string)base.GetValue(ButtonNameProperty);
        //    }
        //    set
        //    {
        //        base.SetValue(ButtonNameProperty, value);
        //    }
        //}

        public NumericBox()
        {
            InitializeComponent();
        }


        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            numBoxTextBox.Size = Size;
            numBoxTextBox.Dec = Dec;
            numBoxTextBox.MinValue = MinValue != null ? (double)MinValue : double.MinValue;
            numBoxTextBox.MaxValue = MaxValue != null ? (double)MaxValue : double.MaxValue;
            numBoxTextBox.Text = Text;
            numBoxTextBox.Thousands = Thousands;
            numBoxButton.Width = ButtonSize;

            //numBoxTextBox.DataContext = TextBoxDC;

            //numBoxTextBox.Name = TextBoxName;
            //numBoxButton.Name = ButtonName;
        }

        public void FormatNumericTextBox()
        {
            numBoxTextBox.FormatOnLostFocus();
        }
    }
}
