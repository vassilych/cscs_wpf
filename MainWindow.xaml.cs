using SplitAndMerge;
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

namespace WpfCSCS
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static Window TheWindow { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(MainView_Loaded);
        }

        void MainView_Loaded(object sender, RoutedEventArgs e)
        {           
            TheWindow = Window.GetWindow(this);
            var parent = VisualTreeHelper.GetParent(this);
            Window parentWindow = Window.GetWindow(this);

            CSCS_SQL.Init();
            CSCS_GUI.MainWindow = this;

            string[] cmdArgs = Environment.GetCommandLineArgs();
            if (cmdArgs.Length <= 1)
            {
                CSCS_GUI.RunScript("../../scripts/start.cscs");
                return;
            }

            var cmdLineParams = cmdArgs[1].Split(new char[] { ',' });
            var scriptName = cmdLineParams[0];
            /*string msg = "StartArgs:";
            for (int i = 0; i < cmdArgs.Length; i++)
            {
                msg += " [" + cmdArgs[i] + "]";
            }
            msg += " Script: [" + scriptName + "]";
            MessageBox.Show(msg, cmdArgs.Length + " args", MessageBoxButton.OK, MessageBoxImage.Asterisk);*/

            CSCS_GUI.RunScript("../../scripts/" + scriptName);
        }
    }
}
