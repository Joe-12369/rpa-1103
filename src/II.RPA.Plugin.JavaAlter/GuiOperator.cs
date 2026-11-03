using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Commons.Params;
using II.RPA.Plugin.JavaAlter.Agent;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;

namespace II.RPA.Plugin.JavaAlter
{
    public class GuiOperator : IGuiOperator_6_9_0, IGuiOperator
    {
        private static ILogger logger = Logger.OfContext();
        const int SUCCESS_STATUS = 0;
        const int FAIL_STATUS = 1;

        public GuiOperator()
        {

        }
        int IGuiOperator.Sort { get; set; }

        eGuiType IGuiOperator.GuiType => eGuiType.Swing;

        IGuiNode IGuiOperator.GetElementFromSelector(GetElementFromSelectorParams args)
        {
            var rpaCommon = JsonConvert.DeserializeObject<RPACommon>(args.Selector);
            var proxy = JavaAgent.Instance.GetJavaProxy(rpaCommon.Selector);
            if (proxy == null)
            {
                return null;
            }

            var element = JavaAgent.Instance.GetElementFromSelectorV2(rpaCommon.Selector, proxy);
            if (element != null)
            {
                element.ProcessId = proxy.TargetProcessId;
                return new JavaNode()
                {
                    ClassName = element.MappedClazz,
                    ControlType = element.ControlType,
                    BoundingRectangle = element.Rectangle,
                    JavaElement = element
                };
            }
            return null;
        }

        string IGuiOperator.GetElementFromSelectorAll(GetElementFromSelectorParams args)
        {
            JMessage msg = new JMessage();
            msg.code = SUCCESS_STATUS;
            var rpaCommon = JsonConvert.DeserializeObject<RPACommon>(args.Selector);
            bool isV2 = rpaCommon.CommonProps.Flag == "2.0";

            JavaElement javaElement = null;
            try
            {
                var proxy = JavaAgent.Instance.GetJavaProxy(rpaCommon.Selector);
                if (proxy != null)
                {
                    javaElement = JavaAgent.Instance.GetElementFromSelectorV2(rpaCommon.Selector, proxy);
                }
            }
            catch (RPAException e)
            {
                e.ComponentType = eComponentType.IsElementExist;
                msg.code = FAIL_STATUS;
                msg.message = e.ErrorMessage;
                return GetEncodingStr(e);
            }
            catch (Exception e)
            {
                var rpaException = new RPAException(eErrorMessageType.Error_ElementIsEnable, eComponentType.IsElementExist, e);
                return GetEncodingStr(rpaException);
            }
            msg.info = javaElement == null ? "0" : "1";
            if (javaElement != null)
            {
                Rectangle rec = javaElement.Rectangle;
                msg.info += string.Format("({0}:{1}:{2}:{3})", rec.X, rec.Y, rec.Width, rec.Height);
            }
            var str = JsonHelper.JsonByNewtonJson(msg);
            return GetEncodingStr(str);
        }

        bool IGuiOperator.InspectNode(Point screenPoint, AutomationElement rootElement, AutomationElement pointElement, string selector, out IGuiNode inspectNode)
        {
            try
            {
                logger.LogDebug($"JavaAlter InspectNode Start... ");
                JavaElement element = null;
                inspectNode = null;
                element = JavaAgent.Instance.FindElementByPointV2(pointElement.Properties.ProcessId, pointElement.Properties.NativeWindowHandle.ValueOrDefault, screenPoint);

                if (element != null)
                {
                    inspectNode = new JavaNode()
                    {
                        ClassName = element.ClassName,
                        ControlType = element.ClassName,
                        BoundingRectangle = element.Rectangle,
                        JavaElement = element
                    };
                }
                logger.LogInformation($"JavaAlter InspectNode End,Found Element:{inspectNode.ControlType},{element.Tech}");
                return element != null;
            }
            catch (Exception ex)
            {
                logger.LogError($"JavaAlter InspectNode:{ex.Message}");
            }
            inspectNode = null;
            return false;
        }

