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

        public static readonly DependencyProperty CaseProperty = DependencyProperty.Register("Case", typeof(string), typeof(EnterBox));
        public string Case
        {
            get
            {
                return (string)base.GetValue(CaseProperty);
            }
            set
            {
                base.SetValue(CaseProperty, value);
            }
        }
        
        public static readonly DependencyProperty KeyTrapsProperty = DependencyProperty.Register("KeyTraps", typeof(string), typeof(EnterBox));
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

        public EnterBox()
        {
            InitializeComponent();
        }

        

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            enterBoxTextBox.Size = Size == 0? Int32.MaxValue : Size;
            enterBoxTextBox.Text = Text;
            enterBoxTextBox.CharacterCasing = Case.ToLower() == "up"? CharacterCasing.Upper : (Case.ToLower() == "down"? CharacterCasing.Lower : CharacterCasing.Normal);

            enterBoxButton.Width = ButtonSize;
        }
    }
}
