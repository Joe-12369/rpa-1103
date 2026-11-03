using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.WebBrowser.Common.Basic.Params
{
    public class ParamCreateTabModel : ParamModel
    {
        public string url { get; set; }
        public int windowId { get; set; }
        public string CreateProcessArgs { get; set; }
        public eGuiType guiType { get; set; }
        public bool IsOpenNewTab { get; set; }
        public bool IsWaitingload { get; set; }
        public int timeout { get; set; }



    }
}
