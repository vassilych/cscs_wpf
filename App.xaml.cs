using System;
using System.Collections.Generic;
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
            System.Diagnostics.Debugger.Launch();
            //System.Diagnostics.Debugger.Break();

            string filename = e.Args.Length == 0 ? "../MainWindow.xaml" : e.Args[0];
            var items = filename.Split(' ');
            filename = items[0];

            string dir = Directory.GetCurrentDirectory();
            var pathname = Path.Combine(dir, filename);
            Console.WriteLine("{0}, {1} --> {2}", filename, pathname, File.Exists(pathname));
            // C:\Users\vassi\Documents\GitHub\cscs_wpf\MainWindow.xaml, 
            StartupUri = new Uri(filename, UriKind.Relative);
        }
    }
}

