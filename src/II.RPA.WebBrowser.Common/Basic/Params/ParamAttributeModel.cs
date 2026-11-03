using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.WebBrowser.Common.Basic.Params
{
    public class ParamAttributeModel : ParamModel
    {
        public string name { get; set; }
        public string value { get; set; }
        public bool isIntact { get; set; }
        public string apiVersion { get; set; }
    }
}
