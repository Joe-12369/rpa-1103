using II.RPA.Plugin.IE.V2.Automation.Provider.EnumWindow;
using MSHTML;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows.Threading;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using II.RPA.Plugin.IE.V2.Automation.Core;
using II.RPA.Plugin.Core;

namespace II.RPA.Plugin.IE.V2.Automation.Provider
{
    internal class CacheDocument
    {
        public IHTMLDocument2 IHTMLDocument2 { get; set; }
        private IHTMLWindow2 _IHTMLWindow2 = null;
        public IHTMLWindow2 IHTMLWindow2
        {
            get
            {
                if (_IHTMLWindow2 == null)
                {
                    _IHTMLWindow2 = GetDocumentWindow2(IHTMLDocument2);
                    Utils.InjectJavaScript(_IHTMLWindow2);
                }
                return _IHTMLWindow2;
            }
        }
        private IHTMLWindow2 GetDocumentWindow2(IHTMLDocument2 doc2)
        {
            return WebWindowHelper.WebWindow(doc2.parentWindow);
        }
    }
    internal class DocumentProvider
    {
        private static ILogger logger = Logger.OfContext();
        public DocumentProvider()
        {
        }
        public DocumentProvider(string title) { }
        public DocumentProvider(IntPtr handle) { }
        //private Dictionary<IntPtr, IHTMLDocument2> docDic = null;
        private Dictionary<IntPtr, CacheDocument> doc2Dic = null;
        private Dispatcher m_Dispatcher;
        private readonly ManualResetEventSlim m_StartedEvent = new ManualResetEventSlim(false);


        internal void ClearRpaIHTMLDocument2Dic()
        {
            doc2Dic.Clear();
        }
        internal CacheDocument GetIHTMLDocument2(IntPtr handle)
        {
            if (doc2Dic == null || !doc2Dic.ContainsKey(handle))
            {
                doc2Dic = GetIHTMLDocument2Dic();
            }
            //if (!doc2Dic.ContainsKey(handle))
            {
                var doc = HTMLDocumentEnumerator.GetDocumentByIntPtr(handle);
                if (doc != null)
                {
                    doc2Dic[handle] = new CacheDocument { IHTMLDocument2 = doc };
                }
            }
            return doc2Dic.ContainsKey(handle) ? doc2Dic[handle] : null;
        }
        public Dictionary<IntPtr, CacheDocument> GetIHTMLDocument2Dic()
        {
            Dictionary<IntPtr, CacheDocument> docDic2 = new Dictionary<IntPtr, CacheDocument>();
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            logger.LogDebug("Start cache document");
            var windows = WindowEnumerator.FindAll();
            logger.LogDebug($"Find window time {stopWatch.ElapsedMilliseconds}");
            for (int i = 0; i < windows.Count; i++)
            {
                WindowHandleInfo windowHandleInfo = new WindowHandleInfo(windows[i].Hwnd);
                logger.LogDebug($"Get child window time {stopWatch.ElapsedMilliseconds} for handle {windows[i].Hwnd}");
                var handles = windowHandleInfo.GetAllChildHandles();
                logger.LogDebug($"Get all child window time {stopWatch.ElapsedMilliseconds} for handle {windows[i].Hwnd}");
                foreach (var handle in handles)
                {
                    logger.LogDebug($"Get document time {stopWatch.ElapsedMilliseconds} for handle {handle}");
                    var doc = HTMLDocumentEnumerator.GetDocumentByIntPtr(handle);
                    logger.LogDebug($"Get document time {stopWatch.ElapsedMilliseconds} for handle {handle}");
                    if (doc != null && !docDic2.ContainsKey(handle))
                    {
                        docDic2.Add(handle, new CacheDocument { IHTMLDocument2 = doc });
                    }
                }
            }
            logger.LogDebug($"Cache document time {stopWatch.ElapsedMilliseconds}");
            stopWatch.Stop();
            return docDic2;
        }
        public Dictionary<IntPtr, CacheDocument> GetIHTMLDocument2Dic(List<IntPtr> handles)
        {
            Dictionary<IntPtr, CacheDocument> docDic2 = new Dictionary<IntPtr, CacheDocument>();
            foreach (var handle in handles)
            {
                var doc = HTMLDocumentEnumerator.GetDocumentByIntPtr(handle);
                if (doc != null && !docDic2.ContainsKey(handle))
                {
                    docDic2.Add(handle, new CacheDocument { IHTMLDocument2 = doc });
                }
            }
            return docDic2;
        }
    }
}