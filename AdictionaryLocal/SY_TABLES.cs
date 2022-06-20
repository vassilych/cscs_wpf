using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfCSCS.Adictionary
{
    public class SY_TABLES
    {
        public int SYCT_ID { get; set; }
        public string SYCT_NAME { get; set; } // table name
        public string SYCT_SCHEMA { get; set; } // table schema
        public string SYCT_DESCRIPTION { get; set; } // table description
        public string SYCT_TYPE { get; set; } // ??
        public string SYCT_USERCODE { get; set; } // short DB name e.g. "BD1" or "B"
    }
}
