using II.RPA.Plugin.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.IE.V2.Automation.Provider
{
    public enum IEMode
    {
        IE8,
        IE11
    }
    internal class IEModel
    {
        public int id { get; set; } 
        public string title { get; set; } = string.Empty;
        public string url { get; set; } = string.Empty;
        public string status { get; set; } = string.Empty;
        public int windowId => mainWindowHandle;
        public int mainWindowHandle => (int)NativeMethods.GetAncestor((IntPtr)id, GetAncestorFlags.GetRoot);
    }
}
