using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.OcrOffline.Params;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;


namespace II.RPA.Plugin.OcrOffline
{
    public class GetTextByCV_V2 : OcrOfflineComponentBase<ParamGetTextByCV_V2Model>
    {
        public GetTextByCV_V2(II.RPA.Plugin.Abstractions.IPluginInvoker pluginInvoker) : base(pluginInvoker) { }
        private static ILogger logger = Logger.OfContext();

        protected override eComponentType ComponentType => eComponentType.ExecuteCV;

        protected override void ValidateParameter(ParamGetTextByCV_V2Model paramModel)
        {
            try
            {
                if (string.IsNullOrEmpty(paramModel.Selector) && string.IsNullOrEmpty(paramModel.ImgPath))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError);
                }
                else if (!string.IsNullOrEmpty(paramModel.ImgPath) && !File.Exists(paramModel.ImgPath))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError);
                }
                if (string.IsNullOrEmpty(paramModel.Server))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError);
                }
                return;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, $"ExecuteCV 参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }

        protected override JMessage Execute(ParamGetTextByCV_V2Model paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext context)
        {
            JMessage msg = new JMessage();
            try
            {
                Bitmap bitmap = null;
                int x = 0, y = 0, width = 0, height = 0;
                if (!string.IsNullOrEmpty(paramModel.Selector))
                {
                    JToken token = JToken.Parse(paramModel.Selector);
                    JArray ay = null;
                    JObject obj = null;
                    if (token.Type == JTokenType.Array)
                    {
                        ay = JArray.Parse(paramModel.Selector);
                        obj = (JObject)ay.Last();
                        x = (int)obj.GetValue("x");
                        y = (int)obj.GetValue("y");
                        width = Math.Abs((int)obj.GetValue("width"));
                        height = Math.Abs((int)obj.GetValue("height"));
                    }
                    else
                    {
                        ay = JArray.Parse(token["selector"][0]["props"].ToString());
                        foreach (var item in ay)
                        {
                            if (item["name"].ToString() == "x")
                            {
                                x = Convert.ToInt32(item["value"].ToString());
                            }
                            else if (item["name"].ToString() == "y")
                            {
                                y = Convert.ToInt32(item["value"].ToString());
                            }
                            else if (item["name"].ToString() == "width")
                            {
                                width = Math.Abs(Convert.ToInt32(item["value"].ToString()));
                            }
                            else if (item["name"].ToString() == "height")
                            {
                                height = Math.Abs(Convert.ToInt32(item["value"].ToString()));
                            }
                        }
                    }
                    bitmap = UiCommon.CaptureDesktopByRectangle(new Rectangle(x, y, width, height));
                }
                else
                {
                    bitmap = new Bitmap(paramModel.ImgPath);
                }
#if Trace
                bitmap.Save("11.jpg");
#endif
                string cvResults = OcrHelper.GetOCRResult(bitmap, paramModel.Server);
                bitmap?.Dispose();
#if Trace
                using (FileStream fs = File.Open("result.txt", FileMode.OpenOrCreate))
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(cvResults);

                    fs.Write(bytes, 0, bytes.Length);
                    fs.Flush();
                    //cvResults = Encoding.UTF8.GetString(bytes);
                }
#endif
                JArray jarray = JArray.Parse(cvResults);

                List<object> infos = new List<object>();
                foreach (var child in jarray.Children())
                {
                    JObject j = child as JObject;
                    foreach (var i in j.Properties())
                    {
                        var leftTop = ((JObject)(i.Value)).GetValue("lt");
                        var rightTop = ((JObject)(i.Value)).GetValue("rt");
                        var rightBottom = ((JObject)(i.Value)).GetValue("rb");
                        var leftBottom = ((JObject)(i.Value)).GetValue("lb");
                        int targetX = (int)leftTop.First() < (int)leftBottom.First() ? (int)leftTop.First() : (int)leftBottom.First();
                        int targetY = (int)leftTop.Last() < (int)rightTop.Last() ? (int)leftTop.Last() : (int)rightTop.Last();
                        int targetMaxX = (int)rightTop.First() > (int)rightBottom.First() ? (int)rightTop.First() : (int)rightBottom.First();
                        int targetMaxY = (int)rightBottom.Last() > (int)leftBottom.Last() ? (int)rightBottom.Last() : (int)leftBottom.Last();
                        int targetWidth = targetMaxX - targetX;
                        int targetHeight = targetMaxY - targetY;
                        Dictionary<string, object> dic = new Dictionary<string, object>();
                        dic["text"] = i.Name;
                        dic["x"] = x + targetX;
                        dic["y"] = y + targetY;
                        dic["width"] = targetWidth;
                        dic["height"] = targetHeight;
                        infos.Add(dic);
                    }
                }
                msg.Info = JsonHelper.JsonByNewtonJson(infos);
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
            }
            return msg;
        }
    }
}

