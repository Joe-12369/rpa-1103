using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.ZExplorer.Cef.EventArgs
{
    public class ErrorEventArgs : System.EventArgs
    {
        public ErrorEventArgs(Exception exception)
        {
            this.Exception = exception;
        }

        internal Exception Exception { get; }
    }
}
