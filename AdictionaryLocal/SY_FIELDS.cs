using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfCSCS.Adictionary
{
    public class SY_FIELDS
    {
        public int SYTD_ID { get; set; } 
        public string SYTD_SCHEMA { get; set; } // table schema in which this field exists
        public string SYTD_FIELD { get; set; } // field(column) name
        public string SYTD_DESCRIPTION { get; set; } // field description
        public string SYTD_TYPE { get; set; } // field type (I, A, N, R, B...)
        public int SYTD_SIZE { get; set; } // field size
        public int SYTD_DEC { get; set; } // number of decimal digits
        public string SYTD_CASE { get; set; } // Y or N -> uppercase??
        public int SYTD_ARRAYNUM { get; set; } // number of elements
    }
}
