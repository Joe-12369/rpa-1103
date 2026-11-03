using II.RPA.Plugin.Core;
using II.RPA.WebBrowser.Common;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;

namespace II.RPA.Plugin.WebBrowser
{
    public class BrowserAction
    {
        private AbActionBase _actionBase;
        public static BrowserAction browserAction;
        private static ILogger logger = Logger.OfContext();

        eGuiType GuiType { get; }
        public BrowserAction(AbActionBase actionBase)
        {
            _actionBase = actionBase;
        }
        //public static BrowserAction GetActionBase(BrowserParams browserParams)
        //{
        //    var activeBase = WebBrowserManager.Info.ActionBase;
        //    logger.LogDebug($"GetActionBase: {activeBase.Count}");
        //    foreach (var action in activeBase)
        //    {
        //        if (action.GuiType.ToString().Equals(browserParams.browserType, StringComparison.OrdinalIgnoreCase))
        //        {
        //            logger.LogDebug($"找到GuiOprator:{action.GuiType}");
        //            var browser = action.GetActionBase(browserParams);
        //            return new BrowserAction(browser);
        //        }
        //    }
        //    return null;
        //}
        public string StartCapture(string jsPath, string config = null, string url = "www.baidu.com")
        {
            return _actionBase.StartCapture(jsPath, config, url);
        }
        public string StartConfig(string jsPath, string config = null, string url = "www.baidu.com")
        {
            return _actionBase.StartConfig(jsPath, config, url);
        }
        public string ElementExecutejs(string selector, string scriptCode, string @params)
        {
            return _actionBase.ElementExecutejs(selector, scriptCode, @params);
        }
        public bool IsLoaded()
        {
            _actionBase.Active();
            return _actionBase.IsLoaded();
        }
        public string GetTabDetail(string value = null)
        {
            _actionBase.Active();
            return _actionBase.GetTabDetail(value);
        }
        public string GetScroll()
        {
            _actionBase.Active();
            return _actionBase.GetScroll();
        }
        public void Scroll(string value)
        {
            _actionBase.Active();
            _actionBase.Scroll(value);
        }
        public string GetCookie(string url, string name)
        {
            _actionBase.Active();
            return _actionBase.GetCookies(url, name);
        }
        public string GetCookies(string url, string name = null)
        {
            _actionBase.Active();
            return _actionBase.GetCookies(url);
        }

        public void SetCookie(string url, string value)
        {
            _actionBase.Active();
            _actionBase.SetCookie(url, value);
        }
        public void Minimized()
        {
            _actionBase.Active();
            _actionBase.Minimized();
        }
        public void Maximized()
        {
            _actionBase.Active();
            _actionBase.Maximized();
        }
        public string Create(string url, string browserType = "chrome", bool isWaitingload = true, int timeout = 1000)
        {
            var browserParams = new BrowserParams { };
            return _actionBase.Create(url, isWaitingload, timeout);
        }
        public string Acquire(string value, string mode = "title", int index = 1, string browserType = "chrome", bool isActive = true)
        {
            return _actionBase.Acquire(value, mode, index, isActive);
        }
        public string Navigate(string url, int timeout = 1000)
        {
            _actionBase.Active();
            return _actionBase.Navigate(url, timeout);
        }
        public void Close()
        {
            _actionBase.Active();
            _actionBase.Close();
        }
        public void CloseAll()
        {
            _actionBase.Active();
            _actionBase.CloseAll();
        }
        public void Active()
        {
            _actionBase.Active();
        }
        public string Download(string selector, string saveFile = "", string fileName = "", int timeout = 10000)
        {
            _actionBase.Active();
            return _actionBase.Download(selector, saveFile, fileName, timeout);
        }
        public string DownloadUrl(string downLoadUrl, string saveFile = "", int timeout = 10000)
        {
            _actionBase.Active();
            return _actionBase.DownloadUrl(downLoadUrl, saveFile, timeout);
        }
        public void GoBack()
        {
            _actionBase.Active();
            _actionBase.GoBack();
        }
        public void GoForward()
        {
            _actionBase.Active();
            _actionBase.GoForward();
        }
        public void ReLoad()
        {
            _actionBase.Active();
            _actionBase.ReLoad();
        }
        public bool Wait(int timeout = 10000)
        {
            _actionBase.Active();
            return _actionBase.Wait(timeout);
        }
        public void Stop()
        {
            _actionBase.Active();
            _actionBase.Stop();
        }
        public void RemoveCookie(string url, string name = "")
        {
            _actionBase.Active();
            _actionBase.RemoveCookie(url, name);
        }
        public string GetUrl()
        {
            _actionBase.Active();
            return _actionBase.GetUrl();
        }
        public string GetTitle()
        {
            _actionBase.Active();
            return _actionBase.GetTitle();
        }
        public void Screenshot(string savePath = "")
        {
            _actionBase.Active();
            _actionBase.Screenshot(savePath);
        }
        public void CaptureScreenshot(bool saveToClp, int cutRange, string savePath, bool cover, int height)
        {
            _actionBase.Active();
            _actionBase.CaptureScreenshot(saveToClp, cutRange, savePath, cover, height);
        }
        public void StartInspectNetwork()
        {
            _actionBase.Active();
            _actionBase.StartInspectNetwork();
        }
        public string GetInspectNetwork(string type, string urlRegex, bool isRegex, string filePath)
        {
            _actionBase.Active();
            return _actionBase.GetInspectNetwork(type, urlRegex, isRegex, filePath);
        }
        public void DetachDebugger()
        {
            _actionBase.Active();
            _actionBase.DetachDebugger();
        }
        public string Executejs(string title, string url, string scriptCode, string @params)
        {
            _actionBase.Active();
            return _actionBase.Executejs(title, url, scriptCode, @params);
        }
        public string GetDocument()
        {
            _actionBase.Active();
            return _actionBase.GetDocument();
        }
        /// <summary>
        /// 重新判断需要关闭的网页tab存在否
        /// </summary>
        /// <param name="webData"></param>
        public void RetryCheckTab(string webData)
        {
            try
            {
                if (!((AbActionBase)_actionBase).IsExistTab())
                {
                    var webRet = JsonConvert.DeserializeObject<Dictionary<string, object>>(webData);
                    _actionBase.Acquire(webRet["title"].ToString());
                }
            }
            catch (Exception ex)
            {
                //兼容某种类型浏览器未做此要求
                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, "RetryCheckTab Error");
            }
        }
    }
}

