using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace WpfControlsLibrary
{
    public class EnterTextBox : TextBox
    {
        public int Size;

        public override void OnApplyTemplate()
        {
            Size = 600;

            base.OnApplyTemplate();

            this.PreviewTextInput += this_PreviewTextInput;

            //this.PreviewKeyDown += this_PreviewKeyDown;


            //this.GotFocus += NumericTextBox_GotFocus;
            //this.LostFocus += NumericTextBox_LostFocus;


            //this.Loaded += NumericTextBox_Loaded;

            //this.TextChanged += NumericTextBox_TextChanged;
        }

        private void this_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var etb = e.Source as EnterTextBox;

            if (etb.Text.Length + e.Text.Length - etb.SelectionLength > Size)
            {
                e.Handled = true;
            }
        }
    }
}
