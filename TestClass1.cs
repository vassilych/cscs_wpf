using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WpfCSCS
{
    public class TestClass1
    {
        public int int1 = 10;
        public int int2 = 20;
        public string string1 { get; set; } = "asdfydfgdsfhgsdfgh";
        public void PrintProperties()
        {
            MessageBox.Show("int1 = " + int1 + "\n" + 
                "int2 = " + int2 + "\n" + 
                "string1 = " + string1);
        }
    }
}
