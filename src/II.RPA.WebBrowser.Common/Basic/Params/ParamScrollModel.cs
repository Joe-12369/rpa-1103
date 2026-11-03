using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.WebBrowser.Common.Basic.Params
{
    public class ParamScrollModel : ParamModel
    {
        public eScrollDirection direction { get; set; }
        public int value { get; set; }
        public string location { get; set; }
        public string elementId { get; set; }
        public string targetElementId { get; set; }
        public string scrollType { get; set; }
        public int offSetX { get; set; }
        public int offSetY { get; set; }
    }
}
