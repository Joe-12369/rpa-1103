using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.Core.Threading;
using II.RPA.Plugin.GUI.Params;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using System.IO;

namespace II.RPA.Plugin.GUI.Plugins
{

    public class CaptureCVForOcrTable : OverlayCaptureAbilityBase<ParamCaptureCVForOcrTableModel>
    {
        private static ILogger logger = Logger.OfContext();

        protected override eComponentType ComponentType => eComponentType.OcrTableAnalyse;

        protected override void ValidateParameter(ParamCaptureCVForOcrTableModel paramModel) {
            try
            {
                logger.LogDebug("Start Capture");
                if (string.IsNullOrEmpty(paramModel.FilePath))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError, "缺少文件地址参数".I18n("controlType_CaptureCVForOcrTable_98CDB9F7"));
                }
                
                return;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, $"参数格式不符合要求 参数:{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }

        protected override JMessage Execute(ParamCaptureCVForOcrTableModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
        {
            logger.LogInformation($"CaptureCVForOcrTable Run:{paramModel.ToJson()} Version:1.2");
            return CaptureTableWithCaptureByRegion(paramModel);

        }


        #region 修改使用当前CaptureByRegion（获取区域文本）的方式进行OCR表格解析的拾取
        GuiElement elementCache = null;
        JMessage captureMessage = null;
        string globalSelector = null;
        Point mousePoint = Point.Empty;

        protected JMessage CaptureTableWithCaptureByRegion(ParamCaptureCVForOcrTableModel paramModel)
        {
            captureMessage = new JMessage();
            globalSelector = string.Empty;
            elementCache = null;
            mousePoint = Point.Empty;
            //CVScreenWnd wnd = null;
            CaptureCVForOcrTableWnd wnd = null;
            string strJsonContent = string.Empty;
            try
            {
                GuiElement.OnSelected = UIElement_OnSelected;
                GuiElement.OnCanceled = UiElement_OnCancel;
                GuiElement.StartCVRegionHighlight(paramModel.FilePath);
                GuiElement.overlayForm.Visible = false;
                GuiElement.overlayForm.inspectorStatus.SetWndShow();
                GuiElement.overlayForm.ShowCaptureDialog();
                Thread.Sleep(150);//避免截取到红色标记背景

                if (elementCache != null)
                {
                    logger.LogDebug($"存图路径:{paramModel.FilePath}");
                    wnd = new CaptureCVForOcrTableWnd(elementCache.BoundingRectangle, elementCache.currentInformativeScreenshot, mousePoint, paramModel.FilePath);
                    if (wnd.ShowDialog() == true)
                    {
                        Rectangle r = new Rectangle();
                        Point start = wnd.startPoint;
                        Point end = wnd.endPoint;

                        r.X = Math.Min(start.X, end.X);
                        r.Y = Math.Min(start.Y, end.Y);

                        r.Width = Math.Abs(end.X - start.X);
                        r.Height = Math.Abs(end.Y - start.Y);

                        RectangleEx _RectangleEx = new RectangleEx();
                        _RectangleEx.X = r.X;
                        _RectangleEx.Y = r.Y;
                        _RectangleEx.Width = r.Width;
                        _RectangleEx.Height = r.Height;

                        _RectangleEx.WindowsX = elementCache.BoundingRectangle.X;
                        _RectangleEx.WindowsY = elementCache.BoundingRectangle.Y;
                        _RectangleEx.WindowsWidth = elementCache.BoundingRectangle.Width;
                        _RectangleEx.WindowsHeight = elementCache.BoundingRectangle.Height;

                        //cvMessage.Info = _RectangleEx.ToJson();

                        JArray jarray = JArray.Parse(globalSelector);
                        var elementName = string.Empty;
                        var firstNode = jarray.First;
                        elementName = firstNode["name"]?.ToString();
                        var firstProps = jarray.First["props"];
                        if (string.IsNullOrEmpty(elementName))
                        {
                            foreach (var item in JArray.Parse(firstProps.ToString()))
                            {
                                if (item.Value<string>("name") == "name" || item.Value<string>("name") == "class")
                                {
                                    elementName = item.Value<string>("value").ToString();
                                    break;
                                }
                            }
                        }
                        globalSelector = ExtractViewData.AddCommonProps(jarray.ToString(), eGuiType.CV, "", elementName, filePath: paramModel.FilePath);
                        _RectangleEx.Selector = globalSelector;
                        strJsonContent = _RectangleEx.ToJson();
                    }
                    else
                    {
                        var ex = new RPAException(eErrorMessageType.Error_USERCLOSE, eComponentType.OcrTableAnalyse);
                        Exception(ex, ref captureMessage);
                        return captureMessage;
                    }

                }
                if (!string.IsNullOrEmpty(GuiElement.overlayForm.ExceptionError))
                {
                    var ex = new RPAException(eErrorMessageType.Error_CVCaptureError, eComponentType.OcrTableAnalyse);
                    Exception(ex, ref captureMessage);
                }
                else
                {
                    captureMessage.Info = strJsonContent;
                }
            }
            catch (COMException ex)
            {
                logger.LogError(ex);
            }
            catch (Exception ex)
            {
                if (GuiElement.overlayForm.Visible == true)
                {
                    GuiElement.overlayForm.Close();
                }
                //GuiElement.overlayForm.SetWndHide();
                Exception(ex, eErrorMessageType.Error_NotFoundElement, ref captureMessage);
            }
            finally
            {
                //GuiElement.overlayForm?.inspectorStatus?.Close();
                GuiElement.overlayForm.inspectorStatus.SetWndClose();
                wnd?.Close();
                //wnd?.Dispose();
            }
            return captureMessage;
        }
        private string UIElement_OnSelected(GuiElement element)
        {
            mousePoint = GuiElement.overlayForm.MouseLastClicPoint;
            elementCache = element;
            globalSelector = AddFileAttrToSelector(element.Selector, element.screenShotPath);
            logger.LogInformation($"UIElement_OnSelected worked");
            return globalSelector;
        }
        private string AddFileAttrToSelector(string orgJson, string filePath)
        {
            try
            {
                JArray jArray = JArray.Parse(orgJson);
                return jArray.ToString();
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                return orgJson;
            }
        }
        private void UiElement_OnCancel()
        {
            if (captureMessage != null)
            {
                //构造一个RPAException，内部会打印日志、包装错误信息、错误码等
                var ex = new RPAException(eErrorMessageType.Error_USERCLOSE, eComponentType.OcrTableAnalyse);
                Exception(ex, ref captureMessage);
            }
            logger.LogInformation($"UiElement_OnCancel worked");
            globalSelector = string.Empty;
        }
        #endregion


    }

    public class RectangleEx
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public int WindowsX { get; set; }
        public int WindowsY { get; set; }
        public int WindowsWidth { get; set; }
        public int WindowsHeight { get; set; }

        public string Selector { get; set; }

    }
}

