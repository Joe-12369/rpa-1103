using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.Core.Threading;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Params;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;

namespace II.RPA.Plugin.GUI.Plugins
{
    public class CaptureByRegion : OverlayCaptureAbilityBase<ParamCaptureByRegionModel>
    {

        private static ILogger logger = Logger.OfContext();
        protected override eComponentType ComponentType => eComponentType.CaptureByRegion;
        protected override void ValidateParameter(ParamCaptureByRegionModel paramModel) {
            try
            {
                if (string.IsNullOrEmpty(paramModel.filepath))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError);
                }
                return;
            }
            catch (Exception ex)
            {
                logger.LogInformation($"参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }
        GuiElement elementCache = null;
        JMessage captureMessage = null;
        string globalSelector = null;
        Point mousePoint = Point.Empty;

        protected override JMessage Execute(ParamCaptureByRegionModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
        {
            captureMessage = new JMessage();
            globalSelector = string.Empty;
            elementCache = null;
            mousePoint = Point.Empty;
            CaptureCVForOcrTableWnd wnd = null;
            //CVScreenWnd wnd = null;
            try
            {
                GuiElement.OnSelected = UIElement_OnSelected;
                GuiElement.OnCanceled = UiElement_OnCancel;
                GuiElement.StartCVRegionHighlight(paramModel.filepath);
                GuiElement.overlayForm.Visible = false;
                GuiElement.overlayForm.inspectorStatus.SetWndShow();
                GuiElement.overlayForm.ShowCaptureDialog();
                if (elementCache != null)
                {
                    wnd = new CaptureCVForOcrTableWnd(elementCache.BoundingRectangle, elementCache.currentInformativeScreenshot, mousePoint, paramModel.filepath);
                    if (wnd.ShowDialog() == true)
                    {
                        Rectangle r = new Rectangle();
                        Point start = wnd.startPoint;
                        Point end = wnd.endPoint;

                        r.X = Math.Min(start.X, end.X);
                        r.Y = Math.Min(start.Y, end.Y);

                        r.Width = Math.Abs(end.X - start.X);
                        r.Height = Math.Abs(end.Y - start.Y);

                        JObject locationNode = new JObject();
                        locationNode.Add("LocationX", r.X);
                        locationNode.Add("LocationY", r.Y);
                        locationNode.Add("Width", r.Width);
                        locationNode.Add("Height", r.Height);

                        JArray jarray = JArray.Parse(globalSelector);
                        JObject jobj = new JObject();
                        jobj.Add("name", "cvelement");

                        Props x = new Props()
                        {
                            Name = "LocationX",
                            Value = r.X.ToString(),
                            Pattern = Pattern.EQUAL.GetPatternAttribute(),
                            Accurate = "true"

                        };
                        Props y = new Props()
                        {
                            Name = "LocationY",
                            Value = r.Y.ToString(),
                            Pattern = Pattern.EQUAL.GetPatternAttribute(),
                            Accurate = "true"

                        };
                        Props Width = new Props()
                        {
                            Name = "Width",
                            Value = r.Width.ToString(),
                            Pattern = Pattern.EQUAL.GetPatternAttribute(),
                            Accurate = "true"

                        };
                        Props Height = new Props()
                        {
                            Name = "Height",
                            Value = r.Height.ToString(),
                            Pattern = Pattern.EQUAL.GetPatternAttribute(),
                            Accurate = "true"

                        };
                        JArray array = new JArray();
                        array.Add(JsonConvert.DeserializeObject(JsonConvert.SerializeObject(x)));
                        array.Add(JsonConvert.DeserializeObject(JsonConvert.SerializeObject(y)));
                        array.Add(JsonConvert.DeserializeObject(JsonConvert.SerializeObject(Width)));
                        array.Add(JsonConvert.DeserializeObject(JsonConvert.SerializeObject(Height)));
                        jobj.Add("props", array);
                        jarray.Add(jobj);
                        var elementName = string.Empty;
                        var firstNode = jarray.First;
                        elementName = firstNode["name"]?.ToString();
                        var firstProps = jarray.First["props"];
                        if (string.IsNullOrEmpty(elementName))
                            foreach (var item in JArray.Parse(firstProps.ToString()))
                            {
                                if (item.Value<string>("name") == "name" || item.Value<string>("name") == "class")
                                {
                                    elementName = item.Value<string>("value").ToString();
                                    break;
                                }
                            }
                        globalSelector = ExtractViewData.AddCommonProps(jarray.ToString(), eGuiType.CV, "", elementName, filePath: paramModel.filepath);
                    }
                    else
                    {
                        var ex = new RPAException(eErrorMessageType.Error_USERCLOSE, eComponentType.CaptureByRegion);
                        Exception(ex, ref captureMessage);
                        return captureMessage;
                    }

                }
                if (!string.IsNullOrEmpty(GuiElement.overlayForm.ExceptionError))
                {
                    var ex = new RPAException(eErrorMessageType.Error_CVCaptureError, eComponentType.CaptureByRegion);
                    Exception(ex, ref captureMessage);
                }
                else
                {
                    captureMessage.Info = globalSelector;
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
                Exception(ex, eErrorMessageType.Error_NotFoundElement, ref captureMessage);
            }
            finally
            {
                GuiElement.overlayForm.inspectorStatus.SetWndClose();
                wnd?.Close();
            }
            return captureMessage;
        }

        private string UIElement_OnSelected(GuiElement element)
        {
            mousePoint = GuiElement.overlayForm.MouseLastClicPoint;
            elementCache = element;
            globalSelector = AddFileAttrToSelector(element.Selector, element.screenShotPath);
            return globalSelector;
        }
        private string AddFileAttrToSelector(string orgJson, string filePath)
        {
            try
            {
                JArray jArray = JArray.Parse(orgJson);
                var item = jArray?.FirstOrDefault();
                //if (item != null)
                //{
                //    (item as JObject).Add("filePath", filePath);
                //}
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
                var ex = new RPAException(eErrorMessageType.Error_USERCLOSE, eComponentType.CaptureByRegion);
                Exception(ex, ref captureMessage);
            }
            globalSelector = string.Empty;
        }
    }
}
