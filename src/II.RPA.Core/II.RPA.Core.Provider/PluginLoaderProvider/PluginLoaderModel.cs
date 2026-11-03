using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Core.Provider.PluginLoaderProvider
{
    public struct PluginRunAction
    {
        public string PluginName
        {
            get;
            set;
        }
        public string PluginAction
        {
            get;
            set;
        }
    }
    public class PluginRunResult
    {
        public string Code
        {
            get;
            set;
        }
        public string Message
        {
            get;
            set;
        }
        public string Result
        {
            get;
            set;
        }
        public string Extend
        {
            get;
            set;
        }
    }
}
