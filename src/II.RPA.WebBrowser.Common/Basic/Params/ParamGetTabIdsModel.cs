using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.WebBrowser.Common.Basic.Params
{
    public class ParamGetTabIdsModel : ParamModel
    {
        public JObject queryObject { get; set; } = new JObject { };
        public List<IntPtr> WndHandles { get; set; }
        public int WindowId { get; set; }
        public bool Original { get; set; }
    }
}
