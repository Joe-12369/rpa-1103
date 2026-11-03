using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.WinAutomation.CommonUtility
{
    public class RecoverViewData
    {
        private static ILogger logger = Logger.OfContext();

        private static JArray CssSelectorParse(string cssSelector, string outerText)
        {
            string[] eleSelector = cssSelector.Split('>');
            JArray array = new JArray();
            for (int i = 0; i < eleSelector.Length; i++)
            {
                JObject nodes = null;
                if (i == eleSelector.Length - 1)
                    nodes = ExtractViewData.Resolver(eleSelector[i].ToString(), outerText);
                else
                    nodes = ExtractViewData.Resolver(eleSelector[i].ToString());
                JObject prop = new JObject();
                foreach (var item in nodes)
                {
                    if (item.Key == "parentId")
                        prop.Add("id", item.Value);
                    else
                        prop.Add(item.Key, item.Value);
                }
                array.Add(prop);
            }
            return array;
        }
        public static string RestoreViewData(string selector)
        {
            string restore = selector;
            try
            {
                var array = JArray.Parse(selector);
                var @object = array[0].ToObject<JObject>();
                if (@object.ContainsKey("app"))
                {
                    JArray prop = new JArray();
                    JObject parent = new JObject();
                    var appobj = new JObject();
                    string outertext = string.Empty;
                    string index = string.Empty;
                    string webType = string.Empty;
                    bool isExistParentId = false;
                    foreach (JProperty item in @object.Properties())
                    {
                        if (!string.IsNullOrEmpty(item.Name))
                        {
                            if (item.Name == "app")
                            {
                                foreach (JProperty child in item.Value)
                                {
                                    if (child.Name == "subType")
                                    {
                                        webType = child.Value.ToString();
                                        appobj.Add("selector", child.Value);
                                    }
                                    else
                                    {
                                        if (child.Name != "type" && child.Name != "name")
                                            appobj.Add(child.Name, child.Value);
                                    }
                                }
                                prop.Add(appobj);
                            }
                            else if (item.Name == "iframe")
                            {
                                JObject obj = null;
                                foreach (var a in item.Value)
                                {
                                    obj = new JObject();
                                    foreach (JProperty child in a)
                                    {
                                        obj.Add(child.Name, child.Value);
                                    }
                                    prop.Add(obj);
                                }
                            }
                            else if (item.Name == "webCtrl")
                            {
                                foreach (JProperty child in item.Value)
                                {
                                    if (child.Name == "tagName")
                                    {
                                        parent.Add("tagName", child.Value);
                                    }
                                    if (child.Name == "parentId" || child.Name == "id")
                                    {
                                        isExistParentId = true;
                                        parent.Add("id", child.Value);
                                    }
                                    if (child.Name == "class")
                                    {
                                        parent.Add("class", child.Value);
                                    }
                                    if (child.Name == "aaname")
                                    {
                                        outertext = child.Value.ToString();
                                    }
                                    if (child.Name == "index")
                                    {
                                        index = child.Value.ToString();
                                    }
                                    if (child.Name == "backup")
                                    {
                                        var jarry = child.Value.ToArray<JToken>();
                                        foreach (var items in child.Value)
                                        {
                                            foreach (JProperty ex in items)
                                            {
                                                if (ex.Name == "selector")
                                                    appobj.Add("cssSelectorEx", ex.Value);
                                                if (ex.Name == "aaname")
                                                    appobj.Add("outertextEx", ex.Value);
                                                if (ex.Name == "index")
                                                    appobj.Add("indexEx", ex.Value);
                                            }
                                        }
                                    }
                                }
                                if (parent.Count > 0)
                                    prop.Add(parent);
                                foreach (JProperty child in item.Value)
                                {
                                    if (child.Name == "selector")
                                    {
                                        string cssSelector = string.Empty;

                                        if (!string.IsNullOrEmpty(index))
                                        {
                                            if (string.IsNullOrEmpty(cssSelector))
                                            {
                                                if (webType.ToLower() == "ie")
                                                    cssSelector = child.Value.ToString() + $":nth-of-type({Convert.ToInt32(index)})";
                                                else
                                                    cssSelector = child.Value.ToString() + $":nth-child({Convert.ToInt32(index)})";
                                            }
                                            else
                                            {
                                                if (webType.ToLower() == "ie")
                                                    cssSelector += $":nth-of-type({Convert.ToInt32(index)})";
                                                else
                                                    cssSelector += $":nth-child({Convert.ToInt32(index)})";
                                            }
                                        }
                                        if (string.IsNullOrEmpty(index))
                                            cssSelector = child.Value.ToString();
                                        if (webType.ToLower() == "chrome" || webType.ToLower() == "firefox" || webType.ToLower() == "edge" || webType.ToLower() == "web360se" || webType.ToLower() == "ie" && !isExistParentId)
                                        {
                                            var cssArray = cssSelector.Split('>');
                                            if (cssArray.FirstOrDefault() == "body" || cssArray.FirstOrDefault() == " body")
                                            {
                                                cssSelector = string.Join(">", cssArray.Skip(1));
                                            }
                                        }


                                        var nodes = CssSelectorParse(cssSelector, outertext);
                                        foreach (var node in nodes)
                                        {
                                            var objnode = new JObject();
                                            foreach (JProperty ele in node)
                                            {
                                                objnode.Add(ele.Name, ele.Value);
                                            }
                                            if (objnode.Count > 0)
                                                prop.Add(objnode);
                                        }
                                    }
                                }
                            }
                            else if (item.Name == "winCtrl")
                            {
                                JObject obj = null;
                                foreach (var a in item.Value)
                                {
                                    obj = new JObject();
                                    foreach (JProperty child in a)
                                    {
                                        obj.Add(child.Name, child.Value);
                                    }
                                    prop.Add(obj);
                                }
                            }
                            else if (item.Name == "javaCtrl")
                            {
                                JObject obj = null;
                                foreach (var a in item.Value)
                                {
                                    obj = new JObject();
                                    foreach (JProperty child in a)
                                    {
                                        obj.Add(child.Name, child.Value);
                                    }
                                    prop.Add(obj);
                                }
                            }
                            else if (item.Name == "custom")
                            {
                                foreach (JProperty child in item.Value)
                                {
                                    if (child.Name == "selector")
                                        appobj.Add("cssSelector", child.Value);
                                    else if (child.Name == "aaname")
                                        appobj.Add("outertext", child.Value);
                                    else
                                    {
                                        int value = 0;
                                        int.TryParse(child.Value.ToString(), out value);
                                        appobj.Add(child.Name, value);
                                    }
                                }
                            }
                            else if (item.Name == "extraInfo")
                            {
                                foreach (JProperty child in item.Value)
                                {
                                    if (child.Name == "fullSelector")
                                        appobj.Add("fullSelector", child.Value);
                                }
                            }
                        }
                    }
                    restore = prop.ToString();
                    return restore;
                }
                else
                {
                    return selector;
                }
            }
            catch (Exception e)
            {
                logger.LogWarning(e.Message, "数据格式错误");
                return selector;
            }
        }
    }
}
