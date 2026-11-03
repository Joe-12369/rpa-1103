using II.RPA.Plugin.Core;
using II.RPA.WinAutomation.CommonUtility;

namespace II.RPA.WebBrowser.Common
{
    public interface IActionBase
    {
        eGuiType GuiType { get; }
        IActionBase GetActionBase(BrowserParams browserParams);
        string StartConfig(string jsPath, string config = null, string url = null);
        string StartCapture(string jsPath, string config = null, string url = null);
        string ElementExecutejs(string selector, string scriptCode, string @params);
        bool IsLoaded();
        string GetTabDetail(string value = null);
        void Minimized();
        void Maximized();
        /// <summary>
        /// 
        /// </summary>
        /// <param name="direction">方向</param>
        /// <param name="value">位置</param>
        void Scroll(string value);
        string GetScroll();
        string Acquire(string value, string mode = "title", int index = 1, bool isActive = true, bool isMatchAll = false, string browserType = "");
        string Navigate(string url, int timeout = 1000);
        void Close(bool isCloseAlert);
        void CloseAll(bool killProcess, int timeout = 10000, bool isCloseAlert = false);
        void Active();
        string Download(string selector, string saveFile = "", string fileName = "", int timeout = 10000);
        string DownloadUrl(string downLoadUrl, string saveFile = "", int timeout = 10000);
        void GoBack();
        void GoForward();
        void ReLoad();
        bool Wait(int timeout = 10000);
        void Stop();
        string GetCookies(string url, string name = null);
        void SetCookie(string url, string value);
        void RemoveCookie(string url, string name = "");
        string GetUrl();
        string GetTitle();
        void Screenshot(string savePath = "");
        void CaptureScreenshot(bool saveToClp, int cutRange, string savePath, bool cover, int height);
        string Executejs(string title, string url, string scriptCode, string @params);
        string GetDocument();
    }
}
