using II.RPA.Plugin.Core;
using MSHTML;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using II.RPA.Plugin.IE.V2.Automation.Provider.EnumWindow;
using System.Windows.Documents;
using II.RPA.Plugin.IE.V2.Automation.Core;
using System.Threading;
using System.Diagnostics;
using II.RPA.Plugin.GUI.Commons.WindowMatch;
using II.RPA.WebBrowser.Common.Basic;
using System.Linq;

namespace II.RPA.Plugin.IE.V2.Automation.Provider
{
    internal class DocumentHelper
    {
        [DllImport("user32.dll", EntryPoint = "GetClassNameA")]
        private static extern int GetClassName(IntPtr hwnd, StringBuilder lpClassName, int nMaxCount);
        /*delegate to handle EnumChildWindows*/
        private delegate int EnumProc(IntPtr hWnd, ref IntPtr lParam);
        [DllImport("user32.dll")]
        private static extern int EnumChildWindows(IntPtr hWndParent, EnumProc lpEnumFunc, ref IntPtr lParam);
        [DllImport("user32.dll", EntryPoint = "RegisterWindowMessageA")]
        private static extern int RegisterWindowMessage(string lpString);
        [DllImport("user32.dll", EntryPoint = "SendMessageTimeoutA")]
        private static extern int SendMessageTimeout(IntPtr hwnd, int msg, int wParam, int lParam, int fuFlags, int uTimeout, out int lpdwResult);
        [DllImport("OLEACC.dll")]
        private static extern int ObjectFromLresult(int lResult, ref Guid riid, int wParam, ref IHTMLDocument2 ppvObject);
        private const int SMTO_ABORTIFHUNG = 0x2;
        private static Guid IID_IHTMLDocument = new Guid("626FC520-A41E-11CF-A731-00A0C9082637");
        private static ILogger logger = Logger.OfContext();
        public static HTMLDocument WebDocument(HTMLDocument document)
        {
            try
            {
                logger.LogDebug("WebDocument" + Thread.CurrentThread.ManagedThreadId.ToString());
                HTMLDocument win2 = document;
                Guid IID_IHTMLDocument = typeof(HTMLDocument).GUID;
                IServiceProvider pro = win2 as IServiceProvider;
                object objWin2 = null;
                pro.QueryService(ref IID_IHTMLDocument, ref IID_IHTMLDocument, out objWin2);
                return objWin2 as HTMLDocument;
            }
            catch (Exception e)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, e, "IE WebDocumentWindow Error");
                return null;
            }

        }
        internal static List<IntPtr> GetCurrentDocumentHandle()
        {
            List<IntPtr> handles = new List<IntPtr>();
            var embeddings = DocumentEmbedding();
            if (embeddings.Count > 0)
                handles.AddRange(embeddings);

            return handles;
        }
        internal static Dictionary<IntPtr, string> GetDocumentAll()
        {
            var handles = new Dictionary<IntPtr, string>();
            var docs = GetDocumentFrom();
            foreach (KeyValuePair<IntPtr, IHTMLDocument2> doc in docs)
            {
                if (!Utils.VerifyPraocessNameByHandle(doc.Key))
                {
                    logger.LogInformation($"title:{doc.Value.title} handle:{doc.Key}");
                    handles.Add(doc.Key, doc.Value.title);
                }
            }
            return handles;
        }
        private static Dictionary<IntPtr, IHTMLDocument2> GetDocumentFrom()
        {
            Dictionary<IntPtr, IHTMLDocument2> valuePairs = new Dictionary<IntPtr, IHTMLDocument2>();
            var handles = GetCurrentDocumentHandle();
            List<IHTMLDocument2> doc2s = new List<IHTMLDocument2>();
            foreach (var handle in handles)
            {
                var doc2 = GetDocumentByIntPtr(handle);
                if (doc2 != null)
                    valuePairs.Add(handle, doc2);
            }
            return valuePairs;
        }
        internal static IntPtr GetIEDocumentWnd(IntPtr wndHandle, bool active)
        {
            return GetIEDocumentWnds(new List<IntPtr>() { wndHandle }, active).FirstOrDefault();
        }
        internal static List<IntPtr> GetIEDocumentWnds(List<IntPtr> wndHandles, bool active)
        {
            if (active)
            {
                var dialoghandle = DocumentHelper.DocumentDialogFrom();
                if (dialoghandle != IntPtr.Zero)
                    return new List<IntPtr>() { dialoghandle };
            }
            List<BrowserWindow> browserWnds;
            if (wndHandles == null)
            {
                browserWnds = BrowserWindowProvider.GetAllBrowserByZOrder((wndInfo) => wndInfo.IsWindowVisible, WindowType.IE);
            }
            else
            {
                browserWnds = wndHandles
                        .Select(handle => BrowserWindowProvider.MatchBrowserWindowByHandle(handle))
                        .Where(wnd => wnd != null
                                    && wnd.WindowType.HasFlag(WindowType.IE)
                                    && !Utils.VerifyPraocessNameByHandle(wnd.Handle))
                        .ToList();
            }
            return browserWnds
                .SelectMany(wnd =>
                            wnd.EnumAllChildWindows((childWndInfo) => (!active || childWndInfo.IsVisible) && wnd.IsRenderWindow(childWndInfo), false))
                .Select(wnd => wnd.Handle)
                .ToList();
        }
        internal static IntPtr InternetExplorerDocument()
        {
            var dialoghandle = DocumentDialogFrom();
            if (dialoghandle != IntPtr.Zero)
                return dialoghandle;
            var parent = NativeMethods.FindWindow("IEFrame", null);
            var frameIntPtr = NativeMethods.FindWindowEx(parent, new IntPtr(), "Frame Tab", null);
            var tabHandle = NativeMethods.FindWindowEx(frameIntPtr, new IntPtr(), "TabWindowClass", null);
            EnumProc proc = new EnumProc(EnumWindows);
            EnumChildWindows(tabHandle, proc, ref tabHandle);
            logger.LogDebug($"InternetExplorerDocument handle {tabHandle}");
            return tabHandle;
        }
        internal static IntPtr InternetExplorerDocument(IntPtr handle)
        {
            var parent = NativeMethods.FindWindowEx(handle, new IntPtr(), "Chrome_WidgetWin_2", null);
            if (parent == IntPtr.Zero)
                parent = handle;
            parent = NativeMethods.FindWindowEx(handle, new IntPtr(), "IEFrame", null);
            var frameIntPtr = NativeMethods.FindWindowEx(parent, new IntPtr(), "Frame Tab", null);
            var tabHandle = NativeMethods.FindWindowEx(frameIntPtr, new IntPtr(), "TabWindowClass", null);
            EnumProc proc = new EnumProc(EnumWindows);
            EnumChildWindows(tabHandle, proc, ref tabHandle);
            return tabHandle;
        }
        internal static List<IntPtr> DocumentEmbedding()
        {
            List<IntPtr> handles = new List<IntPtr>();
            try
            {
                var windows = WindowEnumerator.FindAll();
                foreach (var item in windows)
                {
                    List<IntPtr> embeddings = new List<IntPtr>();
                    WindowHandleInfo windowHandleInfo = new WindowHandleInfo(item.Hwnd);
                    embeddings = windowHandleInfo.GetAllChildHandles();
                    foreach (var embedding in embeddings)
                    {
                        //    var view = NativeMethods.FindWindowEx(embedding, new IntPtr(), "Shell DocObject View", null);
                        //    var windowIntPtr = NativeMethods.FindWindowEx(view, new IntPtr(), "Internet Explorer_Server", null);
                        if (embedding != IntPtr.Zero)
                        {
                            handles.Add(embedding);
                        }
                        //IntPtr windowIntPtr = InternetExplorerDocument(embedding);
                        //if (windowIntPtr != IntPtr.Zero)
                        //    handles.Add(windowIntPtr);
                    }
                }
                return handles;
            }
            catch (Exception)
            {
                return null;
            }
        }

        internal static IntPtr DocumentDialogFrom()
        {
            try
            {
                var dialog = NativeMethods.FindWindowEx(IntPtr.Zero, IntPtr.Zero, "Internet Explorer_TridentDlgFrame", null);
                EnumProc proc = new EnumProc(EnumWindows);
                EnumChildWindows(dialog, proc, ref dialog);
                return dialog;
            }
            catch (Exception e)
            {
                return IntPtr.Zero;
            }
        }

        internal static IHTMLDocument2 DocumentFromWindow(IHTMLWindow2 win)
        {
            try
            {
                IHTMLWindow2 win2 = win;
                Guid IID_IHTMLWindow2 = typeof(IHTMLWindow2).GUID;
                IServiceProvider pro = win2 as IServiceProvider;
                object objWin2 = null;
                pro.QueryService(ref IID_IHTMLWindow2, ref IID_IHTMLWindow2, out objWin2);
                win2 = objWin2 as IHTMLWindow2;
                return win2.document;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        internal static IHTMLDocument2 GetDocumentByIntPtr(IntPtr hWnd)
        {
            IHTMLDocument2 document = null;
            int lngMsg = 0;
            int lRes;
            EnumProc proc = new EnumProc(EnumWindows);
            EnumChildWindows(hWnd, proc, ref hWnd);
            if (!hWnd.Equals(IntPtr.Zero))
            {
                lngMsg = RegisterWindowMessage("WM_HTML_GETOBJECT");
                if (lngMsg != 0)
                {
                    SendMessageTimeout(hWnd, lngMsg, 0, 0, SMTO_ABORTIFHUNG, 1000, out lRes);
                    if (lRes != 0)
                    {
                        int hr = ObjectFromLresult(lRes, ref IID_IHTMLDocument, 0, ref document);
                        if (document != null)
                        {
                            return document;
                        }
                    }
                }
            }
            return null;
        }

        private static int EnumShellEmbeddingWindows(IntPtr hWnd, ref IntPtr lParam)
        {
            int retVal = 1;
            StringBuilder classname = new StringBuilder(128);
            GetClassName(hWnd, classname, classname.Capacity);
            if (string.Compare(classname.ToString(), "Shell Embedding") == 0)
            {
                lParam = hWnd;
                retVal = 0;
            }
            return retVal;
        }
        private static int EnumWindows(IntPtr hWnd, ref IntPtr lParam)
        {
            int retVal = 1;
            StringBuilder classname = new StringBuilder(128);
            GetClassName(hWnd, classname, classname.Capacity);
            if (string.Compare(classname.ToString(), "Internet Explorer_Server") == 0)
            {
                lParam = hWnd;
                retVal = 0;
            }
            return retVal;
        }
    }
}

