using FlaUI.Core.AutomationElements;
using II.RPA.Logging;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.WinAutomation.CommonUtility.Utils;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using II.RPA.Plugin.GUI.ScreenShot;
using II.RPA.Plugin.GUI.ProcessCreator.ElementTrack;
using System.Windows.Media.Imaging;
using II.RPA.Plugin.Core;
using Newtonsoft.Json;
using FlaUI.Core.Tools;
using II.RPA.Plugin.GUI.Params;
using Newtonsoft.Json.Linq;
using System.Windows.Interop;
using System.IO;

namespace II.RPA.Plugin.GUI.Utils
{
    internal class ElementProvider
    {
        private readonly static Lazy<ElementProvider> _instance = new Lazy<ElementProvider>(() => new ElementProvider());

        private ILogger logger = Logger.OfContext();
        public static ElementProvider Instance => _instance.Value;
        private ElementProvider()
        {
        }
        #region 截图拾取元素

        public async Task StartScreenshotCapture(ParamScreenshotCaptureModel paramModel)
        {
            await Task.Run(async () =>
            {
                JMessage msg = new JMessage();
                try
                {
                    Rectangle ret = this.ManualShot();
                    if (ret.IsEmpty)
                    {
                        msg.Code = 1;
                        msg.Message = eCommonMessageType.SDK_cs_YongHuQuXiao.GetMessage();
                        FacMessageHelper.SendScreenshotCaptureResultToFactory(msg);
                        return;
                    }
                    await Task.Delay(100);
                    GuiElement element = this.CaptureElement(ret.Center());
                    var nodeSelector = element.Selector;
                    this.SaveElementShotImg(element, paramModel.FileFullName);
                    if (!string.IsNullOrEmpty(nodeSelector))
                    {
                        var common = JsonConvert.DeserializeObject<RPACommon>(nodeSelector);
                        common.CommonProps.filePath = paramModel.FilePath;
                        common.CommonProps.ResDirPath = $@"res\{GlobleConfigurations.CaptureUniqueID}";
                        common.CommonProps.isSmartControl = "false";
                        var selector = common.ToJsonNoneFormating();
                        JObject retObj = new JObject() {
                            { "uiSelector",selector}
                        };
                        msg.Info = retObj.ToJsonNoneFormating();
                        FacMessageHelper.SendScreenshotCaptureResultToFactory(msg);
                        Task.Run(() =>
                        {
                            if (paramModel.MemoryInfo != null)
                                ZbrainAPIService.NotifyElementRecord(common.CommonProps.ProcessName, ret, paramModel.MemoryInfo);
                        }).ConfigureAwait(false).GetAwaiter();
                    }
                    else
                    {
                        throw new RPAException(eErrorMessageType.Error_ElementCaptureError);
                    }
                }
                catch (Exception ex)
                {
                    RPAException rpaEx;
                    if (!(ex is RPAException))
                    {
                        rpaEx = new RPAException(eErrorMessageType.Error_ElementCaptureError, ex);
                    }
                    else
                    {
                        rpaEx = (RPAException)ex;
                    }
                    msg.Code = rpaEx.ErrorCode;
                    msg.UnifyCode = rpaEx.UnifyErrorCode;
                    msg.Message = rpaEx.ErrorMessage;
                    FacMessageHelper.SendScreenshotCaptureResultToFactory(msg);
                }
            });
        }
        public async Task StopScreenshotCapture()
        {
            await Task.Run(() =>
            {
                JMessage msg = new JMessage();
                try
                {
                    Rectangle ret = this.StopShot();
                    msg.Code = 1;
                    msg.Message = eCommonMessageType.SDK_cs_YongHuQuXiao.GetMessage();
                    FacMessageHelper.SendScreenshotCaptureResultToFactory(msg);
                    return;
                }
                catch (Exception ex)
                {
                    RPAException rpaEx;
                    if (!(ex is RPAException))
                    {
                        rpaEx = new RPAException(eErrorMessageType.Error_ElementCaptureError, ex);
                    }
                    else
                    {
                        rpaEx = (RPAException)ex;
                    }
                    msg.Code = rpaEx.ErrorCode;
                    msg.UnifyCode = rpaEx.UnifyErrorCode;
                    msg.Message = rpaEx.ErrorMessage;
                    FacMessageHelper.SendScreenshotCaptureResultToFactory(msg);
                }
            });
        }
        #endregion

        #region 屏幕截图 获取区域范围
        private ScreenShoter m_ScreenShoter = null;
        private readonly object m_ScreenShoter_lock = new object();
        private readonly ConcurrencyItemContainer<System.Windows.Int32Rect> m_SnapResult = new ConcurrencyItemContainer<System.Windows.Int32Rect>();
        private void ScreenShoter_Snapped(object sender, FunctionEventArgs<System.Windows.Media.ImageSource> e)
        {
            if (e.IsDone)
            {
                CroppedBitmap image = e.Info as CroppedBitmap;
                m_SnapResult.SetData(image.SourceRect);
            }
            else
            {
                m_SnapResult.SetData(System.Windows.Int32Rect.Empty);
            }
        }
        private void InitScreenShoter()
        {
            if (m_ScreenShoter == null)
            {
                lock (this)
                {
                    if (m_ScreenShoter == null)
                    {
                        m_ScreenShoter = new ScreenShoter()
                        {
                            UsageScenario = UsageScenarioType.ScreenshotCapture,
                        };
                        //m_ScreenShoter.Snapped += ScreenShoter_Snapped;
                        m_ScreenShoter.Snapped += ScreenShoter_Snapped;
                    }
                }
            }
        }

