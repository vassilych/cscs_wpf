using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfCSCS.Adictionary;

namespace WpfCSCS.AdictionaryLocal
{
    public class Adictionary
    {
        public List<SY_DATABASES> SY_DATABASESList { get; set; }
        public List<SY_FIELDS> SY_FIELDSList { get; set; }
        public List<SY_INDEXES> SY_INDEXESList { get; set; }
        public List<SY_TABLES> SY_TABLESList { get; set; }
    }
}
