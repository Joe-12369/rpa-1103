//using II.RPA.WinAutomation.CommonUtility;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace II.RPA.WebBrowser.Common
//{
//    public abstract class AbActionBase : IActionBase
//    {
//        public abstract eGuiType GuiType { get; }

//        public abstract string Acquire(string value, string mode = "title", int index = 1, bool isActive = true, bool isMatchAll = false);
//        public abstract void Active();
//        public abstract void Close();
//        public abstract void CloseAll(bool killProcess);
//        public abstract string Create(string url, int timeout = 1000);
//        public abstract string Download(string selector, string saveFile = "", string fileName = "", int timeout = 10000);
//        public abstract string DownloadUrl(string downLoadUrl, string saveFile = "", int timeout = 10000);
//        public abstract string ElementExecutejs(string selector, string scriptCode, string @params);
//        public abstract string Executejs(string title, string url, string scriptCode, string @params);
//        public abstract AbActionBase GetActionBase(BrowserParams browserParams);
//        public abstract string GetCookies(string url, string name = null);
//        public abstract string GetDocument();
//        public abstract string GetScroll();
//        public abstract string GetTabDetail(string value = null);
//        public abstract string GetTitle();
//        public abstract string GetUrl();
//        public abstract void GoBack();
//        public abstract void GoForward();
//        public abstract bool IsLoaded();
//        public abstract void Maximized();
//        public abstract void Minimized();
//        public abstract string Navigate(string url, int timeout = 1000);
//        public abstract void ReLoad();
//        public abstract void RemoveCookie(string url, string name = "");
//        public abstract void Screenshot(string savePath = "");
//        public abstract void CaptureScreenshot(bool saveToClp, int cutRange, string savePath, bool cover, int height);
//        public abstract void Scroll(string value);
//        public abstract void SetCookie(string url, string value);
//        public abstract string StartCapture(string jsPath, string config = null, string url = null);
//        public abstract string StartConfig(string jsPath, string config = null, string url = null);
//        public abstract void Stop();
//        public abstract bool Wait(int timeout = 10000);
//        public abstract void StartInspectNetwork();
//        public abstract string GetInspectNetwork(string type, string url, bool isRegex, string filePath);
//        public abstract void DetachDebugger();
//        public virtual bool IsExistTab()
//        {
//            throw new NotImplementedException();
//        }

//        IActionBase IActionBase.GetActionBase(BrowserParams browserParams)
//        {
//            return null;
//        }
//        public virtual string Create(string url, bool isWaitingload, int timeout = 1000)
//        {
//            return Create(url, timeout);
//        }
//    }
//}
