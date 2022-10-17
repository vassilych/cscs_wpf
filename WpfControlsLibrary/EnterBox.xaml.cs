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
    /// Interaction logic for EnterBox.xaml
    /// </summary>
    /// 

    public partial class EnterBox : UserControl
    {
        public static readonly DependencyProperty SizeProperty = DependencyProperty.Register("Size", typeof(int), typeof(EnterBox));
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

        public static readonly DependencyProperty ButtonSizeProperty = DependencyProperty.Register("ButtonSize", typeof(int), typeof(EnterBox));
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

        public static readonly DependencyProperty FieldNameProperty = DependencyProperty.Register("FieldName", typeof(string), typeof(EnterBox));
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

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(EnterBox));
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

        public EnterBox()
        {
            InitializeComponent();
        }

        

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            enterBoxTextBox.Size = Size;
            enterBoxTextBox.Text = Text;

            enterBoxButton.Width = ButtonSize;
        }
    }
}
