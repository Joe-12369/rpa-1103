using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace II.RPA.Plugin.GUI.ProcessCreator.ViewModel
{
    public class CvActionViewModel : ActionViewModelBase
    {
        private static readonly ILogger logger = Logger.OfContext();
        private string m_UiSelector;
        public CvActionViewModel(Task<SelectElementModel> aDoSelectorTask,
            ActionParamModel anActionParam) : base(aDoSelectorTask, anActionParam)
        {
            logger.LogDebug("CvActionViewModel");
        }
        protected override void GenerateComponent(string aComponentId)
        {
            Task.Run(async() => 
            {
                JArray actions = new JArray();
                m_UiSelector = (await DoSelectorTask).Selector;
                if (aComponentId == "Element_Click_V2")
                {
                    logger.LogDebug($"generate:{aComponentId}");
                    var actionJO = new JObject();
                    actionJO.Add("type", "Element_Click_V2");
                    var propertynJO = new JObject();
                    JObject uiSelector = new JObject();
                    if (string.IsNullOrEmpty(m_UiSelector))
                    {
                        logger.LogDebug("GenerateComponent: m_UiSelector is empty");
                        return;
                    }
                    uiSelector.Add("value", m_UiSelector);
                    propertynJO.Add("args0.uiSelector", uiSelector);
                    actionJO.Add("properties", propertynJO);
                    actions = new JArray { actionJO };
                    logger.LogDebug($"{actionJO.ToJson()}");
                }
                else
                {
                    var actionJO = new JObject
                    {
                        { "type", aComponentId }
                    };
                    var propertynJO = new JObject();
                    ExecuteAction? currentAction = CurrentResolvedActions.FirstOrDefault(ca => ca.ActionId == aComponentId);
                    if (currentAction.HasValue && currentAction.Value.ActionInput != null)
                    {
                        logger.LogDebug($"Find {aComponentId} in CurrentResolvedActions!");
                        foreach (string input in currentAction.Value.ActionInput)
                        {
                            //有默认值
                            if (input.Contains("="))
                            {
                                string[] inputParams = input.Split('=');
                                if (inputParams.Length == 2)
                                {
                                    JObject valueJo = new JObject
                            {
                                { "value", inputParams[1] }
                            };
                                    propertynJO.Add(inputParams[0], valueJo);
                                }
                            }
                            else
                            {
                                JObject valueJo = new JObject();
                                switch (input)
                                {
                                    case "args0.uiSelector":
                                    case "args0.condition":
                                    case "args0.similar_element_object":
                                        m_UiSelector = (await DoSelectorTask).Selector;
                                        valueJo.Add("value", m_UiSelector);
                                        break;
                                    case "args0.location":
                                    case "args0.End":
                                        valueJo.Add("value", $"[{ActionParam.MouseLocation.X},{ActionParam.MouseLocation.X}]");
                                        break;
                                    case "args0.image_path":
                                    case "args0.image":
                                        var _picturePath = Path.Combine("res", Path.GetFileName(ActionParam.PicturePath));
                                        valueJo.Add("value", $"{_picturePath}");
                                        break;
                                    case "args0.executeable":
                                        valueJo.Add("value", CurrentResolvedActionParams["args0.executeable"]);
                                        break;
                                    case "args0.itemsToSelect":
                                    case "args0.text":
                                        try
                                        {
                                            var elementText = ActionParam.CurrentGuiNode.GetText();
                                            if (!string.IsNullOrEmpty(elementText))
                                            {
                                                valueJo.Add("value", $"\"{ActionParam.CurrentGuiNode.GetText()}\"");
                                            }
                                        }
                                        catch
                                        {
                                            logger.LogError($"Fail to set select item for combobox");
                                        }
                                        break;
                                    default:
                                        if (CurrentResolvedActionParams.TryGetValue(input, out string value))
                                        {
                                            valueJo.Add("value", value);
                                        }
                                        else
                                        {
                                            logger.LogError($"There is invalid arg:{input}");
                                        }
                                        break;
                                }
                                propertynJO.Add(input, valueJo);
                            }
                        }
                    }
                    else
                    {
                        if (ElementComponents.TryGetValue(aComponentId, out string arg))
                        {
                            JObject uiSelector = new JObject();
                            m_UiSelector = (await DoSelectorTask).Selector;
                            uiSelector.Add("value", m_UiSelector);
                            propertynJO.Add(arg, uiSelector);
                            logger.LogDebug($"ElementComponents get {aComponentId}");
                        }
                        else
                        {
                            logger.LogDebug($"Cant get {aComponentId} from ElementComponents");
                        }
                        logger.LogDebug($"can not find {aComponentId} in CurrentResolvedActions.Maybe search");
                    }
                    actionJO.Add("properties", propertynJO);
                    actions = new JArray { actionJO };
                    logger.LogDebug($"{actionJO.ToJson()}");
                }
                if (MessageService.PostMessage(TerminalType.CoreServer, "flowedit.plugingenerate", actions, (ret) =>
                {
                    logger.LogDebug($"post return : {ret}");
                }))
                {
                    logger.LogDebug($"post done");
                }
                else
                {
                    logger.LogError($"post fail");
                }
            });
            
        }
        //protected override Task ResolveAction()
        //{
        //    logger.LogDebug("CVElementActionRecommend");
        //    var actions = new List<ActionModel>();
        //    return Task.Run(async () =>
        //    {
        //        var element = await DoSelectorTask;
        //        m_UiSelector = element.Selector;
        //        m_CvResut = element.CvElementAdditionJO;
        //        //默认其他图标
        //        int elementClass = 19;
        //        try
        //        {
        //            JArray props = m_CvResut["props"] as JArray;
        //            var elementobj = props.FirstOrDefault(p => p["name"].ToString() == "elementobj");
        //            if (elementobj != null)
        //            {
        //                var valueObject = JsonConvert.DeserializeObject<JObject>(elementobj["value"].ToString());
        //                var cvtext = valueObject["result_value"]["text_contents"];
        //                var cvimageBase64 = valueObject["result_value"]["template_image"];
        //                if (cvtext == null || string.IsNullOrEmpty(cvtext.ToString()))
        //                {
        //                    string imagebase64 = cvimageBase64.ToString();
        //                    Dictionary<string,object> requestObject = new Dictionary<string, object>
        //                    {
        //                        {"image",imagebase64},
        //                        ///...
        //                    };
        //                    HttpContent content = new StringContent(requestObject.ToJson(), Encoding.UTF8);
        //                    content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        //                    logger.LogDebug($"start element_class");
        //                    var url = SignHttpRequest("http://127.0.0.1:6440/element_class", requestObject);
        //                    var response = await HttpClientInstance.PostAsync(url, content);
        //                    var responseEnsure = response.EnsureSuccessStatusCode();
        //                    var responseString = await responseEnsure.Content.ReadAsStringAsync();
        //                    elementClass = JObject.Parse(responseString).GetValue<int>("ECResult");
        //                    logger.LogDebug($"element_class:{responseString}");
        //                }
        //            }
        //            else
        //            {
        //                throw new Exception("elementobj is null");
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            logger.LogError("fail to call cv element class", ex);
        //        }
        //        if (TrySelectorAnalyze(m_UiSelector, elementClass, out List<ActionModel> anActionModels))
        //        {
        //            actions = anActionModels;
        //        }
        //        else
        //        {
        //            logger.LogInformation($"Fail to intelligence recommend,default instead");
        //            var firstAction = Components.FirstOrDefault(c => c.ComponentId == "Element_Click_V2");
        //            if (firstAction != null)
        //            {
        //                actions.Add(new ActionModel
        //                {
        //                    ActionId = firstAction.ComponentId,
        //                    ActionName = firstAction.ComponentLabel,
        //                    DataItemType = DataItemType.PREFER_ACTION,
        //                    ActionIcon = firstAction.IconFontCode,
        //                    //ActionIcon = firstAction.
        //                });
        //            }
        //            var secondAction = Components.FirstOrDefault(c => c.ComponentId == "Element_GetText");
        //            if (secondAction != null)
        //            {
        //                actions.Add(new ActionModel
        //                {
        //                    ActionId = secondAction.ComponentId,
        //                    ActionName = secondAction.ComponentLabel,
        //                    DataItemType = DataItemType.PREFER_ACTION,
        //                    ActionIcon = secondAction.IconFontCode,
        //                    //ActionIcon = firstAction.
        //                });
        //            }
        //            var thirdAction = Components.FirstOrDefault(c => c.ComponentId == "logic_control_if_element_v1");
        //            if (thirdAction != null)
        //            {
        //                actions.Add(new ActionModel
        //                {
        //                    ActionId = thirdAction.ComponentId,
        //                    ActionName = thirdAction.ComponentLabel,
        //                    DataItemType = DataItemType.PREFER_ACTION,
        //                    ActionIcon = thirdAction.IconFontCode,
        //                    //ActionIcon = firstAction.
        //                });
        //            }
        //            var fourthAction = Components.FirstOrDefault(c => c.ComponentId == "Element_WaitElement");
        //            if (fourthAction != null)
        //            {
        //                actions.Add(new ActionModel
        //                {
        //                    ActionId = fourthAction.ComponentId,
        //                    ActionName = fourthAction.ComponentLabel,
        //                    DataItemType = DataItemType.PREFER_ACTION,
        //                    ActionIcon = fourthAction.IconFontCode,
        //                    //ActionIcon = firstAction.
        //                });
        //            }
        //        }
        //        actions.Add(new ActionModel { ActionName = "Separator", DataItemType = DataItemType.SEPARATOR });

        //        #region category recommend
        //        actions.AddRange(RecommendActionCategories);
        //        #endregion

        //        SetActionCollection(actions);
        //    });
        //}
        private bool TrySelectorAnalyze(string anUiSelector,int anElementClass, out List<ActionModel> anActionModels)
        {
            anActionModels = new List<ActionModel>();
            if (StaticModels.CvElementActions.TryGetValue(anElementClass.ToString(), out List<ElementAction> actions))
            {
                CurrentResolvedActions = actions.First().Actions;
                anActionModels = CurrentResolvedActions.Select(a =>
                {
                    var component = Components.FirstOrDefault(c => c.ComponentId == a.ActionId);
                    if (component != null)
                    {
                        return new ActionModel
                        {
                            ActionId = component.ComponentId,
                            ActionName = component.ComponentLabel,
                            DataItemType = DataItemType.PREFER_ACTION,
                            //ActionIcon = component.IconFontCode,
                            ActionIcon = component.IconUnicode,
                        };
                    }
                    else
                    {
                        NLogger.Default.Error($"mismatch component:{a.ActionId}");
                        return new ActionModel
                        {
                            ActionId = a.ActionId,
                            ActionName = a.ActionId,
                            DataItemType = DataItemType.PREFER_ACTION,
                        };
                    }
                }).ToList();
                if (actions.Any())
                {
                    NLogger.Default.Info($"TrySelectorAnalyze:true");
                    return true;
                }
            }
            NLogger.Default.Warn($"TrySelectorAnalyze:false");
            return false;
        }

        private string SignHttpRequest(string anUrl, Dictionary<string, object> dicParams)
        {
            Uri uri = new Uri(anUrl);
            string saltStr = EncryptUtils.MD5Encrypt("indeedRpaCVOfflineSalt" + GlobleConfigurations.UserToken);
            DateTime DateNow = DateTime.Now;
            long t = (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000000;
            dicParams.Add("t", t);

            var dicParams_sort = (from k in dicParams
                                  orderby k.Key
                                  select k).ToDictionary(kv => kv.Key, kv => kv.Value ?? "");
            StringBuilder sb_salt = new StringBuilder();
            List<KeyValuePair<string, object>> sgin_params = new List<KeyValuePair<string, object>>();
            string signSourceStr = "{0}&{1}&{2}";
            //item.Postdata = dicParams_sort.ToJson();
            foreach (var p in dicParams_sort)
            {
                //try
                //{
                sb_salt.Append(saltStr);


                //if (p.Value != null)
                //{
                if (p.Value.GetType().IsArray)
                {
                    continue;
                }
                if (!(p.Value is string) && p.Value.GetType().IsClass)
                {
                    continue;
                }
                sgin_params.Add(p);
                NLogger.Default.Debug($"参与签名参数：【{p.Key.ToUpper()}】{(p.Value.ToString().Length > 20 ? p.Value.ToString().Substring(0, 20) : p.Value.ToString())}");
            }

            NLogger.Default.Debug(sb_salt);
            string paramsStr = string.Join("&", sgin_params.Select(o => $"{o.Key.ToUpper()}={(o.Value ?? "")}"));
            NLogger.Default.Debug($"{uri.AbsolutePath}{Environment.NewLine}{sb_salt}{Environment.NewLine}{paramsStr}");
            signSourceStr = string.Format(signSourceStr, uri.AbsolutePath, sb_salt.ToString(), paramsStr);
            string sign = EncryptUtils.MD5Encrypt(signSourceStr);
            dicParams_sort.Remove("t");
            var urlFormat = $"{anUrl}{(anUrl.IndexOf("?") > 0 ? "&" : "?")}sign={sign}&t={t}";
            NLogger.Default.Debug($"URL:{urlFormat}");
            return urlFormat;
        }
    }
    
}
