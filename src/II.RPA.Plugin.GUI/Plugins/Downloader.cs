using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using II.RPA.Plugin.GUI.Params;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Commons.WindowMatch;
using II.RPA.WebBrowser.Common.Utils;
using System.Management;
using System.Text.RegularExpressions;
using System.Windows.Interop;

namespace II.RPA.Plugin.GUI.Plugins
{
    public class Downloader : GuiComponentBase<ParamDownloaderModel>
    {
        private static ILogger logger = Logger.OfContext();

        protected override eComponentType ComponentType => eComponentType.Downloader;

        protected override JMessage Execute(ParamDownloaderModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
        {
            JMessage msg = new JMessage();
            try
            {
                GuiElement element = null;
                try
                {
                    Dictionary<string, object> hashtable = new Dictionary<string, object>{
                        {"processId",paramModel.processId }
                    };
                    var param = new GetElementFromSelectorParams
                    {
                        Selector = paramModel.Selector,
                        SelectorType = paramModel.SelectorType,
                        ExtendParam = hashtable,
                        MatchType = paramModel.MatchType,
                        RunParam = paramModel.RunParam,
                        WindowInfo = paramModel.WindowInfo
                        //TabId = paramModel.TabId  //最终会根据元素成功新匹配到一个TabID 
                    };
                    element = GuiElement.FromSelector(param);
                }
                catch (Exception ex)
                {
                    Exception(ex, eErrorMessageType.Error_NotFoundElement, ref msg);
                    return msg;
                }
                try
                {
                    if (element == null)
                    {
                        throw new RPAException(eErrorMessageType.Error_NotFoundElement);
                    }
                    if (paramModel.IsRandomLocationClick)
                    {
                        logger.LogInformation("Downloader：启用了模拟人工随机位置点击");
                        paramModel.inputMode = 1;//模拟输入
                    }
                    long operationTimeout = 0;
                    operationTimeout = paramModel.RunParam.TotalTime + paramModel.timeout;
                    logger.LogInformation($"回传coreserver超时时间:{operationTimeout}ms.");
                    NotifyCoreServerElementFound(paramModel.RunUniqueID, operationTimeout);

                    var downloaderParam = new Commons.Params.ParamDownloaderModel()
                    {
                        saveFile = paramModel.saveFile,
                        fileName = paramModel.fileName,
                        saveMode = paramModel.saveMode,
                        clickMode = paramModel.clickMode,
                        inputMode = paramModel.inputMode,
                        interval = paramModel.interval,
                        timeout = paramModel.timeout,
                        waitDialogTime = paramModel.waitDialogTime
                    };

                    if (paramModel.SelectorType.Equals(eGuiType.CV.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        // CV元素下载：点击→处理保存对话框→文件系统监控下载完成
                        msg.Info = CvElementDownload(element, downloaderParam);
                    }
                    else
                    {
                        msg.Info = element.ElementDownloader(downloaderParam);
                    }
                }
                catch (RPAException rpaEx)
                {
                    Exception(rpaEx, rpaEx.ErrorMessageType, ref msg);
                    msg.AllowRetry = ((int)AllowRetryType.NotAllow).ToString();
                }
                return msg;
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_DownloadFail, ref msg);
                msg.AllowRetry = ((int)AllowRetryType.NotAllow).ToString();
            }
            return msg;
        }

        /// <summary>
        /// CV元素下载：通过CV坐标点击元素，然后以Windows级别处理保存对话框，
        /// 最后通过文件系统监控等待下载完成。
        /// </summary>
        private string CvElementDownload(GuiElement element, Commons.Params.ParamDownloaderModel param)
        {
            // 1. 确保保存目录存在
            if (!string.IsNullOrEmpty(param.saveFile) && !Directory.Exists(param.saveFile))
            {
                Directory.CreateDirectory(param.saveFile);
            }

            // 2. 通过CV元素坐标触发点击
            var clickParams = new UiElementClickParams();
            clickParams.operateType = param.clickMode;
            logger.LogInformation("CV下载：通过CV元素坐标触发点击");
            element.uiNode.MouseClick(0, 0, clickParams);

            // 3. 等待浏览器保存对话框
            var desktopWindow = new DesktopWindow();
            var stopWatch = Stopwatch.StartNew();
            IntPtr dialogHandle = IntPtr.Zero;

            while (stopWatch.ElapsedMilliseconds <= param.waitDialogTime)
            {
                var dialogs = desktopWindow.FindAllSavaDialogWindow(wnd => wnd.IsVisibleEnabled);
                var dialog = dialogs.FirstOrDefault();
                if (dialog != null)
                {
                    dialogHandle = dialog.Handle;
                    break;
                }
                Task.Delay(20).Wait();
            }
            stopWatch.Stop();

            if (dialogHandle == IntPtr.Zero)
            {
                throw new RPAException(eErrorMessageType.Error_DownloadFail,
                    "下载文件组件通过CV元素执行时，未检测到浏览器保存对话框。请确认浏览器下载设置中已开启「下载前询问每个文件的保存位置」。");
            }

            logger.LogInformation("CV下载：检测到保存对话框，正在处理...");

            // 4. 处理保存对话框（填充路径、点击保存）
            string path = DialogHelper.SaveDialogWindow(
                dialogHandle,
                param.saveFile,
                param.fileName,
                param.interval,
                param.InputMode,
                NativeMethods.SAVE_BUTTON_ID,
                param.SaveMode);

            logger.LogInformation($"CV下载：文件将保存至 {path}，等待下载完成...");

            // 5. 通过文件系统监控等待下载完成（文件出现且大小稳定）
            stopWatch.Restart();
            while (stopWatch.ElapsedMilliseconds <= param.timeout)
            {
                if (File.Exists(path))
                {
                    try
                    {
                        long size1 = new FileInfo(path).Length;
                        Thread.Sleep(1000);
                        if (File.Exists(path))
                        {
                            long size2 = new FileInfo(path).Length;
                            if (size1 == size2 && size1 > 0)
                            {
                                logger.LogInformation($"CV下载：文件下载完成，大小 {size2} 字节");
                                return path.Replace("\\", "\\\\");
                            }
                        }
                    }
                    catch (IOException)
                    {
                        // 文件仍在写入中，继续等待
                    }
                }
                Thread.Sleep(500);
            }
            stopWatch.Stop();

            throw new RPAException(eErrorMessageType.Error_DownloadTimeOut);
        }

        protected override void ValidateParameter(ParamDownloaderModel paramModel) {
            try
            {
                if (string.IsNullOrEmpty(paramModel.Selector))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError);
                }

                return;
            }
            catch (RPAException rpaEx)
            {
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, $"下载 参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }
    }
}

