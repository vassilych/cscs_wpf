using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace WpfControlsLibrary
{
    public class ASDualListDialogHelper : Control
    {
        public List<string> List1 = new List<string>();
        public List<string> List2 = new List<string>();

        public static readonly DependencyProperty OkButtonCaptionProperty = DependencyProperty.Register("OkButtonCaption", typeof(string), typeof(ASDualListDialogHelper));
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
        
        public static readonly DependencyProperty CancelButtonCaptionProperty = DependencyProperty.Register("CancelButtonCaption", typeof(string), typeof(ASDualListDialogHelper));
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
        
        public static readonly DependencyProperty HelpButtonCaptionProperty = DependencyProperty.Register("HelpButtonCaption", typeof(string), typeof(ASDualListDialogHelper));
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
        
        public static readonly DependencyProperty Label1CaptionProperty = DependencyProperty.Register("Label1Caption", typeof(string), typeof(ASDualListDialogHelper));
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
        
        public static readonly DependencyProperty Label2CaptionProperty = DependencyProperty.Register("Label2Caption", typeof(string), typeof(ASDualListDialogHelper));
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
        
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register("Title", typeof(string), typeof(ASDualListDialogHelper));
        public string Title
        {
            get
            {
                return (string)base.GetValue(TitleProperty);
            }
            set
            {
                base.SetValue(TitleProperty, value);
            }
        }
        
        public static readonly DependencyProperty ShowHelpProperty = DependencyProperty.Register("ShowHelp", typeof(bool), typeof(ASDualListDialogHelper));
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
        
        public static readonly DependencyProperty SortedProperty = DependencyProperty.Register("Sorted", typeof(bool), typeof(ASDualListDialogHelper));
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


        public ASDualListDialogHelper()
        {
            
        }
    }
}
