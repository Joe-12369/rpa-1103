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
    [ClientActionName("comboSelectUi")]
    internal class UI_Selector_Combo_Provider : BaseActionProvider
    {
        private readonly JObject m_ReqValObject = new JObject
        {
            { "isCVCapture","False"},
            { "InspectStatus",0},
        };
        public UI_Selector_Combo_Provider(ILogger aLogger) : base(aLogger)
        {
        }
        public UI_Selector_Combo_Provider(ILogger aLogger, IRuntimeHostController runtimeHostController) : base(aLogger, runtimeHostController)
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
                PluginName = "Element.Capture",
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
                    var selector = JToken.Parse(baseRetValJO.Result);
                    JObject uiSelector = new JObject();
                    if (selector is JArray)
                    {
                        m_Logger.LogDebug("旧版Selector");
                        var info = new JObject();
                        uiSelector.Add("selector", selector);
                        uiSelector.Add("shot", m_RPicturePath);
                        try
                        {
                            //if 'selector' in uiSelector.keys():
                            var subType = uiSelector["selector"][0]["app"]["subType"].Value<string>();
                            switch (subType)
                            {
                                case "Windows":
                                    info.Add("windowType", subType);
                                    info.Add("windowName", uiSelector["selector"][0]["app"]["processName"].Value<string>());
                                    info.Add("elementName", uiSelector["selector"][0]["winCtrl"].Last["Name"].Value<string>());
                                    break;
                                case "Chrome":
                                    info.Add("windowType", subType);
                                    info.Add("windowName", uiSelector["selector"][0]["app"]["title"].Value<string>());
                                    info.Add("elementName", uiSelector["selector"][0]["webCtrl"]["tagName"].Value<string>());
                                    break;
                                case "IE":
                                    info.Add("windowType", subType);
                                    info.Add("windowName", uiSelector["selector"][0]["app"]["title"].Value<string>());
                                    var ss = uiSelector["selector"][0]["webCtrl"]["selector"].Value<string>();
                                    var ssArray = ss.Split('>');
                                    info.Add("elementName", ssArray.Last());
                                    break;
                                case "Java":
                                    info.Add("windowType", subType);
                                    info.Add("windowName", uiSelector["selector"][0]["javaCtrl"][0]["name"].Value<string>());
                                    info.Add("elementName", uiSelector["selector"][0]["javaCtrl"].Last["id"].Value<string>());
                                    break;
                                default:
                                    m_Logger.LogWarning("Other type", subType);
                                    info = new JObject
                                    {
                                        { "windowType",""},
                                        { "windowName",""},
                                        { "elementName",""},
                                    };
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            m_Logger.LogWarning("", ex);
                            info = new JObject
                            {
                                { "windowType",""},
                                { "windowName",""},
                                { "elementName",""},
                            };
                        }
                        uiSelector.Add("info", info);
                        var app = uiSelector["selector"][0]["app"];
                        if (app == null)
                        {
                            uiSelector["info"]["catureMode"] = uiSelector["selector"][0]["captureMode"];
                            uiSelector["info"]["catureType"] = "cv";

                            JArray filePathes = uiSelector["selector"] as JArray;
                            foreach (var item in filePathes)
                            {
                                var itemPath = item["filePath"];
                                if (itemPath?.Parent is JProperty property)
                                {
                                    property.Remove();
                                }
                                else
                                {
                                    ((JObject)itemPath?.Parent)?.Remove("filePath");
                                }
                            }
                        }
                        else
                        {
                            uiSelector["info"]["catureMode"] = uiSelector["selector"][0]["app"]["captureMode"];
                            uiSelector["info"]["catureType"] = "normal";
                            JObject pathParent = uiSelector["selector"][0]["app"] as JObject;

                            pathParent.Remove("filePath");
                        }
                    }
                    else
                    {
                        m_Logger.LogDebug("新版Selector");
                        uiSelector = selector as JObject;
                        if (uiSelector["commonProps"]["filePath"] != null)
                        {
                            uiSelector["commonProps"]["filePath"] = this.GetRPicturePath(uiSelector["commonProps"]["filePath"]);
                        }
                        //uiSelector["shot"] = RPicturePath;
                        if (uiSelector["elementDetail"] != null)
                        {
                            var icon_name = uiSelector["elementDetail"]["categoryIcon"];
                            try
                            {
                                if (icon_name != null && !string.IsNullOrEmpty(icon_name.ToString()))
                                {
                                    uiSelector["elementDetail"]["categoryIcon"] = Path.Combine("res", icon_name.ToString());
                                }
                                else
                                {
                                    uiSelector["elementDetail"]["categoryIcon"] = icon_name?.ToString();
                                }
                            }
                            catch (Exception ex)
                            {
                                m_Logger.LogWarning("", ex);
                            }
                        }
                    }
                    baseRetValJO.Result = new JObject
                    {
                        {"uiSelector",uiSelector },
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
