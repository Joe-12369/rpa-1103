using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Params;
using II.RPA.WinAutomation.CommonUtility.Utils;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using II.RPA.Plugin.GUI.DataCollection.V2.Windows;
using II.RPA.Plugin.GUI.DataCollection.V2;
using II.RPA.Plugin.GUI.Commons.Params.DataCollect;
using II.RPA.Plugin.GUI.ScreenShot;
using II.RPA.Plugin.GUI.ProcessCreator.ElementTrack;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Drawing;
using FlaUI.Core.AutomationElements;
using II.RPA.Plugin.Core.Entities;
using Newtonsoft.Json.Linq;
using II.RPA.Plugin.GUI.Utils;
using FlaUI.Core.Tools;
using System.IO;
using SharpVectors.Dom;
using System.Windows.Interop;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.Core.UI;
using System.Collections.Concurrent;
using II.RPA.WebBrowser.Common.Basic;

namespace II.RPA.Plugin.GUI.Plugins.DataCollection
{
    public class ExtractWinndowInfo : ElementAbilityBase<ParamExtractWinndowInfoModel>
    {
        private static ILogger logger = Logger.OfContext();
        protected override eComponentType ComponentType => eComponentType.ScreenshotCapture;
        private readonly UIProvider m_UIProvider;
        public ExtractWinndowInfo()
        {
            m_UIProvider = new UIProvider();
        }

        protected override void ValidateParameter(ParamExtractWinndowInfoModel paramModel) {
            try
            {
                if (paramModel.windowType.EqualIgnoreCase("web"))
                {
                    var browserWndHandle = IntPtr.Zero;
                    if (string.IsNullOrEmpty(paramModel.guiType) || GlobleConfigurations.UseSZBrowser == true)
                    {
                        var windowInfo = BrowserWindowProvider.GetFirstBrowserByZOrder();
                        paramModel.guiType = windowInfo.WindowType.ToString();
                        browserWndHandle = windowInfo.Handle;
                        paramModel.WindowInfo = windowInfo;
                    }
                    else
                    {
                        eGuiType guiType = (eGuiType)Enum.Parse(typeof(eGuiType), paramModel.guiType.ToString(), true);
                        var windowInfo = BrowserWindowProvider.GetFirstBrowserByZOrder(null, guiType);
                        paramModel.guiType = windowInfo.WindowType.ToString();
                        browserWndHandle = windowInfo.Handle;
                        paramModel.WindowInfo = windowInfo;
                    }
                    paramModel.WndHandle = browserWndHandle;
                    logger.LogDebug($"GetZindexFirstBrowser：{paramModel.windowType}，{paramModel.guiType},{paramModel.WndHandle}");
                }
                else
                {
                    IntPtr hWnd = IntPtr.Zero;
                    //针对虚拟桌面启动后，获取激活窗口句柄为0的情况，做最多3000秒等待
                    RPAUtils.Retry(() =>
                    {
                        hWnd = ForegroundWinUtils.GetForegroundWindow();
                        return hWnd != IntPtr.Zero;
                    }, 3000, 200);
                    paramModel.guiType = "windows";
                    paramModel.WndHandle = hWnd;

                }
                return;
            }
            catch (Exception ex)
            {
                logger.LogInformation($"参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }
        protected override JMessage Execute(ParamExtractWinndowInfoModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
        {
            JMessage msg = new JMessage();
            var extractInfo = string.Empty;
            try
            {
                logger.LogDebug($"开始拾取：Capture.DataCollectCapture");
                FacMessageHelper.SendShowOrHiddenMainWndActionToFactory(false);
                bool userLast = false;
                ZbrainAPIService.NotifyGetWindowImg_ChatRpa(paramModel.WndHandle, "", ref userLast); ;
                extractInfo = this.ExtractInfo(paramModel);
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_ElementCaptureError, ref msg);
            }
            finally
            {
                msg.Info = new JObject() { { "value", extractInfo } }.ToString();
            }
            return msg;
        }

        private string ExtractInfo(ParamExtractWinndowInfoModel paramModel)
        {
            try
            {
                ShowScanWnd(paramModel.WndHandle);
                bool useNormalGuiType = paramModel.sourceType.EqualIgnoreCase("normal") || paramModel.sourceType.EqualIgnoreCase("webCombineCv");
                string opGuiType = useNormalGuiType ? paramModel.guiType : eGuiType.CV.ToString();
                var guiOperator =(IGuiOperator_6_9_0)GuiOperatorProvider.Instance.GetGuiOperator(opGuiType, paramModel.WindowInfo);
                string elesData = "";
                IGuiOperator_6_9_0 getAllData_GuiOperator = (IGuiOperator_6_9_0)guiOperator;
                if (paramModel.sourceType.EqualIgnoreCase("webCombineCv"))
                {
                    var data = ((IGuiWebOperator)guiOperator).GetViewElementsData(new Commons.GetElementsAllParams
                    {
                        WindowHandle = paramModel.WndHandle,
                        ClsNames = string.Empty,
                        TagNames = string.Empty,
                        AttrKeywords = string.Empty,
                        PrevCVSelector = string.Empty,
                        CVUseLastData = false,
                        SpecialOperation = string.Empty,
                        TabId = -1, //暂时使用激活页，口子预留
                    });
                    #region 输出测试图像
                    //var factor = GetFactor();
                    //if (factor == 0)
                    //{
                    //    factor = 1.0;
                    //}

                    //NativeMethods.GetWindowRect(paramModel.WndHandle, out var rootWndRect);
                    //rootRect = new Rectangle(rootWndRect.Left, rootWndRect.Top, rootWndRect.Right - rootWndRect.Left, rootWndRect.Bottom - rootWndRect.Top);

                    //using (Bitmap b = new Bitmap(rootRect.Width, rootRect.Height))
                    //{
                    //    //rootRect = rootRect.Downscale(factor);

                    //    var nb = ImageHelper.Resize(b, rootRect.Width, rootRect.Height);
                    //    using (Graphics g = Graphics.FromImage(b))
                    //    {
                    //        System.Drawing.Pen pen = new System.Drawing.Pen(System.Drawing.Color.Blue, 2);
                    //        g.CopyFromScreen(rootRect.X, rootRect.Y, 0, 0, rootRect.Size);
                    //        g.DrawRectangle(pen, 0, 0, rootRect.Width, rootRect.Height);
                    //        SetVisiualRect(data, factor, g);
                    //        b.Save($@"D:\test\{DateTime.Now.Ticks}.png");//保存图片
                    //    }
                    //}


                    #endregion
                    elesData = data.ToJsonNoneFormating();
                    logger.LogDebug($"GetViewElementsData：{elesData}");
                }
                string retExtractWinndowInfo = ZbrainAPIService.ExtractWinndowInfo(paramModel.WndHandle, paramModel.query, elesData);

                return retExtractWinndowInfo;
            }
            catch (Exception ex)
            {
                logger.LogError($"获取所有元素异常,{ex}");
                throw ex;
            }
            finally
            {

                m_UIProvider?.CloseScanAnimationWnd();
            }
        }
        private void ShowScanWnd(IntPtr wndHandle)
        {
            var windowRect = RectHelper.GetWindowRect(wndHandle);
            m_UIProvider?.CloseScanAnimationWnd();
            logger.LogDebug($"开始构建弹出扫描动画界面");
            m_UIProvider.ShowScanAnimationWnd(windowRect.X, windowRect.Y, windowRect.Width, windowRect.Height,
                (sender, e) =>
                {
                    logger.LogDebug($"关闭扫描动画界面");
                    m_UIProvider?.CloseScanAnimationWnd();
                });

        }
    }
}

