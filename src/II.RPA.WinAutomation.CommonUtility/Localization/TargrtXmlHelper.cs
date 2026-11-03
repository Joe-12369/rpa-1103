using II.RPA.Logging;
using II.RPA.WinAutomation.CommonUtility.Utils;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;

namespace II.RPA.WinAutomation.CommonUtility
{
    public class TargrtXmlHelper
    {
        private static ILogger logger = Logger.OfContext();
        private static readonly TargrtXmlHelper _instance = new TargrtXmlHelper();
        private static Dictionary<XmlElement, string> keyValuePairs;
        private string CultureName = RPAEnvironment.GetInstance().CurrentLanguage.ToString().Replace("_", "-");

        private string defaultXML = @"II.RPA.WinAutomation.CommonUtility.Config.controlType.xml";

        static TargrtXmlHelper()
        {
            try
            {
                RPAEnvironment.GetInstance().LanguageChanged += (s, e) =>
                {
                    logger.LogInformation($"language changed:{e.OldLanguage}-->{e.NewLanguage}");
                    keyValuePairs.Clear();
                    _instance.CultureName = e.NewLanguage.ToString().ToString().Replace("_", "-");
                    _instance.LoadResources();
                };
                _instance.LoadResources();
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"初始化 TargrtXmlHelper 时加载 XML 失败: {ex.Message}");
            }
        }

        public static TargrtXmlHelper CreateInstance()
        {
            return _instance;
        }

        public string GetTranslatedNameByCode(string code, string text)
        {
            try
            {
                var codeArry = code.Split('/');
                code = codeArry[0].ToLower().Replace(" ", "_");

                if (codeArry.Length > 1 && !string.IsNullOrEmpty(codeArry[1]))
                {
                    var attrObj = JsonConvert.DeserializeObject(codeArry[1]);
                    var targetXes = keyValuePairs.Keys.Where(a => a.Name.ToLower() == code);
                    foreach (var txe in targetXes)
                    {
                        bool isSelect = true;
                        foreach (var attr in attrObj as JObject)
                        {
                            var attributeValue = txe.GetAttribute(attr.Key);
                            if (!string.IsNullOrEmpty(attr.Value?.ToString()) && !attributeValue.Equals(attr.Value.ToString()))
                            {
                                isSelect = false;
                                break;
                            }
                        }
                        if (isSelect)
                        {
                            return FormatTargetName(keyValuePairs[txe], text);
                        }
                    }
                }

                var targetXe = keyValuePairs.Keys.FirstOrDefault(a => a.Name.ToLower() == code);
                if (targetXe != null)
                {
                    return FormatTargetName(keyValuePairs[targetXe], text);
                }

                return FormatTargetName(code, text);
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"获取元素转译名错误:{ex.Message}");
                return FormatTargetName(code, text);
            }
        }

        public string GetTranslatedNameByCode(string code)
        {
            try
            {
                var codeArry = code.Split('/');
                code = codeArry[0].ToLower().Replace(" ", "_");

                if (codeArry.Length > 1 && !string.IsNullOrEmpty(codeArry[1]))
                {
                    var attrObj = JsonConvert.DeserializeObject(codeArry[1]);
                    var targetXes = keyValuePairs.Keys.Where(a => a.Name.ToLower() == code);
                    foreach (var txe in targetXes)
                    {
                        bool isSelect = true;
                        foreach (var attr in attrObj as JObject)
                        {
                            var attributeValue = txe.GetAttribute(attr.Key);
                            if (!string.IsNullOrEmpty(attr.Value?.ToString()) && !attributeValue.Equals(attr.Value.ToString()))
                            {
                                isSelect = false;
                                break;
                            }
                        }
                        if (isSelect)
                            return keyValuePairs[txe];
                    }
                }

                var targetXe = keyValuePairs.Keys.FirstOrDefault(a => a.Name.ToLower() == code);
                if (targetXe != null)
                    return keyValuePairs[targetXe];

                return code;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, $"获取元素转译名错误:{ex.Message}");
                return code;
            }
        }

        private void LoadResources()
        {
            logger.LogInformation("load control type resource...");

            keyValuePairs = new Dictionary<XmlElement, string>();

            if (CultureName == "zh-CN")
            {
                LoadXml();
                return;
            }

            var rm = Resource.II_RPA_Resource.ResourceManager;

            XmlDocument doc = new XmlDocument();
            var asm = Assembly.GetExecutingAssembly();
            using (var stream = asm.GetManifestResourceStream(defaultXML))
            {
                doc.Load(stream);
                var nodes = doc.LastChild.ChildNodes;
                foreach (XmlNode xn in nodes)
                {
                    if (xn is XmlElement xe)
                    {
                        string key = $"controlType_ControlType_{xe.Name}";
                        string val = rm.GetString(key);
                        if (string.IsNullOrEmpty(val))
                        {
                            val = xe.InnerText;
                        }
                        keyValuePairs[xe] = val;
                    }
                }
            }
        }

        private void LoadXml()
        {
            keyValuePairs = new Dictionary<XmlElement, string>();
            XmlDocument doc = new XmlDocument();
            Assembly asm = Assembly.GetExecutingAssembly();
            using (Stream sm = asm.GetManifestResourceStream(defaultXML))
            {
                doc.Load(sm);
                XmlNode xmlNode = doc.LastChild;
                XmlNodeList nodes = xmlNode.ChildNodes;
                foreach (XmlNode xn in nodes)
                {
                    XmlElement xe = (XmlElement)xn;
                    if (!keyValuePairs.ContainsKey(xe))
                        keyValuePairs[xe] = xe.InnerText;
                }
            }
        }

        private static string FormatTargetName(string baseName, string text)
        {
            if (string.IsNullOrEmpty(text))
                return baseName;

            if (baseName.Contains("_"))
            {
                var k = baseName.Split('_');
                return k[0] + $"_{text}";
            }
            return baseName + $"_{text}";
        }
    }
}

