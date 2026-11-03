using II.RPA.Core.Runtime;
using II.RPA.Core.Log;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Core.Provider.PluginLoaderProvider.ClientAction.ClientActionProvider
{
    /// <summary>
    /// 走engine
    /// </summary>
    [ClientActionName("captureRenovateElements")]
    internal class UI_Selector_Renovate_Provider : BaseActionProvider
    {
        public UI_Selector_Renovate_Provider(ILogger aLogger) : base(aLogger)
        {

        }
        public UI_Selector_Renovate_Provider(ILogger aLogger, IRuntimeHostController runtimeHostController) : base(aLogger, runtimeHostController)
        {
            NeedSTA = true;
        }

        public override PluginRunAction ResolveAction(string aReqVal)
        {
            return new PluginRunAction
            {
                PluginAction = "Excute",
                PluginName = "Element.CaptureRenovate",
            };
        }
        public override string ResolveReqVal(string aReqVal)
        {
            var baseReqVal = base.ResolveReqVal(aReqVal);
            JObject JoReqVal = JObject.Parse(baseReqVal);
            var similarSelector = JoReqVal["renovateSelector"];
            try
            {
                m_Logger.LogInfo("Get renovateSelector selectorAll");
                var fullSelector = JoReqVal["selectorAll"];
                similarSelector[0]["extraInfo"]["fullSelector"] = fullSelector;
            }
            catch (Exception ex)
            {
                m_Logger.LogWarning("", ex);
            }
            JObject resolveReq = new JObject
            {
                {"filepath",m_APicturePath},
                {"isCVCapture",false},
                {"cvServer",""},
                {"InspectStatus",0},
                {"selector",similarSelector.ToString()},
            };
            return resolveReq.ToString();
        }
        public override PluginRunResult ResolveRetVal(string aRetValFromPlugin)
        {
            var baseRetVal = base.ResolveRetVal(aRetValFromPlugin);
            if (baseRetVal.Code == "0")
            {
                if (string.IsNullOrEmpty(baseRetVal.Result))
                {
                    return new PluginRunResult
                    {
                        Message = "拾取失败",
                        Code = "0",
                        Result = "fail",
                    };
                }
                else
                {
                    JObject uiSelector = new JObject();
                    var selector = JToken.Parse(baseRetVal.Result);
                    if (selector["commonProps"] == null)
                    {
                        m_Logger.LogDebug("旧版Selector");
                        uiSelector["selector"] = selector;
                        uiSelector["shot"] = m_RPicturePath;

                        var info = new JObject();
                        JToken selectorAll;
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
                            var fullSelector = uiSelector["selector"][0]["extraInfo"]["fullSelector"];
                            selectorAll = fullSelector;
                            JObject fullSelectorParent = fullSelector.Parent as JObject;
                            fullSelectorParent.Remove("fullSelector");
                        }
                        catch (Exception ex)
                        {
                            selectorAll = new JArray();
                            m_Logger.LogWarning("", ex);
                            info = new JObject
                            {
                                { "windowType",""},
                                { "windowName",""},
                                { "elementName",""},
                            };
                        }

                        uiSelector["info"] = info;
                        if (uiSelector["selector"][0]["app"] != null)
                        {
                            var app = uiSelector["selector"][0]["app"] as JObject;
                            app.Remove("filePath");
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
                        //var elements = GetElementAarray(selector);
                        baseRetVal.Result = new JObject
                        {
                            {"uiSelector",uiSelector},
                            {"selectorAll",selectorAll},
                        }.ToString();
                        return baseRetVal;
                    }
                    else
                    {
                        m_Logger.LogDebug("新版Selector");
                        uiSelector = selector as JObject;
                        if (uiSelector["commonProps"]["filePath"] != null
                            && uiSelector["commonProps"]["filePath"].ToString() != "")
                        {
                            uiSelector["commonProps"]["filePath"] = this.GetRPicturePath(uiSelector["commonProps"]["filePath"]);
                        }
                        uiSelector["shot"] = m_RPicturePath;
                        baseRetVal.Result = new JObject
                        {
                            {"uiSelector",uiSelector}
                        }.ToString();
                        return baseRetVal;
                    }
                }
            }
            else if (baseRetVal.Code == "-1")
            {
                baseRetVal.Code = "1";
                baseRetVal.Result = new JObject
                {
                    {"uiSelector",new JObject() }
                }.ToString();
                baseRetVal.Message = "用户取消";
                return baseRetVal;
            }
            else
            {
                baseRetVal.Code = "1";
                baseRetVal.Result = new JObject
                {
                    {"uiSelector",new JObject() }
                }.ToString();
                return baseRetVal;
            }
        }
    }
}

