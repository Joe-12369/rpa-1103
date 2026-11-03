using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
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
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;


namespace II.RPA.Plugin.GUI
{

    public class CaptureCV : StaElementAbilityBase<ParamCaptureCVModel>
    {
        private static ILogger logger = Logger.OfContext();
        public CaptureCV(IStaDispatcher staDispatcher)
            : base(staDispatcher)
        {
        }

        protected override eComponentType ComponentType => eComponentType.CaptureCV;

        protected override void ValidateParameter(ParamCaptureCVModel paramModel) {
            try
            {
                logger.LogDebug("Start Capture");
                if (string.IsNullOrEmpty(paramModel.FilePath))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError, "缺少文件地址参数".I18n("controlType_CaptureCV_98CDB9F7"));
                }
                return;
            }
            catch (Exception ex)
            {
                logger.LogInformation($"参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }

        protected override JMessage Execute(ParamCaptureCVModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
        {
            JMessage cvMessage = new JMessage();
            try
            {
                //Bitmap bitmap = UiCommon.GetDesktop();
                //Bitmap bitmap = GuiElement.CaptureDesktop();
                //bitmap.Save(Path.Combine(Path.GetDirectoryName(paramModel.FilePath), Guid.NewGuid().ToString() + ".png"));
                //GuiElement.overlayForm.inspectorStatus = new InspectorStatusForm("区域拾取");

                //GuiElement.overlayForm.inspectorStatus = new InspectInfoWnd("区域拾取");
                GuiElement.overlayForm.InitCancellationForCapture();
                if (GuiElement.overlayForm.inspectorStatus == null)
                {
                    GuiElement.overlayForm.inspectorStatus = new InspectInfoWnd();
                }
                // InitCaptureByRegion / SetWndShow：与 CaptureCVForOcrTableWnd.Window_Loaded 合一，避免与 InspectInfoWnd 叠用时的重复初始化

               ////新方法
               CaptureCVForOcrTableWnd wnd = new CaptureCVForOcrTableWnd(paramModel.FilePath);
                ////CVScreenWnd wnd = new CVScreenWnd(paramModel.FilePath);//有重影
                //wnd.inspectorStatusForm.Show();
                ////if (wnd.ShowDialog() == DialogResult.OK)
                if (wnd.ShowDialog() == true)
                {
                    Rectangle r = new Rectangle();
                    Point start = wnd.startPoint;
                    Point end = wnd.endPoint;
                    r.X = Math.Min(start.X, end.X);
                    r.Y = Math.Min(start.Y, end.Y);

                    r.Width = Math.Abs(end.X - start.X);
                    r.Height = Math.Abs(end.Y - start.Y);

                    //JArray jarray = new JArray();
                    //JObject node1 = new JObject();
                    //node1.Add("selector", "CV");
                    //JObject node2 = new JObject();
                    //node2.Add("x", r.X);
                    //node2.Add("y", r.Y);
                    //node2.Add("width", r.Width);
                    //node2.Add("height", r.Height);
                    //jarray.Add(node1);
                    //jarray.Add(node2);
                    //JObject jobj = new JObject();
                    //jobj.Add("name", "cv");
                    //var selectorProp = new
                    //{
                    //    Name = "cv",
                    //    props = new List<Props>()
                    //};
                    //Props range = new Props()
                    //{
                    //    Name = "Range",
                    //    Value = node2.ToString(),
                    //    Pattern = Pattern.EQUAL.GetPatternAttribute(),
                    //    Accurate = "true"

                    //};
                    //JArray array = new JArray();
                    //array.Add(JsonConvert.DeserializeObject(JsonConvert.SerializeObject(range)));
                    //jobj.Add("props", array);

                    //var wndStr = JsonConvert.SerializeObject(jobj);
                    //var wndObj = JsonConvert.DeserializeObject(wndStr);
                    //JArray j = new JArray();
                    //j.Add(wndObj);

                    JArray jarray = new JArray();
                    JObject jobj = new JObject();
                    jobj.Add("name", "cv");
                    //var selectorProp = new
                    //{
                    //    Name = "cv",
                    //    props = new List<Props>()
                    //};
                    Props x = new Props()
                    {
                        Name = "x",
                        Value = r.X.ToString(),
                        Pattern = Pattern.EQUAL.GetPatternAttribute(),
                        Accurate = "true"

                    };
                    Props y = new Props()
                    {
                        Name = "y",
                        Value = r.Y.ToString(),
                        Pattern = Pattern.EQUAL.GetPatternAttribute(),
                        Accurate = "true"

                    };
                    Props Width = new Props()
                    {
                        Name = "width",
                        Value = r.Width.ToString(),
                        Pattern = Pattern.EQUAL.GetPatternAttribute(),
                        Accurate = "true"

                    };
                    Props Height = new Props()
                    {
                        Name = "height",
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
                    cvMessage.Info = ExtractViewData.AddCommonProps(jarray.ToString(), eGuiType.CV, "", "OCR元素".I18n("CaptureCV_8D76810A"), filePath: paramModel.FilePath);
                }
                else
                {
                    Exception(eErrorMessageType.Error_USERCLOSE, ref cvMessage);
                }
                //wnd.inspectorStatusForm.Close();
                wnd.Close();
                ////wnd.Dispose();
            }
            catch (COMException ex)
            {
                logger.LogError(ex);
            }
            catch (Exception ex)
            {
                Exception(ex, eErrorMessageType.Error_CVCaptureError, ref cvMessage);
            }
            return cvMessage;
        }

    }
}