        bool IGuiOperator.IsCanUse(Point screenPoint, AutomationElement rootElement, AutomationElement pointElement)
        {

            logger.LogDebug($"Start IsCanUse from JavaAlter ");
            var isJavaWin = false;
            try
            {
                if (pointElement.Properties.NativeWindowHandle.IsSupported)
                {
                    var windowHandle = pointElement.Properties.NativeWindowHandle.ValueOrDefault;

                    if (IsJavaWindowInternal(rootElement))
                    {
                        if (JavaAgent.Instance.JavaProxyes.ContainsKey(pointElement.Properties.ProcessId))
                        {
                            logger.LogInformation($"detect java process: {pointElement.Properties.ProcessId}");
                            isJavaWin = true;
                        }
                        else
                        {
                            if (JavaAgent.Instance.CrateJavaProxy(pointElement.Properties.ProcessId))
                            {
                                logger.LogInformation($"init java service: {pointElement.Properties.ProcessId}");
                                return true;
                            }
                            logger.LogDebug($"Java Agent Init Error");
                            throw new JavaException(eErrorMessageType.Error_UNKOWN);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
            }
            finally
            {
                logger.LogDebug($"End IsCanUse JavaAlter ");
            }
            return isJavaWin;
        }

        void IGuiOperator.ReadyToCapture()
        {

        }

        void IGuiOperator.Release()
        {

        }
        private bool IsJavaWindowInternal(AutomationElement aRootElement)
        {
            string className = aRootElement.ClassName;
            logger.LogDebug($"IsJavaWindowInternal:{className}");
            switch (className)
            {
                case "YonyouUWnd":
                case "SunAwtCanvas":
                case "SunAwtFrame":
                case "SunAwtDialog":
                case "SunAwtWindow":
                    return true;
                default:
                    return false;
            }
        }
        private string GetEncodingStr(RPAException _rpa_ex)
        {
            JMessage msg = new JMessage();
            msg.code = FAIL_STATUS;
            msg.message = _rpa_ex.ErrorMessage;
            var encodeStr = JsonHelper.JsonByNewtonJson(msg);
            return GetEncodingStr(encodeStr);
        }
        private string GetEncodingStr(string txt)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(txt);
            return Encoding.UTF8.GetString(bytes);
        }

        public string GetElementsAll(GetElementsAllParams @params)
        {
            throw new NotImplementedException();
        }

        public string BuildSelectors(BulidSelectorsParams @params)
        {
            throw new NotImplementedException();
        }

        public IGuiNode GetElementById(GetElementByIdParams args)
        {
            throw new NotImplementedException();
        }

        public List<List<string>> CollectData_v2(ParamDataCollect_V2Model paramModel)
        {
            throw new NotImplementedException();
        }

        public List<SelectorProps> GetSameParentsProps_v2(ParamDataCollect_V2Model paramModel)
        {
            throw new NotImplementedException();
        }

        public List<JObject> GetSimilarOrDataCollectElements(GetElementsAllParams @params)
        {
            throw new NotImplementedException();
        }

        public IGuiNode[] QuerySelectorAll(GetElementFromSelectorParams args)
        {
            throw new NotImplementedException();
        }

        public bool VerifyWindowOfNode(GetElementFromSelectorParams args)
        {
            throw new NotImplementedException();
        }

        public string GetTargetElementInfo(GetTargetElementInfoModelParams @params)
        {
            throw new NotImplementedException();
        }

        public string BuildSelectors(ParamBuildSelectorsModel paramModel)
        {
            throw new NotImplementedException();
        }

        public List<SelectorProps> GetSameParentsProps(ParamCollectDataBySelectorModel paramModel)
        {
            throw new NotImplementedException();
        }

        public List<List<string>> CollectDataBySelector(ParamCollectDataBySelectorModel paramModel)
        {
            throw new NotImplementedException();
        }

        public void ScrollIntoView(string srcoll)
        {
            try
            {
                var scrollParams = JsonConvert.DeserializeObject<Dictionary<string, object>>(srcoll);
                if (scrollParams == null) return;
                int scrollTop = scrollParams.ContainsKey("scrollTop") ? Convert.ToInt32(scrollParams["scrollTop"]) : 0;
                int scrollLeft = scrollParams.ContainsKey("scrollLeft") ? Convert.ToInt32(scrollParams["scrollLeft"]) : 0;
                int delay = scrollParams.ContainsKey("ScrollVerticalDelay") ? Convert.ToInt32(scrollParams["ScrollVerticalDelay"]) : 0;

                var handle = NativeMethods.GetForegroundWindow();
                var windowElement = GUI.Commons.UiCommon.UIAAutomation.FromHandle(handle);
                if (windowElement == null) return;
                var windowRect = windowElement.BoundingRectangle;

                var centerX = windowRect.Left + windowRect.Width / 2;
                var centerY = windowRect.Top + windowRect.Height / 2;
                Mouse.Position = new Point(centerX, centerY);
                Thread.Sleep(50);

                if (scrollTop != 0)
                {
                    int steps = Math.Abs(scrollTop);
                    int direction = Math.Sign(scrollTop);
                    for (int i = 0; i < steps; i++)
                    {
                        Mouse.Scroll(direction);
                        Thread.Sleep(delay > 0 ? delay : 30);
                    }
                }

                if (scrollLeft != 0)
                {
                    int steps = Math.Abs(scrollLeft);
                    int direction = Math.Sign(scrollLeft);
                    for (int i = 0; i < steps; i++)
                    {
                        Mouse.HorizontalScroll(direction);
                        Thread.Sleep(delay > 0 ? delay : 30);
                    }
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, "JavaAlter ScrollIntoView failed");
            }
        }

        public string GetBoundings(string jsonParams)
        {
            throw new NotImplementedException();
        }

        public string BuildSelectors(string jsonParams)
        {
            throw new NotImplementedException();
        }
    }
}

