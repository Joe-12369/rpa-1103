using II.RPA.Core.Runtime;
using II.RPA.Core.Log;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using II.RPA.WinAutomation.CommonUtility;

namespace II.RPA.Core.Provider.PluginLoaderProvider.ClientAction.ClientActionProvider
{
    [ClientActionName("agentSelectUi")]
    internal class UI_Selector_Agent_Provider : BaseActionProvider
    {
        private readonly JObject m_ReqValObject = new JObject
        {
            { "isCVCapture","False"},
            { "InspectStatus",0},
        };
        public UI_Selector_Agent_Provider(ILogger aLogger) : base(aLogger)
        {
        }
        public UI_Selector_Agent_Provider(ILogger aLogger, IRuntimeHostController runtimeHostController) : base(aLogger, runtimeHostController)
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
                PluginName = "Element.CaptureForAgent",
                PluginAction = "Excute",
            };
        }
        public override string ResolveReqVal(string aReqVal)
        {
            var basReqVal = base.ResolveReqVal(aReqVal);
            var jOBaseReqVal = JObject.Parse(basReqVal);
            m_ReqValObject.Add("filepath", m_APicturePath);
            int cvflag = 0;
            var elementTypes = jOBaseReqVal["elementTypes"];
            if (elementTypes is JArray elementTypesArray)
            {
                var cv = elementTypesArray.FirstOrDefault(e => e.ToString() == "cv");
                if (cv != null)
                {
                    cvflag = 1;
                }
            }
            if (jOBaseReqVal.ContainsKey("commonProps"))
            {
                m_ReqValObject.Add("commonProps", jOBaseReqVal["commonProps"]);
            }
            m_ReqValObject.Add("IsCvSupport", cvflag);
            m_ReqValObject.Add("CaptureMode", jOBaseReqVal.Value<string>("type"));
            ///NS上线后、Z-Brain的url从NS获取
            ///cvExecutionServiceURL = "http://127.0.0.1:6440/cv_execution_service/"
            ///cvserver = "http://127.0.0.1:6440/get_text_content_service"
            ///cvPickServer = 'http://127.0.0.1:6440/cv_element_service_setup_detect/'
            ///cvPort = 6440
            m_ReqValObject.Add("cvServer", cvPickServer);
            //是否自动生成相似元素
            if (jOBaseReqVal.ContainsKey("isAutoSimilar"))
            {
                m_ReqValObject.Add("IsAutoSimilar", jOBaseReqVal.Value<int>("isAutoSimilar"));
            }
            return m_ReqValObject.ToString();
        }
        public override PluginRunResult ResolveRetVal(string aRetValFromPlugin)
        {
            var baseRetValJO = base.ResolveRetVal(aRetValFromPlugin);
            if (baseRetValJO.Code == "0")
            {
                if (string.IsNullOrEmpty(baseRetValJO.Result))
                {
                    baseRetValJO.Code = "1";
                    baseRetValJO.Message = "拾取失败";
                    return baseRetValJO;
                }
                else
                {
                    //tars助手拾取返回的 Result中包含 元素selector 和outerHtml
                    var elementInfo = JToken.Parse(baseRetValJO.Result);
                    baseRetValJO.Result = new JObject
                    {
                        //和前端约定还是用uiSelector去承接整个返回的元素信息（selector+outerHtml）
                        {"uiSelector",elementInfo},
                        {"buriedPoint",baseRetValJO.Extend }
                    }.ToString();
                    m_Logger.LogDebug(baseRetValJO.Result);
                    return baseRetValJO;
                }
            }
            else if (baseRetValJO.Code == "-1")
            {
                baseRetValJO.Message = eCommonMessageType.SDK_cs_YongHuQuXiao.GetMessage();
                baseRetValJO.Result = new JObject
                    {
                        {"buriedPoint",baseRetValJO.Extend }
                    }.ToString();
                return baseRetValJO;
            }
            else
            {
                baseRetValJO.Result = new JObject
                    {
                        {"buriedPoint",baseRetValJO.Extend }
                    }.ToString();
                return baseRetValJO;
            }
        }
        public override PluginRunResult PreResolveRetVal(string aReqVal, string aReqValOrigin = null)
        {
            var reqValOriginJO = JObject.Parse(aReqValOrigin);
            var type = reqValOriginJO.GetValue("type");
            var elementTypes = reqValOriginJO.GetValue("elementTypes");
            if (type == null && elementTypes == null)
            {
                return new PluginRunResult
                {
                    Code = "400000974",
                    Message = eCommonMessageType.UI_Selector_Combo_Provider_cs_JianRongXingWenTi.GetMessage(),
                    Result = "",
                };
            }
            return base.PreResolveRetVal(aReqVal, aReqValOrigin);
        }
    }
}
