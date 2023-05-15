using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Shapes;

namespace WpfControlsLibrary
{
    /// <summary>
    /// Interaction logic for ASDualListDialog.xaml
    /// </summary>
    public partial class ASDualListDialog : Window
    {
        private ObservableCollection<string> leftList;
        public ObservableCollection<string> LeftList
        {
            get { return leftList; }
            set { leftList = value; RedisplayLists(); }
        }
        
        private ObservableCollection<string> rightList;
        public ObservableCollection<string> RightList
        {
            get { return rightList; }
            set { rightList = value; RedisplayLists(); }
        }

        //public List<string> LeftList = new List<string>();
        //public List<string> RightList = new List<string>();

        public static readonly DependencyProperty OkButtonCaptionProperty = DependencyProperty.Register("OkButtonCaption", typeof(string), typeof(ASDualListDialog));
        public string OkButtonCaption
        {
            get
            {
                return (string)base.GetValue(OkButtonCaptionProperty);
            }
            set
            {
                base.SetValue(OkButtonCaptionProperty, value);
            }
        }

        public static readonly DependencyProperty CancelButtonCaptionProperty = DependencyProperty.Register("CancelButtonCaption", typeof(string), typeof(ASDualListDialog));
        public string CancelButtonCaption
        {
            get
            {
                return (string)base.GetValue(CancelButtonCaptionProperty);
            }
            set
            {
                base.SetValue(CancelButtonCaptionProperty, value);
            }
        }

        public static readonly DependencyProperty HelpButtonCaptionProperty = DependencyProperty.Register("HelpButtonCaption", typeof(string), typeof(ASDualListDialog));
        public string HelpButtonCaption
        {
            get
            {
                return (string)base.GetValue(HelpButtonCaptionProperty);
            }
            set
            {
                base.SetValue(HelpButtonCaptionProperty, value);
            }
        }

        public static readonly DependencyProperty Label1CaptionProperty = DependencyProperty.Register("Label1Caption", typeof(string), typeof(ASDualListDialog));
        public string Label1Caption
        {
            get
            {
                return (string)base.GetValue(Label1CaptionProperty);
            }
            set
            {
                base.SetValue(Label1CaptionProperty, value);
            }
        }

        public static readonly DependencyProperty Label2CaptionProperty = DependencyProperty.Register("Label2Caption", typeof(string), typeof(ASDualListDialog));
        public string Label2Caption
        {
            get
            {
                return (string)base.GetValue(Label2CaptionProperty);
            }
            set
            {
                base.SetValue(Label2CaptionProperty, value);
            }
        }

        public static readonly DependencyProperty DialogTitleProperty = DependencyProperty.Register("DialogTitle", typeof(string), typeof(ASDualListDialog));
        public string DialogTitle
        {
            get
            {
                return (string)base.GetValue(DialogTitleProperty);
            }
            set
            {
                base.SetValue(DialogTitleProperty, value);
            }
        }

        public static readonly DependencyProperty ShowHelpProperty = DependencyProperty.Register("ShowHelp", typeof(bool), typeof(ASDualListDialog));
        public bool ShowHelp
        {
            get
            {
                return (bool)base.GetValue(ShowHelpProperty);
            }
            set
            {
                base.SetValue(ShowHelpProperty, value);
            }
        }

        public static readonly DependencyProperty SortedProperty = DependencyProperty.Register("Sorted", typeof(bool), typeof(ASDualListDialog));
        public bool Sorted
        {
            get
            {
                return (bool)base.GetValue(SortedProperty);
            }
            set
            {
                base.SetValue(SortedProperty, value);
            }
        }

        
        public ASDualListDialog()
        {
            InitializeComponent();
            RedisplayLists();

            //Title = DialogTitle;
        }

        private void RedisplayLists()
        {
            List1.ItemsSource = this.LeftList;
            List2.ItemsSource = this.RightList;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if(List1.SelectedItem != null)
            {
                var selectedIndex = List1.SelectedIndex;
                RightList.Add(LeftList[selectedIndex]);
                LeftList.RemoveAt(selectedIndex);
            }
        }

        private void AddAllButton_Click(object sender, RoutedEventArgs e)
        {
            for(int i = 0; i < List1.Items.Count; i++)
            {
                RightList.Add(LeftList[i]);
                LeftList.RemoveAt(i);
                i--;
            }
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (List2.SelectedItem != null)
            {
                var selectedIndex = List2.SelectedIndex;
                LeftList.Add(RightList[selectedIndex]);
                RightList.RemoveAt(selectedIndex);
            }
            LeftList = new ObservableCollection<string>(LeftList.ToList().OrderBy(p=>p).ToList());
        }

        private void RemoveAllButton_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < List2.Items.Count; i++)
            {
                LeftList.Add(RightList[i]);
                RightList.RemoveAt(i);
                i--;
            }
            LeftList = new ObservableCollection<string>(LeftList.ToList().OrderBy(p => p).ToList());
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            RightList = new ObservableCollection<string>();
            this.Close();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("DEFAULT HELP !!!!! ");
        }
    }
}