        private Rectangle ManualShot()
        {
            InitScreenShoter();
            m_ScreenShoter.Start();
            m_SnapResult.WaitForData(-1);
            var rect = m_SnapResult.TakeData();
            if (rect == System.Windows.Int32Rect.Empty)
            {
                return Rectangle.Empty;
            }
            else
            {
                return new Rectangle(rect.X, rect.Y, rect.Width, rect.Height);
            }
        }
        private Rectangle StopShot()
        {
            m_ScreenShoter.Stop();
            return Rectangle.Empty;
            //m_SnapResult.WaitForData(-1);
            //var rect = m_SnapResult.TakeData();
            //if (rect == System.Windows.Int32Rect.Empty)
            //{
            //   return Rectangle.Empty;
            //}
            //else
            //{
            //    return new Rectangle(rect.X, rect.Y, rect.Width, rect.Height);
            //}
        }
        #endregion

        #region 根据屏幕坐标点，获取元素
        public GuiElement CaptureElement(Point screenPoint)
        {
            if (Configurations.Info.CvToNomalMoveMouse)
            {
                II.RPA.Plugin.GUI.Commons.MouseMotions.MouseMotionHelper.PersonMoveMouse(true, screenPoint);
            }
            IGuiNode inspectNode = null;
            var (rootElement, pointElement) = GetWindowElement(screenPoint);
            IGuiOperator guiOperator = GetGuiOperator(screenPoint, rootElement, pointElement);
            var inspectResult = guiOperator.InspectNode(screenPoint, rootElement, pointElement, "", out inspectNode);
            if (inspectResult)
            {
                var element = new GuiElement(inspectNode);
                SaveAiRepairData(element, rootElement);
                return element;
            }
            else
            {
                logger.LogDebug("查找元素 失败 赋值windows元素");
                IGuiNode_6_6_0 CGuiNode = (IGuiNode_6_6_0)(GuiManager.CreateWindowsGuiNode(pointElement));
                //实例化一个新的Windows UIANode 作为默认的元素，尽可能少的使用
                return new GuiElement(CGuiNode);
            }
        }

        private IGuiOperator GetGuiOperator(Point screenPoint, AutomationElement rootElement, AutomationElement pointElement)
        {
            if (GuiOperatorProvider.Instance.TryGetSupportedOperator(screenPoint, rootElement, pointElement, out var guiOperator))
            {
                return guiOperator;
            }
            return null;
        }

        private (AutomationElement rootElement, AutomationElement pointElement) GetWindowElement(Point point)
        {
            var hWndList = UiCommon.GetRootWindowList(point);
            AutomationElement rootElement = null;
            AutomationElement pointElement = null;
            if (hWndList.Count > 0)
            {
                foreach (IntPtr hWnd in hWndList)
                {
                    if (hWnd != IntPtr.Zero)
                    {
                        rootElement = UiCommon.UIAAutomation.FromHandle(hWnd);   //获取根窗体
                    }
                    if (rootElement == null)
                    {
                        break;
                    }
                    if (rootElement.Properties.BoundingRectangle.IsSupported && rootElement.BoundingRectangle.Contains(point))
                    {
                        break;
                    }
                }
                pointElement = UiCommon.UIAAutomation.FromHandle(hWndList.Last());
            }
            return (rootElement, pointElement);
        }
        #endregion

        #region 保存拾取元素的相关数据，供智能修复使用
        private void SaveAiRepairData(GuiElement guiElement, AutomationElement rootElement)
        {
            try
            {
                logger.LogDebug("SaveAiRepairData Start");
                Rectangle wndRect = Rectangle.Empty;
                Rectangle elementRect = guiElement.BoundingRectangle;
                if (elementRect.IsEmpty)
                {
                    return;
                }
                JObject jObj = new JObject();
                JObject elementDataObj = new JObject
                {
                    new JProperty("x", elementRect.X),
                    new JProperty("y", elementRect.Y),
                    new JProperty("width", elementRect.Width),
                    new JProperty("height", elementRect.Height)
                };
                jObj.Add(new JProperty(ConstFields.RES_SELECTOR_STATICDATA_ELEMENTDATA, elementDataObj));
                //保存更窗口selector逻辑，暂时不启用
                //string eleSelector = GuiManager.CreateGuiWindowsOperator().BuilderSelector(element);
                //jObj.Add(new JProperty(ConstFields.RES_SELECTOR_STATICDATA_WINDOWSSELECTOR, eleSelector));

                UiCommon.SaveStaticSelectorData(GlobleConfigurations.CaptureUniqueID, ConstFields.RES_SELECTOR_STATICFILE_NAME, jObj.ToString());

                Dictionary<string, object> wndInfo = new Dictionary<string, object>
                {
                    { "handle", (int)rootElement.Properties.NativeWindowHandle.Value },
                    { "area", new[] { wndRect.X, wndRect.Y, wndRect.Width, wndRect.Height } }
                };
                ZbrainAPIService.SendUIDToZBrain(wndInfo);
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
            }
            logger.LogDebug("SaveAiRepairData End");
        }
        #endregion
        #region 保存元素缩略图
        public void SaveElementShotImg(GuiElement guiElement, string filePath)
        {
            using (var currentDesktopScreenshot = UiCommon.UIAAutomation.GetDesktop().Capture())
            {
                using (guiElement.CaptureInformativeScreenshot(filePath, currentDesktopScreenshot))
                { }
            }
        }
        #endregion
    }
}
