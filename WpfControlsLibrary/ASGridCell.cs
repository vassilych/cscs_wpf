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
        
        public static readonly DependencyProperty DecimalChrsProperty = DependencyProperty.Register("DecimalChrs", typeof(int?), typeof(ASGridCell));
        public int? DecimalChrs
        {
            get
            {
                return (int?)base.GetValue(DecimalChrsProperty);
            }
            set
            {
                base.SetValue(DecimalChrsProperty, value);
            }
        }
        
        public static readonly DependencyProperty SizeProperty = DependencyProperty.Register("Size", typeof(int?), typeof(ASGridCell));
        public int? Size
        {
            get
            {
                return (int?)base.GetValue(SizeProperty);
            }
            set
            {
                base.SetValue(SizeProperty, value);
            }
        }

        public static readonly DependencyProperty ThousandsProperty = DependencyProperty.Register("Thousands", typeof(bool), typeof(ASGridCell));
        public bool Thousands
        {
            get
            {
                return (bool)base.GetValue(ThousandsProperty);
            }
            set
            {
                base.SetValue(ThousandsProperty, value);
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
