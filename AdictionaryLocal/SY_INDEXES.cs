using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfCSCS.Adictionary
{
    public class SY_INDEXES
    {
        public int SYKI_ID { get; set; }
        public string SYKI_SCHEMA { get; set; } // shecma in which this index exists
        public string SYKI_DESCRIPTION { get; set; } // index description
        public string SYKI_KEYNAME { get; set; } // index name
        public int SYKI_KEYNUM { get; set; } // index priority number for current schema
        public string SYKI_FIELD { get; set; } // field for the index. There can be more than one field(segment) for one index
        public string SYKI_UNIQUE { get; set; } // is the index unique? Y or N
        public string SYKI_ASCDESC { get; set; } // sorting type for the current index. A or D(asc or desc)
        public int? SYKI_SEGNUM { get; set; } // ordinal number of the segment(field) for the current index
    }
}
