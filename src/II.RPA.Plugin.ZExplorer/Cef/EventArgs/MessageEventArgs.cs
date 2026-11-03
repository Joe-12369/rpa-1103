using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.ZExplorer.Cef.EventArgs
{
    public class MessageEventArgs : System.EventArgs
    {
        public MessageEventArgs(string data)
        {
            this.Data = data;
        }

        internal string Data { get; }
    }
}
