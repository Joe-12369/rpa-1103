using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft;

namespace II.RPA.Plugin.ZExplorer.IPC
{
    public class BrowserLog
    {
        public static void Write(string action, object content)
        {
            Debug.WriteLine($"[ZExplore]:{action}->{Newtonsoft.Json.JsonConvert.SerializeObject(content)}");
        }
    }
}
