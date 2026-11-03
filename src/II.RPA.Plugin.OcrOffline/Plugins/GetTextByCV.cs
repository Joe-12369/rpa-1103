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
    public class GetTextByCV : OcrOfflineComponentBase<ParamGetTextByCVModel>
    {
        public GetTextByCV(II.RPA.Plugin.Abstractions.IPluginInvoker pluginInvoker) : base(pluginInvoker) { }
        private static ILogger logger = Logger.OfContext();

        protected override eComponentType ComponentType => eComponentType.ExecuteCV;

        protected override void ValidateParameter(ParamGetTextByCVModel paramModel)
        {
            try
            {
                if (string.IsNullOrEmpty(paramModel.Selector))
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

        protected override JMessage Execute(ParamGetTextByCVModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext context)
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

                List<string> infos = new List<string>();
                foreach (var child in jarray.Children())
                {
                    JObject j = child as JObject;
                    foreach (var i in j.Properties())
                    {
                        infos.Add(i.Name);
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

