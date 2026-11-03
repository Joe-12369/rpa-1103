using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Commons.Params
{
    public class QuerySelectorParams
    {
        public string ElementIds { get; set; }
        public string Selector { get; set; }

    }
    public class BulidSelectorsParams
    {
        public string ElementIds { get; set; }
        public string WindowId { get; set; }
        public int TabId { get; set; }
    }
}
