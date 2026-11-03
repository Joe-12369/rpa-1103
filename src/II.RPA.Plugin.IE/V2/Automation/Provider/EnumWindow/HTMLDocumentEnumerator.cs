using II.RPA.Plugin.Core;
using MSHTML;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;

namespace II.RPA.Plugin.IE.V2.Automation.Provider.EnumWindow
{
    internal static class HTMLDocumentEnumerator
    {
        private static ILogger logger = Logger.OfContext();
        private delegate int EnumProc(IntPtr hWnd, ref IntPtr lParam);
        [DllImport("user32.dll")]
        private static extern int EnumChildWindows(IntPtr hWndParent, EnumProc lpEnumFunc, ref IntPtr lParam);
        internal static int EnumWindows(IntPtr hWnd, ref IntPtr lParam)
        {
            int retVal = 1;
            StringBuilder classname = new StringBuilder(128);
            NativeMethods.GetClassName(hWnd, classname, classname.Capacity);
            if (string.Compare(classname.ToString(), "Internet Explorer_Server") == 0)
            {
                lParam = hWnd;
                retVal = 0;
            }
            return retVal;
        }
        internal static IHTMLDocument2 GetDocumentByIntPtr(IntPtr hWnd)
        {
            IHTMLDocument2 document = null;
            int lngMsg = 0;
            int lRes;
            if (!NativeMethods.GetClassName(hWnd).EqualIgnoreCase("Internet Explorer_Server"))
            {
                EnumProc proc = new EnumProc(EnumWindows);
                EnumChildWindows(hWnd, proc, ref hWnd);
            }
            if (!hWnd.Equals(IntPtr.Zero))
            {
                lngMsg = NativeMethods.RegisterWindowMessage("WM_HTML_GETOBJECT");
                if (lngMsg != 0)
                {
                    NativeMethods.SendMessageTimeout(hWnd, lngMsg, 0, 0, NativeMethods.SMTO_ABORTIFHUNG, 1000, out lRes);
                    if (lRes != 0)
                    {
                        int hr = OLEACC.ObjectFromLresult(lRes, ref OLEACC.IID_IHTMLDocument, 0, ref document);
                        if (document != null)
                        {
                            return document;
                        }
                    }
                }
            }
            return null;
        }
    }
}
