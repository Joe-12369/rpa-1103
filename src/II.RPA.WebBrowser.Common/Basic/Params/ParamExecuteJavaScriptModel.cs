using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.WebBrowser.Common.Basic.Params
{
    public class ParamExecuteJavaScriptModel : ParamModel
    {
        public string scriptCode { get; set; }
        public string ExecutionWorld { get; set; }
    }
}
