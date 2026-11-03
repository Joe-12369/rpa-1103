using MSHTML;
using SHDocVw;
using System;
using System.Runtime.InteropServices;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using II.RPA.Plugin.Core;

namespace II.RPA.Plugin.IE.V2.Automation.Provider
{  // This is the COM IServiceProvider interface, not System.IServiceProvider .Net interface!
    [ComImport(), ComVisible(true), Guid("6D5140C1-7436-11CE-8034-00AA006009FA"),
    InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IServiceProvider
    {
        [return: MarshalAs(UnmanagedType.I4)]
        [PreserveSig]
        int QueryService(ref Guid guidService, ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out object ppvObject);
    }

    internal class FindWebBrowser
    {
        private static ILogger logger = Logger.OfContext();
        private static readonly Guid GuidOfIServiceProvider = typeof(IServiceProvider).GUID;
        private static Guid SID_STopLevelBrowser = new Guid(1284947520U, 37212, 4559, 153, 211, 0, 170, 0, 74, 232, 55);
        private static readonly Guid GuidOfIWebBrowser2 = typeof(IWebBrowser2).GUID;
        private static Guid SID_SWebBrowserApp = new Guid(188165, 0, 0, 192, 0, 0, 0, 0, 0, 0, 70);
        public FindWebBrowser()
        {

        }
        public static bool ActivateMainWindow(IntPtr handle)
        {
            NativeMethods.SwitchToThisWindow(handle, true);
            NativeMethods.SetForegroundWindow(handle);
            return true;
        }
        public static IWebBrowser2 GetIEWebBrowser(IHTMLDocument2 doc2)
        {
            return IEWebBrowser(doc2);
        }
        public static IWebBrowser2 IEWebBrowser(IHTMLDocument2 hTMLDocument2)
        {
            try
            {
                IHTMLDocument2 document2 = hTMLDocument2;
                IHTMLWindow2 parentWindow = null;
                parentWindow = WebWindowHelper.WebWindow(document2.parentWindow);
                Guid guidOfIServiceProvider = GuidOfIServiceProvider;
                IServiceProvider serviceProvider = parentWindow as IServiceProvider;
                object obj;
                serviceProvider.QueryService(ref SID_STopLevelBrowser, ref guidOfIServiceProvider, out obj);
                serviceProvider = (obj as IServiceProvider);
                Guid guidOfIWebBrowser = GuidOfIWebBrowser2;
                object obj2;
                serviceProvider.QueryService(ref SID_SWebBrowserApp, ref guidOfIWebBrowser, out obj2);

                IWebBrowser2 webBrowser = obj2 as IWebBrowser2;
                if (webBrowser != null)
                {
                    return webBrowser;
                }
            }
            catch (Exception e)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, e, "IE IEWebBrowser Error");
            }
            return null;
        }
    }
}

