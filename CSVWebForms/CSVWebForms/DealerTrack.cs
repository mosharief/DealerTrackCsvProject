using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CSVWebForms
{
    public class DealerTrack
    {
        public int DealNumber { get; set; }
        public string CustomerName { get; set; }
        public string DealershipName { get; set; }
        public string Vehicle { get; set; }
        public string Price { get; set; }
        public DateTime Date { get; set; }
    }
}