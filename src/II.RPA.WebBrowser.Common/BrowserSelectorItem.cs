using II.RPA.Logging;
using II.RPA.Plugin.Core.Commons;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Windows.Documents;

namespace II.RPA.WebBrowser.Common
{
    public static class BrowserSelectorItem
    {
        private static ILogger logger = Logger.OfContext();

        public static string GetAllSelectorItem(JToken jToken, string browserType)
        {
            try
            {
                List<JObject> listjObject = new List<JObject>();
                foreach (var item in jToken)
                {
                    var jobject = new JObject();
                    if (item["fullSelector"] != null)
                    {
                        var value = jToken.DeepClone();
                        string[] removeArray = new string[] { "xpath", "cssSelector", "outertext", "fullSelector" };
                        value.RemoveFields(removeArray);
                        jobject.Add("fullSelector", value);
                    }
                    if (item["type"].ToString() == "wnd" || item["type"].ToString() == "win")
                    {
                        if (browserType.ToLower() == "chrome")
                            jobject.Add("selector", "Chrome");
                        if (browserType.ToLower() == "firefox")
                            jobject.Add("selector", "FireFox");
                        if (browserType.ToLower() == "edge")
                            jobject.Add("selector", "Edge");
                        if (browserType.ToLower() == "ie")
                            jobject.Add("selector", "IE");
                        if (browserType.ToLower() == "web360se")
                            jobject.Add("selector", "Web360se");

                    }
                    else
                    {
                        jobject.Add("tagName", item["name"]);
                    }
                    if (item["xpath"] != null)
                    {
                        jobject.Add("xpath", item["xpath"]);
                    }
                    if (item["cssSelector"] != null)
                    {
                        jobject.Add("cssSelector", item["cssSelector"]);
                    }
                    if (item["outertext"] != null)
                    {
                        jobject.Add("outertext", item["outertext"]);
                    }
                    if (item["props"] != null)
                    {
                        foreach (var prop in item["props"])
                        {
                            if (jobject.Property(prop["name"].ToString()) == null)
                            {
                                if (Convert.ToBoolean(prop["accurate"]) || prop["name"].ToString().Equals("url"))
                                    jobject.Add(prop["name"].ToString(), prop["value"]);
                            }
                        }
                    }
                    listjObject.Add(jobject);
                }
                var result = JsonHelper.JsonByNewtonJson<List>(listjObject);
                return result;
            }
            catch (Exception e)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, e, "GetAllSelectorItem Error");
                return jToken.ToString();
            }

        }

        public static JToken GetAllSelectorJson(string json, string browserType, string pattern = "equal")
        {
            try
            {
                var selector = new JArray();
                if (string.IsNullOrEmpty(json))
                {

                }
                else
                {
                    var result = (JToken)JsonHelper.JsonDesByNewtonJson(json);

                    foreach (var item in result)
                    {
                        JObject jObject = new JObject();
                        if (item.Value<string>("selector") != null)
                        {
                            jObject.Add("name", "tab");
                            jObject.Add("type", "win");
                        }
                        else
                        {
                            jObject.Add("name", item.Value<string>("tagName").ToString());
                            jObject.Add("type", browserType.ToLower());
                        }
                        //jObject.Add("optionProps", BuildOptionProps(item));
                        jObject.Add("props", BuildProps(item, pattern));
                        selector.Add(jObject);
                    }
                }
                return selector;
            }
            catch (Exception e)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, e, "GetAllSelectorJson Error");
                return json;
            }
        }

        private static JToken BuildProps(JToken jToken, string pattern = "equal")
        {
            JObject jObject;
            var props = new JArray();
            if (!string.IsNullOrEmpty(jToken.Value<string>("cssSelector")) || !string.IsNullOrEmpty(jToken.Value<string>("cssSelectorEx")))
            {
                jObject = new JObject();
                jObject.Add("name", "cssSelector");
                jObject.Add("value", jToken.Value<string>("cssSelector"));
                jObject.Add("pattern", "equal");
                jObject.Add("accurate", "true");
                props.Add(jObject);
            }
            if (jToken.Value<string>("title") != null)
            {
                jObject = new JObject();
                jObject.Add("name", "title");
                jObject.Add("value", jToken.Value<string>("title"));
                if (pattern == "equal")
                    jObject.Add("pattern", "equal");
                else if (pattern == "regular")
                    jObject.Add("pattern", "regular");
                jObject.Add("accurate", "true");
                props.Add(jObject);
            }
            if (jToken.Value<string>("url") != null)
            {
                jObject = new JObject();
                jObject.Add("name", "url");
                jObject.Add("value", jToken.Value<string>("url"));
                if (pattern == "equal")
                    jObject.Add("pattern", "equal");
                else if (pattern == "regular")
                    jObject.Add("pattern", "regular");
                jObject.Add("accurate", "true");
                props.Add(jObject);
            }
            if (jToken.Value<string>("id") != null && jToken.Value<string>("src") != null)
            {
                jObject = new JObject();
                jObject.Add("name", "src");
                jObject.Add("value", jToken.Value<string>("src"));
                jObject.Add("pattern", "equal");
                jObject.Add("accurate", "true");
                props.Add(jObject);
            }
            else
            {
                if (jToken.Value<string>("id") != null)
                {
                    jObject = new JObject();
                    jObject.Add("name", "id");
                    jObject.Add("value", jToken.Value<string>("id"));
                    jObject.Add("pattern", "equal");
                    jObject.Add("accurate", "true");
                    props.Add(jObject);
                }
                if (jToken.Value<string>("src") != null)
                {
                    jObject = new JObject();
                    jObject.Add("name", "src");
                    jObject.Add("value", jToken.Value<string>("src"));
                    jObject.Add("pattern", "regular");
                    jObject.Add("accurate", "true");
                    props.Add(jObject);
                }
            }
            if (jToken.Value<string>("type") != null)
            {
                jObject = new JObject();
                jObject.Add("name", "type");
                jObject.Add("value", jToken.Value<string>("type"));
                jObject.Add("pattern", "equal");
                jObject.Add("accurate", "true");
                props.Add(jObject);
            }
            if (jToken.Value<string>("class") != null)
            {
                jObject = new JObject();
                jObject.Add("name", "class");
                jObject.Add("value", jToken.Value<string>("class").Replace(".", " "));
                jObject.Add("pattern", "contain");
                jObject.Add("accurate", "true");
                props.Add(jObject);
            }
            if (jToken.Value<string>("outertext") != null)
            {
                jObject = new JObject();
                jObject.Add("name", "outertext");
                jObject.Add("value", jToken.Value<string>("outertext"));
                if (pattern == "equal")
                    jObject.Add("pattern", "equal");
                else if (pattern == "regular")
                    jObject.Add("pattern", "regular");
                jObject.Add("accurate", "true");
                props.Add(jObject);
            }


            if (jToken.Value<string>("tag") != null)
            {
                jObject = new JObject();
                jObject.Add("name", "tag");
                jObject.Add("value", jToken.Value<string>("tag"));
                jObject.Add("pattern", "equal");
                jObject.Add("accurate", "true");
                props.Add(jObject);
            }
            if (jToken.Value<string>("name") != null)
            {
                jObject = new JObject();
                jObject.Add("name", "name");
                jObject.Add("value", jToken.Value<string>("name"));
                jObject.Add("pattern", "equal");
                jObject.Add("accurate", "true");
                props.Add(jObject);
            }
            if (jToken.Value<string>("herf") != null)
            {
                jObject = new JObject();
                jObject.Add("name", "herf");
                jObject.Add("value", jToken.Value<string>("herf"));
                jObject.Add("pattern", "equal");
                jObject.Add("accurate", "true");
                props.Add(jObject);
            }


            if (jToken.Value<string>("value") != null)
            {
                jObject = new JObject();
                jObject.Add("name", "value");
                jObject.Add("value", jToken.Value<string>("value"));
                jObject.Add("pattern", "equal");
                jObject.Add("accurate", "true");
                props.Add(jObject);
            }
            if (jToken.Value<string>("hidden") != null)
            {
                jObject = new JObject();
                jObject.Add("name", "hidden");
                jObject.Add("value", jToken.Value<string>("hidden"));
                jObject.Add("pattern", "equal");
                jObject.Add("accurate", "true");
                props.Add(jObject);
            }
            if (jToken.Value<string>("readonly") != null)
            {
                jObject = new JObject();
                jObject.Add("name", "readonly");
                jObject.Add("value", jToken.Value<string>("readonly"));
                jObject.Add("pattern", "equal");
                jObject.Add("accurate", "true");
                props.Add(jObject);
            }

            if (jToken.Value<string>("nth-child") != null)
            {
                jObject = new JObject();
                jObject.Add("name", "nth-child");
                jObject.Add("value", jToken.Value<string>("nth-child"));
                jObject.Add("pattern", "equal");
                jObject.Add("accurate", "true");
                props.Add(jObject);
            }
            //if (jToken.Value<string>("index") != null)
            //{
            //    jObject = new JObject();
            //    jObject.Add("name", "index");
            //    jObject.Add("value", jToken.Value<string>("index"));
            //    jObject.Add("pattern", "equal");
            //    props.Add(jObject);
            //}
            return props;
        }

        private static JToken BuildOptionProps(JToken jToken)
        {
            JObject jObject;
            var optionProps = new JArray();
            if (jToken.Value<string>("url") != null)
            {
                jObject = new JObject();
                jObject.Add("name", "url");
                jObject.Add("value", jToken.Value<string>("url"));
                optionProps.Add(jObject);
            }
            if (jToken.Value<string>("tag") != null)
            {
                jObject = new JObject();
                jObject.Add("name", "tag");
                jObject.Add("value", jToken.Value<string>("tag"));
                optionProps.Add(jObject);
            }
            if (jToken.Value<string>("id") != null)
            {
                jObject = new JObject();
                jObject.Add("name", "id");
                jObject.Add("value", jToken.Value<string>("id"));
                optionProps.Add(jObject);
            }
            if (jToken.Value<string>("name") != null)
            {
                jObject = new JObject();
                jObject.Add("name", "name");
                jObject.Add("value", jToken.Value<string>("name"));
                optionProps.Add(jObject);
            }
            if (jToken.Value<string>("herf") != null)
            {
                jObject = new JObject();
                jObject.Add("name", "herf");
                jObject.Add("value", jToken.Value<string>("herf"));
                optionProps.Add(jObject);
            }
            if (jToken.Value<string>("class") != null)
            {
                jObject = new JObject();
                jObject.Add("name", "class");
                jObject.Add("value", jToken.Value<string>("class"));
                optionProps.Add(jObject);
            }
            if (jToken.Value<string>("src") != null)
            {
                jObject = new JObject();
                jObject.Add("name", "src");
                jObject.Add("value", jToken.Value<string>("src"));
                optionProps.Add(jObject);
            }
            if (jToken.Value<string>("type") != null)
            {
                jObject = new JObject();
                jObject.Add("name", "type");
                jObject.Add("value", jToken.Value<string>("type"));
                optionProps.Add(jObject);
            }
            if (jToken.Value<string>("value") != null)
            {
                jObject = new JObject();
                jObject.Add("name", "value");
                jObject.Add("value", jToken.Value<string>("value"));
                optionProps.Add(jObject);
            }
            if (jToken.Value<string>("hidden") != null)
            {
                jObject = new JObject();
                jObject.Add("name", "hidden");
                jObject.Add("value", jToken.Value<string>("hidden"));
                optionProps.Add(jObject);
            }
            if (jToken.Value<string>("readonly") != null)
            {
                jObject = new JObject();
                jObject.Add("name", "readonly");
                jObject.Add("value", jToken.Value<string>("readonly"));
                optionProps.Add(jObject);
            }
            if (jToken.Value<string>("outertext") != null)
            {
                jObject = new JObject();
                jObject.Add("name", "outertext");
                jObject.Add("value", jToken.Value<string>("outertext"));
                optionProps.Add(jObject);
            }
            if (jToken.Value<string>("nth-child") != null)
            {
                jObject = new JObject();
                jObject.Add("name", "nth-child");
                jObject.Add("value", jToken.Value<string>("nth-child"));
                optionProps.Add(jObject);
            }
            if (jToken.Value<string>("index") != null)
            {
                jObject = new JObject();
                jObject.Add("name", "index");
                jObject.Add("value", jToken.Value<string>("index"));
                optionProps.Add(jObject);
            }
            return optionProps;
        }
    }
}

