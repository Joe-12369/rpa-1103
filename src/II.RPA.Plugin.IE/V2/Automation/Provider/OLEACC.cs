using MSHTML;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.IE.V2.Automation.Provider
{
    internal static class OLEACC
    {
        internal static Guid IID_IHTMLDocument = new Guid("626FC520-A41E-11CF-A731-00A0C9082637");
        [DllImport("OLEACC.dll")]
        internal static extern int ObjectFromLresult(int lResult, ref Guid riid, int wParam, ref IHTMLDocument2 ppvObject);
    }
}
