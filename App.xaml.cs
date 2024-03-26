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
using System.Windows.Media;
using SplitAndMerge;

namespace WpfCSCS
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
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
    }
}
