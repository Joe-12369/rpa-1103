using II.RPA.Core.Runtime;
using II.RPA.Core.Log;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using II.RPA.WinAutomation.CommonUtility;

namespace II.RPA.Core.Provider.PluginLoaderProvider.ClientAction.ClientActionProvider
{
    [ClientActionName("uiselector")]
    public class UI_Selector_CS_Provider : BaseActionProvider
    {
        public UI_Selector_CS_Provider(ILogger aLogger) : base(aLogger)
        {

        }
        public UI_Selector_CS_Provider(ILogger aLogger, IRuntimeHostController runtimeHostController) : base(aLogger, runtimeHostController)
        {
            NeedSTA = true;
        }
        private JObject m_ReqValObject = new JObject
        {
            { "isCVCapture","False"},
            { "cvServer",""},
            { "InspectStatus",0},
            { "CaptureMode",1},
        };


        public override PluginRunAction ResolveAction(string aReqVal)
        {
            return new PluginRunAction
            {
                PluginAction = "Excute",
                PluginName = "Element.Capture",
            };
        }
        /// <summary>
        /// 前端不传任何参数
        /// </summary>
        /// <param name="aReqVal">null</param>
        /// <returns></returns>
        public override string ResolveReqVal(string aReqVal)
        {
            //{ "filepath": 图片地址, "isCVCapture": False, "cvServer": "", "InspectStatus": 0,"CaptureMode":1}
            ///不处理入参、全部hardcode
            m_ReqValObject.Add("filepath", m_APicturePath);
            return m_ReqValObject.ToString();
        }
        public override PluginRunResult ResolveRetVal(string aRetValFromPlugin)
        {
            var baseRet = base.ResolveRetVal(aRetValFromPlugin);
            if (baseRet.Code == "0")
            {
                if (string.IsNullOrEmpty(baseRet.Result))
                {
                    baseRet.Code = "1";
                    baseRet.Message = "拾取失败";
                }
                else
                {
                    var uiSelector = new JObject
                    {
                        { "selector", JToken.Parse(baseRet.Result) },
                        { "shot", m_RPicturePath }
                    };
                    var info = new JObject();
                    string selectorAll = string.Empty;

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

                    try
                    {                      
                        var fullSelector = uiSelector["selector"]?[0]?["extraInfo"]?["fullSelector"];
                        selectorAll = fullSelector?.Value<string>();

                        JObject parentJO = fullSelector?.Parent as JObject;
                        parentJO?.Remove("fullSelector");
                    }
                    catch (Exception ex)
                    {
                        m_Logger.LogWarning("", ex);
                        ///ignore
                    }
                    uiSelector.Add("info", info);
                    try
                    {
                        var appToken = uiSelector["selector"][0]["app"];
                        if (appToken != null)
                        {
                            ///删除json节点
                            JObject appTokenJO = appToken as JObject;
                            appTokenJO.Remove("filePath");
                        }
                        else
                        {
                            JArray filePathes = uiSelector["selector"] as JArray;
                            foreach (var item in filePathes)
                            {
                                var itemPath = item["filePath"];
                                ((JObject)itemPath?.Parent)?.Remove("filePath");
                            }
                        }
                        object selectorAllVal;
                        if (string.IsNullOrEmpty(selectorAll))
                        {
                            selectorAllVal = new JArray();
                        }
                        else
                        {
                            selectorAllVal = selectorAll;
                        }
                        baseRet.Result = new JObject
                        {
                            {"uiSelector",uiSelector },
                            {"selectorAll",(JToken)selectorAllVal},
                        }.ToString();
                        return baseRet;
                    }
                    catch (Exception ex)
                    {
                        m_Logger.LogWarning("", ex);
                        ///ignore
                    }
                }
            }
            else if (baseRet.Code == "-1")
            {
                baseRet.Result = "cancel";
                baseRet.Message = eCommonMessageType.SDK_cs_YongHuQuXiao.GetMessage();
                return baseRet;
            }
            else
            {
                baseRet.Message = "selector格式有误";
                return baseRet;
            }
            return new PluginRunResult
            {
                Code = "99",
                Result = "error",
                Message = "selector解析失败",
            };
        }
        
    }
}
