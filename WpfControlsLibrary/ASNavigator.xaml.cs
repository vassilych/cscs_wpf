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
    public partial class ASNavigator : UserControl
    {
        RoutedCommand trapF5 = new RoutedCommand();
        RoutedCommand trapF6 = new RoutedCommand();
        RoutedCommand trapF7 = new RoutedCommand();
        RoutedCommand trapF8 = new RoutedCommand();
        public ASNavigator()
        {
            InitializeComponent();

            trapF5.InputGestures.Add(new KeyGesture(Key.F5));
            trapF6.InputGestures.Add(new KeyGesture(Key.F6));
            trapF7.InputGestures.Add(new KeyGesture(Key.F7));
            trapF8.InputGestures.Add(new KeyGesture(Key.F8));
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Window.GetWindow(this).CommandBindings.Add(new CommandBinding(trapF5, F5Handler));
            Window.GetWindow(this).CommandBindings.Add(new CommandBinding(trapF6, F6Handler));
            Window.GetWindow(this).CommandBindings.Add(new CommandBinding(trapF7, F7Handler));
            Window.GetWindow(this).CommandBindings.Add(new CommandBinding(trapF8, F8Handler));
        }

        private void F5Handler(object sender, ExecutedRoutedEventArgs e)
        {
            NavigatorFirst();
        }

        private void F6Handler(object sender, ExecutedRoutedEventArgs e)
        {
            NavigatorLast();
        }

        private void F7Handler(object sender, ExecutedRoutedEventArgs e)
        {
            NavigatorPrevious();
        }

        private void F8Handler(object sender, ExecutedRoutedEventArgs e)
        {
            NavigatorNext();
        }

        

        public NavigatorButton buttonClicked;

        public event EventHandler Navigator_buttonClicked;

        private void FirstButton_Click(object sender, RoutedEventArgs e)
        {
            NavigatorFirst();
        }
        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            NavigatorPrevious();
        }
        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            NavigatorNext();
        }
        private void LastButton_Click(object sender, RoutedEventArgs e)
        {
            NavigatorLast();
        }

        private void NavigatorFirst()
        {
            buttonClicked = NavigatorButton.First;
            Navigator_buttonClicked(this, EventArgs.Empty);
        }
        private void NavigatorPrevious()
        {
            buttonClicked = NavigatorButton.Previous;
            Navigator_buttonClicked(this, EventArgs.Empty);
        }
        private void NavigatorNext()
        {
            buttonClicked = NavigatorButton.Next;
            Navigator_buttonClicked(this, EventArgs.Empty);
        }
        private void NavigatorLast()
        {
            buttonClicked = NavigatorButton.Last;
            Navigator_buttonClicked(this, EventArgs.Empty);
        }
    }
}
