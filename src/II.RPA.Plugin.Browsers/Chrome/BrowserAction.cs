//using II.RPA.Logging;
//using II.RPA.Plugin.Browsers.Chrome.BrowserProfile;
//using II.RPA.Plugin.Core.Commons;
//using II.RPA.WebBrowser.Common;
//using II.RPA.WebBrowser.Common.MultiInstance;
//using II.RPA.WinAutomation.CommonUtility;
//using Microsoft.Extensions.Logging;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace II.RPA.Plugin.Browsers.Chrome
//{
//    public class BrowserAction : IActionBase
//    {
//        private static ILogger logger = Logger.OfContext();
//        public eGuiType GuiType => eGuiType.Chrome;


//        public IActionBase GetActionBase(BrowserParams browserParams)
//        {
//            var activeBase = RPAUtils.RetryWait(() => BrowserContextProvider.Instance.GetBrowserAction(browserParams), (uint)browserParams.timeout, 200);
//            logger.LogDebugLazy(() => $"activeBase == null:{activeBase == null}");
//            if (activeBase == null)
//            {
//                throw new RPAException(this.GuiType, eErrorMessageType.Error_ChromeExtensionUnopen);
//            }
//            return activeBase;
//        }

//        public string Acquire(string value, string mode = "title", int index = 1, bool isActive = true, bool isMatchAll = false, string browserType = "")
//        {
//            throw new NotImplementedException();
//        }

//        public void Active()
//        {
//            throw new NotImplementedException();
//        }

//        public void CaptureScreenshot(bool saveToClp, int cutRange, string savePath, bool cover, int height)
//        {
//            throw new NotImplementedException();
//        }

//        public void Close()
//        {
//            throw new NotImplementedException();
//        }

//        public void CloseAll(bool killProcess, int timeout = 10000)
//        {
//            throw new NotImplementedException();
//        }

//        public string Create(string url, int timeout = 1000)
//        {
//            throw new NotImplementedException();
//        }

//        public string Download(string selector, string saveFile = "", string fileName = "", int timeout = 10000)
//        {
//            throw new NotImplementedException();
//        }

//        public string DownloadUrl(string downLoadUrl, string saveFile = "", int timeout = 10000)
//        {
//            throw new NotImplementedException();
//        }

//        public string ElementExecutejs(string selector, string scriptCode, string @params)
//        {
//            throw new NotImplementedException();
//        }

//        public string Executejs(string title, string url, string scriptCode, string @params)
//        {
//            throw new NotImplementedException();
//        }


//        public string GetCookies(string url, string name = null)
//        {
//            throw new NotImplementedException();
//        }

//        public string GetDocument()
//        {
//            throw new NotImplementedException();
//        }

//        public string GetScroll()
//        {
//            throw new NotImplementedException();
//        }

//        public string GetTabDetail(string value = null)
//        {
//            throw new NotImplementedException();
//        }

//        public string GetTitle()
//        {
//            throw new NotImplementedException();
//        }

//        public string GetUrl()
//        {
//            throw new NotImplementedException();
//        }

//        public void GoBack()
//        {
//            throw new NotImplementedException();
//        }

//        public void GoForward()
//        {
//            throw new NotImplementedException();
//        }

//        public bool IsLoaded()
//        {
//            throw new NotImplementedException();
//        }

//        public void Maximized()
//        {
//            throw new NotImplementedException();
//        }

//        public void Minimized()
//        {
//            throw new NotImplementedException();
//        }

//        public string Navigate(string url, int timeout = 1000)
//        {
//            throw new NotImplementedException();
//        }

//        public void ReLoad()
//        {
//            throw new NotImplementedException();
//        }

//        public void RemoveCookie(string url, string name = "")
//        {
//            throw new NotImplementedException();
//        }

//        public void Screenshot(string savePath = "")
//        {
//            throw new NotImplementedException();
//        }

//        public void Scroll(string value)
//        {
//            throw new NotImplementedException();
//        }

//        public void SetCookie(string url, string value)
//        {
//            throw new NotImplementedException();
//        }

//        public string StartCapture(string jsPath, string config = null, string url = null)
//        {
//            throw new NotImplementedException();
//        }

//        public string StartConfig(string jsPath, string config = null, string url = null)
//        {
//            throw new NotImplementedException();
//        }

//        public void Stop()
//        {
//            throw new NotImplementedException();
//        }

//        public bool Wait(int timeout = 10000)
//        {
//            throw new NotImplementedException();
//        }
//    }
//}
