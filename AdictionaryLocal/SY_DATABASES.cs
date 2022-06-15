using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfCSCS.Adictionary
{
    public class SY_DATABASES
    {
        public int SYCD_ID { get; set; }
        public string SYCD_USERCODE { get; set; } // short DB name e.g. "BD1" or "B"
        public string SYCD_COMPCODE { get; set; } // company name e.g. "KAMEND"
        public string SYCD_YEAR { get; set; } // DB year
        public string SYCD_DESCRIPTION { get; set; } // DB descriptioon
        public string SYCD_DBASENAME { get; set; } // full DB name
        public string SYCD_USERNAME { get; set; } // ??
        public string SYCD_USERPSWD { get; set; } // ??
    }
}
