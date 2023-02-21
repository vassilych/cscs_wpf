using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace WpfControlsLibrary
{
    public class ASGridCell : Control
    {
        public static readonly DependencyProperty FieldNameProperty = DependencyProperty.Register("FieldName", typeof(string), typeof(ASGridCell));
        public string FieldName
        {
            get
            {
                return (string)base.GetValue(FieldNameProperty);
            }
            set
            {
                base.SetValue(FieldNameProperty, value);
            }
        }
        
        public static readonly DependencyProperty EditorProperty = DependencyProperty.Register("Editor", typeof(string), typeof(ASGridCell));
        public string Editor
        {
            get
            {
                return (string)base.GetValue(EditorProperty);
            }
            set
            {
                base.SetValue(EditorProperty, value);
            }
        }
        
        public static readonly DependencyProperty EditLengthProperty = DependencyProperty.Register("EditLength", typeof(int), typeof(ASGridCell));
        public int EditLength
        {
            get
            {
                return (int)base.GetValue(EditLengthProperty);
            }
            set
            {
                base.SetValue(EditLengthProperty, value);
            }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (EditLength == 0)
                EditLength = int.MaxValue;
        }
    }
}
