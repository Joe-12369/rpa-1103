using System;
using Newtonsoft.Json.Linq;

namespace II.RPA.Plugin.ZExplorer.Cef.EventArgs
{
    public sealed class CommandEventArgs : System.EventArgs
    {
        public string Method { get; internal set; }

        public JObject Parameters { get; internal set; }
    }
}
