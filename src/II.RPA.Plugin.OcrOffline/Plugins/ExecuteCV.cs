using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.OcrOffline.Params;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json.Linq;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;

namespace II.RPA.Plugin.OcrOffline
{
    public class ExecuteCV : OcrOfflineComponentBase<ParamExecuteCVModel>
    {
        public ExecuteCV(II.RPA.Plugin.Abstractions.IPluginInvoker pluginInvoker) : base(pluginInvoker) { }
        private static ILogger logger = Logger.OfContext();

        protected override eComponentType ComponentType => eComponentType.ExecuteCV;

        protected override void ValidateParameter(ParamExecuteCVModel paramModel)
        {
            try
            {
                if (string.IsNullOrEmpty(paramModel.Selector))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError);
                }
                if (string.IsNullOrEmpty(paramModel.TextToFind))
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
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"ExecuteCV 参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }

        protected override JMessage Execute(ParamExecuteCVModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext context)
        {
            JMessage msg = new JMessage();
            try
            {
                int x = 0, y = 0, width = 0, height = 0;
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

                Bitmap bitmap = UiCommon.CaptureDesktopByRectangle(new Rectangle(x, y, width, height));
#if Trace
                bitmap.Save("11.jpg");
#endif
                string cvResults = OcrHelper.GetOCRResult(bitmap, paramModel.Server);
                //string cvResults = "";
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
                int targetX = 0;
                int targetY = 0;
                int targetWidth = 0;
                int targetHeight = 0;
                foreach (var child in jarray.Children())
                {
                    JObject j = child as JObject;

                    foreach (var i in j.Properties())
                    {
                        logger.LogDebug(i.Name);
                    }
                    var ru = j.Properties().Where(m => m.Name.Contains(paramModel.TextToFind)).FirstOrDefault();
                    if (ru != null)
                    {
                        var leftTop = ((JObject)(ru.Value)).GetValue("lt");
                        var rightTop = ((JObject)(ru.Value)).GetValue("rt");
                        var rightBottom = ((JObject)(ru.Value)).GetValue("rb");
                        var leftBottom = ((JObject)(ru.Value)).GetValue("lb");
                        targetX = (int)leftTop.First() < (int)leftBottom.First() ? (int)leftTop.First() : (int)leftBottom.First();
                        targetY = (int)leftTop.Last() < (int)rightTop.Last() ? (int)leftTop.Last() : (int)rightTop.Last();
                        int targetMaxX = (int)rightTop.First() > (int)rightBottom.First() ? (int)rightTop.First() : (int)rightBottom.First();
                        int targetMaxY = (int)rightBottom.Last() > (int)leftBottom.Last() ? (int)rightBottom.Last() : (int)leftBottom.Last();
                        targetWidth = targetMaxX - targetX;
                        targetHeight = targetMaxY - targetY;
                        break;
                    }
                }

                if (!(targetX == 0 && targetY == 0 && targetWidth == 0 && targetHeight == 0))
                {
                    Point location = new Point();
                    int tempStartX = x + targetX;
                    int tempStartY = y + targetY;
                    location.X = tempStartX + targetWidth / 2;
                    location.Y = tempStartY + targetHeight / 2;
                    msg.Info = JsonHelper.JsonByNewtonJson(location);
                }
                else
                {
                    Exception(null, eGuiType.CV, eErrorMessageType.Error_NotFoundElement, ref msg);
                    return msg;
                }
            }
            catch (Exception ex)
            {
                Exception(ex, eGuiType.CV, eErrorMessageType.Error_NotFoundElement, ref msg);
                return msg;
            }
            return msg;
        }
    }
}

