using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SplitAndMerge;
using WpfControlsLibrary;

namespace WpfCSCS
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            EventManager.RegisterClassHandler(typeof(ASEnterBox), TextBox.KeyDownEvent, new KeyEventHandler(Generic_KeyDown));
            EventManager.RegisterClassHandler(typeof(RadioButton), CheckBox.KeyDownEvent, new KeyEventHandler(Generic_KeyDown));
            EventManager.RegisterClassHandler(typeof(CheckBox), CheckBox.KeyDownEvent, new KeyEventHandler(Generic_KeyDown));
            EventManager.RegisterClassHandler(typeof(ComboBox), CheckBox.KeyDownEvent, new KeyEventHandler(Generic_KeyDown));
            EventManager.RegisterClassHandler(typeof(TextBox), CheckBox.KeyDownEvent, new KeyEventHandler(Generic_KeyDown)); // for ASDateEditer

            base.OnStartup(e);                                                  
                                                                                
            //System.Threading.Thread.Sleep(1 * 1000);
            //System.Diagnostics.Debugger.Launch();
            //System.Diagnostics.Debugger.Break();

            string cscsScript = e.Args.Length == 0 ? GetConfiguration("CSCS_Init", "../../scripts/defaultWindow.cscs") : e.Args[0];
            var pathName = Path.GetFullPath(cscsScript);
            if (!File.Exists(pathName))
            {
                MessageBox.Show("File " + pathName + " doesn't exist.", "Invalid file path",
                                         MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
            }

            CSCS_GUI.Dispatcher = this.Dispatcher;
            CSCS_GUI gui = new CSCS_GUI();

            CSCS_SQL.ConnectionString = App.GetConfiguration("ConnectionString", "");
            if(App.GetConfiguration("LoadAdictionary", "false") == "true")
            {
                gui.CacheAdictionary();
                gui.FillDatabasesDictionary();
            }

            //cscsScript = "../../scripts/start.cscs";
            Console.WriteLine("Running CSCS script: " + pathName);

            try
            {
                gui.RunScript(pathName);
            }
            catch (Exception exc)
            {
                MessageBox.Show("Error running " + pathName + ": " + exc.Message, "Error running script",
                                         MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
            }

            //StartupUri = new Uri("../MainWindow.xaml", UriKind.Relative);
        }

        public static string GetConfiguration(string key, string defaultValue = null)
        {
            var settings = ConfigurationManager.AppSettings;
            if (settings == null || settings.Count == 0 || !settings.AllKeys.Contains(key))
            {
                return defaultValue;
            }
            return settings.Get(key);
        }

        void Generic_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter/* & (sender as TextBox).AcceptsReturn == false*/) MoveToNextUIElement(e);
        }

        void CheckBox_KeyDown(object sender, KeyEventArgs e)
        {
            MoveToNextUIElement(e);
            //Sucessfully moved on and marked key as handled.
            //Toggle check box since the key was handled and
            //the checkbox will never receive it.
            if (e.Handled == true)
            {
                CheckBox cb = (CheckBox)sender;
                cb.IsChecked = !cb.IsChecked;
            }

        }

        void MoveToNextUIElement(KeyEventArgs e)
        {
            // Creating a FocusNavigationDirection object and setting it to a
            // local field that contains the direction selected.
            FocusNavigationDirection focusDirection = FocusNavigationDirection.Next;

            // MoveFocus takes a TraveralReqest as its argument.
            TraversalRequest request = new TraversalRequest(focusDirection);

            // Gets the element with keyboard focus.
            UIElement elementWithFocus = Keyboard.FocusedElement as UIElement;

            // Change keyboard focus.
            if (elementWithFocus != null)
            {
                if (elementWithFocus.MoveFocus(request)) e.Handled = true;
            }
        }
    }
}
