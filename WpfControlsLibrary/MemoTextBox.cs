using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WpfControlsLibrary
{
    public class MemoTextBox : TextBox
    {
        public static readonly DependencyProperty SizeProperty = DependencyProperty.Register("Size", typeof(int), typeof(MemoTextBox));
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

        //public int Size;
        string textBeforeChange;

        public Dictionary<string, List<object>> paramsForKeyTraps = new Dictionary<string, List<object>>();


        public override void OnApplyTemplate()
        {
            //Size = 600;
            if(Size == 0)
            {
                Size = Int32.MaxValue;
            }

            TextWrapping = TextWrapping.Wrap;
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto;

            base.OnApplyTemplate();

            this.PreviewTextInput += this_PreviewTextInput;

            this.PreviewKeyDown += this_PreviewKeyDown;

            //this.AddHandler(CommandManager.ExecutedEvent, new RoutedEventHandler(CommandExecuted), true);

            //this.GotFocus += NumericTextBox_GotFocus;
            //this.LostFocus += NumericTextBox_LostFocus;

            //this.Loaded += NumericTextBox_Loaded;

            this.TextChanged += this_TextChanged;
        }

        //private void CommandExecuted(object sender, RoutedEventArgs e)
        //{
        //    if ((e as ExecutedRoutedEventArgs).Command == ApplicationCommands.Paste)
        //    {
        //        // verify that the textbox handled the paste command
        //        if (e.Handled)
        //        {
                    
        //        }
        //    }
        //}

        private void this_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var etb = e.Source as MemoTextBox;

            textBeforeChange = etb.Text;
        }

        private void this_TextChanged(object sender, TextChangedEventArgs e)
        {
            var etb = e.Source as MemoTextBox;

            etb.TextChanged -= this_TextChanged;

            if (etb.Text.Length > Size)
            {
                var selStart = etb.SelectionStart;
                etb.Text = etb.Text.Substring(0, Size);
                if(selStart > Size)
                {
                    selStart = Size;
                }
                etb.SelectionStart = selStart;
            }

            etb.TextChanged += this_TextChanged;
        }

        private void this_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var etb = e.Source as MemoTextBox;

            if (etb.Text.Length + e.Text.Length - etb.SelectionLength > Size)
            {
                e.Handled = true;
            }
        }
    }
}
