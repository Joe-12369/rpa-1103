using II.RPA.Core.Runtime;
using II.RPA.Core.Log;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Core.Provider.PluginLoaderProvider.ClientAction.ClientActionProvider
{
    [ClientActionName("DataCollectConfiguration")]
    public class NewDataCollectProvider : BaseActionProvider
    {
        public NewDataCollectProvider(ILogger aLogger, IRuntimeHostController aRuntimeHostController) : base(aLogger, aRuntimeHostController)
        {
            NeedSTA = true;
        }
        public override int TimeoutSeconds
        {
            get
            {
                if (int.TryParse(ConfigurationManager.AppSettings["CaptureActionTimeoutSecond"], out int anCaptureActionTimeoutSecond))
                {
                    m_Logger.LogDebug($"TimeoutSeconds from config is {anCaptureActionTimeoutSecond}");
                    return anCaptureActionTimeoutSecond;
                }
                else
                {
                    m_Logger.LogDebug($"Can not read CaptureActionTimeoutSecond from config.reture base.TimeoutSeconds instead");
                    return base.TimeoutSeconds;
                }


            }
        }
        public override PluginRunAction ResolveAction(string aReqVal)
        {
            return new PluginRunAction
            {
                PluginAction = "Excute",
                PluginName = "Element.DataCollectConfiguration",
            };
        }
        public override string ResolveReqVal(string aReqVal)
        {
            var basReqVal = base.ResolveReqVal(aReqVal);
            var jOBaseReqVal = JObject.Parse(basReqVal);
            jOBaseReqVal.Add("PackagePath", PackagePath);

            return jOBaseReqVal.ToString();
        }

        /// <summary>
        /// 数据采集结果返回加工
        /// </summary>
        /// <param name="aRetValFromPlugin"></param>
        /// <returns></returns>
        public override PluginRunResult ResolveRetVal(string aRetValFromPlugin)
        {
            var baseRetValJO = base.ResolveRetVal(aRetValFromPlugin);

            if (string.IsNullOrEmpty(baseRetValJO.Result))//没有正常返回内容
            {
                baseRetValJO.Result = new JObject
                    {
                        {"buriedPoint",baseRetValJO.Extend }
                    }.ToString();
                return baseRetValJO;
            }
            else
            {
                var selector = JToken.Parse(baseRetValJO.Result);
                baseRetValJO.Result = new JObject
                    {
                        {"uiSelector",selector },
                        {"buriedPoint",baseRetValJO.Extend }
                    }.ToString();
                m_Logger.LogDebug(baseRetValJO.Result);
                return baseRetValJO;
            }

            #region 废弃
            //    if (baseRetValJO.Code == "0")
            //{
            //    if (string.IsNullOrEmpty(baseRetValJO.Result))
            //    {
            //        baseRetValJO.Code = "-1";
            //        baseRetValJO.Message = string.IsNullOrEmpty(baseRetValJO.Message)?"拾取失败": baseRetValJO.Message;
            //        return baseRetValJO;
            //    }
            //    else
            //    {
            //        var selector = JToken.Parse(baseRetValJO.Result);
            //        baseRetValJO.Result = new JObject
            //        {
            //            {"uiSelector",selector },
            //            {"buriedPoint",baseRetValJO.Extend }
            //        }.ToString();
            //        m_Logger.LogDebug(baseRetValJO.Result);
            //        return baseRetValJO;
            //    }
            //}
            //else if (baseRetValJO.Code == "1")
            //{
            //    baseRetValJO.Message = string.IsNullOrEmpty(baseRetValJO.Message) ? "用户取消" : baseRetValJO.Message;
            //    baseRetValJO.Result = new JObject
            //        {
            //            {"buriedPoint",baseRetValJO.Extend }
            //        }.ToString();
            //    return baseRetValJO;
            //}
            //else
            //{
            //    baseRetValJO.Message = baseRetValJO.Message;
            //    baseRetValJO.Result =  new JObject
            //        {
            //            {"buriedPoint",baseRetValJO.Extend }
            //        }.ToString();
            //    return baseRetValJO;
            //}
            #endregion
        }
    }
}
