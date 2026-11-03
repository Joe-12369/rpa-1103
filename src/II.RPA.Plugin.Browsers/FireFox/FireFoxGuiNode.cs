using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WebBrowser.Common.Basic.Params;
using II.RPA.WebBrowser.Common.Utils;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace II.RPA.Plugin.Browsers.FireFox
{
    public class FireFoxGuiNode : ElementGuiNode
    {
        public FireFoxGuiNode(PageElement pageElement) : base(pageElement)
        {

        }
        /// <summary>
        /// add "undefined;" to last line.
        /// https://stackoverflow.com/questions/44567525/inject-scripts-error-script-returned-non-structured-clonable-data-on-firefox-ex
        /// </summary>
        /// <param name="scriptCode"></param>
        /// <param name="params"></param>
        /// <returns></returns>
        /// <exception cref="RPAException"></exception>
        //public override string Executejs(string scriptCode, string @params, string executionEnvironment = "", bool isAsyncCode = false, int waitResultTimeout = 0)
        //{
        //    var ts = DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0);
        //    long times = Convert.ToInt64(ts.TotalMilliseconds);
        //    var code = $"window.scriptCode{times}={scriptCode};undefined;";
        //    this.PageElement.Executejs(code);
        //    System.Threading.Thread.Sleep(100);
        //    if (string.IsNullOrEmpty(@params) || @params == "[]")
        //    {
        //        return this.PageElement.Executejs(string.Format($"window.scriptCode{times}()"));
        //    }
        //    else
        //    {
        //        JArray param = null;
        //        try
        //        {
        //            param = JArray.Parse(@params);
        //        }
        //        catch (Exception)
        //        {
        //            throw new RPAException(this.GuiType, eErrorMessageType.Error_ParamsError);
        //        }
        //        int index = param.Count;

        //        string paras = string.Empty;
        //        foreach (var item in param)
        //        {
        //            switch (item.Type)
        //            {
        //                case JTokenType.String:
        //                    paras += "'" + item.ToString() + "'";
        //                    break;
        //                case JTokenType.Integer:
        //                    paras += Convert.ToInt32(item);
        //                    break;
        //                case JTokenType.Float:
        //                    paras += Convert.ToDouble(item);
        //                    break;
        //                case JTokenType.Boolean:
        //                    paras += Convert.ToBoolean(item);
        //                    break;
        //                default:
        //                    throw new RPAException(this.GuiType, eErrorMessageType.Error_ParamsError);
        //            }

        //            index--;
        //            if (index > 0)
        //                paras += ",";
        //        }
        //        StringBuilder stringBuilder = new StringBuilder(256);
        //        stringBuilder.Append($"window.scriptCode{times}({paras})");
        //        return PageElement.Executejs(stringBuilder.ToString());
        //    }
        //}
        public override string ElementDownload(string saveFile = "", string fileName = "", int timeout = 10000)
        {
            this.ScrollIntoViewIfNeeded();
            var rect = this.BuildElementBounding();
            Mouse.LeftClick(new Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2));
            FindFireFoxDownload();
            string path = string.Empty;
            #region [等待另存为窗口出现]
            if (DownloadFileUtils.FindSaveAsFrom("请输入要保存的文件名…"))
            {
                if (!DownloadFileUtils.SaveAs(saveFile, "请输入要保存的文件名…", ref fileName))
                {
                    Thread.Sleep(200);
                    DownloadFileUtils.SaveAs(saveFile, "请输入要保存的文件名…".I18n("FireFoxGuiNode_EE03820"), ref fileName);
                }
                if (DownloadFileUtils.FindSaveAsFrom("确认另存为"))
                {
                    DownloadFileUtils.NotarizeSaveAs();
                    path = Path.Combine(saveFile, fileName);
                }
                path = Path.Combine(saveFile, fileName);
            }
            #endregion

            #region [等待下载]
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            try
            {
                JArray traceItem = null;
                var model = new ParamDownloadModel();
                while (stopWatch.ElapsedMilliseconds <= timeout)
                {
                    try
                    {
                        if (traceItem == null)
                            traceItem = this.PageElement.TabPage.Downloading();
                        var array = JArray.Parse(traceItem.ToString());
                        int id = 0;
                        int.TryParse(traceItem[0].Value<string>("id"), out id);
                        model.id = id;
                        var results = this.PageElement.TabPage.DownLoadSearch(model);
                        foreach (var item in results)
                        {
                            if (traceItem[0].Value<string>("id") == item.Value<string>("id").ToString())
                            {
                                if (!string.IsNullOrEmpty(path))
                                {
                                    //SelectedReserver();
                                    if (item.Value<bool>("exists"))
                                    {
                                        if (path != item.Value<string>("filename").ToString())
                                            File.Move(item.Value<string>("filename").ToString(), path);
                                        stopWatch.Stop();
                                        return path.Replace("\\", "\\\\");
                                    }
                                }
                                else
                                {
                                    if (string.IsNullOrEmpty(fileName))
                                        path = Path.Combine(saveFile, Path.GetFileName(traceItem[0].Value<string>("filename")));
                                    else
                                        path = Path.Combine(saveFile, fileName);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError("下载信息获取异常", ex);
                    }
                }
                stopWatch.Stop();
            }
            catch (Exception e)
            {
                throw new FireFoxException(eErrorMessageType.Error_DownloadError, e);
            }
            #endregion
            throw new FireFoxException(eErrorMessageType.Error_DownloadTimeOut);
        }
        private bool FindFireFoxDownload()
        {

            try
            {
                for (int i = 0; i < 5; i++)
                {
                    Thread.Sleep(500);
                    IntPtr dialogHandle = NativeMethods.FindWindowEx(new IntPtr(), new IntPtr(), "MozillaDialogClass", null);
                    if (dialogHandle != IntPtr.Zero)
                    {
                        var dialogFrom = UiCommon.UIAAutomation.FromHandle(dialogHandle);
                        if (dialogFrom != null)
                        {
                            List<ConditionBase> button = new List<ConditionBase>();
                            /*******插件化拆分时  调整uia3Automation 对象获取的来源*********/
                            //button.Add(new PropertyCondition(uia3Automation.PropertyLibrary.Element.Name, "保留"));
                            button.Add(new PropertyCondition(UiCommon.UIAAutomation.PropertyLibrary.Element.Name, "保存文件"));
                            AndCondition ad = new AndCondition(button);
                            for (int j = 0; j < 10; j++)
                            {
                                var btn = dialogFrom.FindAll(TreeScope.Descendants, ad).FirstOrDefault();
                                if (btn.Patterns.LegacyIAccessible.Pattern.State.Value != (AccessibilityState.STATE_SYSTEM_DEFAULT | AccessibilityState.STATE_SYSTEM_UNAVAILABLE))
                                {
                                    Thread.Sleep(200);
                                    var rect = btn.BoundingRectangle;
                                    Mouse.LeftClick(new Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2));
                                    break;
                                }
                                else
                                {
                                    UiCommon.SetForegroundWindow(dialogHandle);
                                }
                            }

                        }
                    }
                }

            }
            catch (Exception)
            {
            }
            return true;
        }
    }
}
