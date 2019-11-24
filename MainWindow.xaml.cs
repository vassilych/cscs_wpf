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
            CSCS_GUI.RunScript("../../scripts/wpfgui.cscs");
//            CSCS_GUI.RunScript("../../scripts/start.cscs");
        }

        public List<Visual> GetVisualTreeInfo(Visual element)
        {
            if (element == null)
            {
                throw new ArgumentNullException(String.Format("Element {0} is null !", element.ToString()));
            }

            List<Visual> controls = new List<Visual>();

            GetControlsList(element, 0, controls);

            return controls;
        }

        void GetControlsList(Visual control, int level, List<Visual> controls)
        {
            int ChildNumber = VisualTreeHelper.GetChildrenCount(control);

            for (int i = 0; i <= ChildNumber - 1; i++)
            {
                Visual v = (Visual)VisualTreeHelper.GetChild(control, i);

                controls.Add(v);

                if (VisualTreeHelper.GetChildrenCount(v) > 0)
                {
                    GetControlsList(v, level + 1, controls);
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
