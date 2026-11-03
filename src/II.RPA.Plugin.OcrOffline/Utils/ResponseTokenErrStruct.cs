using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.OcrOffline.Utils
{
    public class ResponseTokenErrStruct
    {
        public string msg { get; set; } = string.Empty;
        public string code { get; set; } = string.Empty;
        public string sub_msg { get; set; } = string.Empty;
        public string sub_code { get; set; } = string.Empty;
    }
}
