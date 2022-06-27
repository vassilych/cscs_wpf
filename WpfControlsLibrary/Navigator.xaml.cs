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
    public enum NavigatorButton
    {
        First, Previous, Next, Last
    }
    public partial class Navigator : UserControl
    {
        public Navigator()
        {
            InitializeComponent();
        }

        public NavigatorButton buttonClicked;

        public event EventHandler Navigator_buttonClicked;

        private void FirstButton_Click(object sender, RoutedEventArgs e)
        {
            buttonClicked = NavigatorButton.First;
            Navigator_buttonClicked(this, EventArgs.Empty);
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            buttonClicked = NavigatorButton.Previous;
            Navigator_buttonClicked(this, EventArgs.Empty);
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            buttonClicked = NavigatorButton.Next;
            Navigator_buttonClicked(this, EventArgs.Empty);
        }

        private void LastButton_Click(object sender, RoutedEventArgs e)
        {
            buttonClicked = NavigatorButton.Last;
            Navigator_buttonClicked(this, EventArgs.Empty);
        }




    }
}
