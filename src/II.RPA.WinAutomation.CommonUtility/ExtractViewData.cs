using II.RPA.Logging;
using II.RPA.WinAutomation.CommonUtility;
using II.RPA.WinAutomation.CommonUtility.Parse;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;
using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;

namespace II.RPA.WinAutomation.CommonUtility
{

    public class ElementDetail// categoryDetail
    {
        [JsonProperty("categoryIcon")]
        public string CategoryIcon { get; set; } = "";
        public string CategoryIconFile { get; set; } = "";
        [JsonProperty("categoryName")]
        public string CategoryName { get; set; } = "";
        [JsonProperty("elementName")]
        public string ElementName { get; set; } = "";
        [JsonProperty("elementId")]
        public string ElementId { get; set; } = "";
        [JsonProperty("windowName")]
        public string WindowName { get; set; } = "CV";
        [JsonProperty("mobilePackageName")]
        public string MobilePackageName { get; set; } = "";
        [JsonProperty("mobileActivityName")]
        public string MobileActivityName { get; set; } = "";
    }
    public class RPACommon
    {
        [JsonProperty("commonProps")]
        public CommonProps CommonProps { get; set; } = new CommonProps();
        [JsonProperty("selector")]
        public JArray Selector { get; set; }
        [JsonProperty("elementDetail")]
        public ElementDetail ElementDetail { get; set; } = new ElementDetail();
    }
    public class CommonProps
    {
        public string GUIType { get; set; } = "";
        public string filePath { get; set; } = "";
        public int captureMode { get; set; } = 0;
        [JsonProperty("flag")]

        public string Flag { get; set; } = "";
        /// <summary>
        /// 先这样赋值1.6吧，后续其他人在代码归并之前，调整 【GlobleConfigurations.CurrentSelectorVersion】时切记保持同步
        /// </summary>
        [JsonProperty("version")]
        public string Version { get; set; } = "1.6";
        [JsonProperty("isfindbyindex")]
        public string isFindbyIndex { get; set; } = "false";
        [JsonProperty("resDirPath")]
        public string ResDirPath { get; set; } = string.Empty;
        [JsonProperty("issmartcontrol")]
        public string isSmartControl { get; set; } = "false";
        [JsonProperty("queryMode")]
        public string QueryMode { get; set; } = "default"; //default 、 cssSelector  、Xpath、fullXpath(独立xpath查找，和获取元素对象中的xpath不同)
        [JsonProperty("cssSelector")]
        public string CssSelector { get; set; } = "";
        [JsonProperty("processName")]
        public string ProcessName { get; set; } = "";
        [JsonProperty("modifyTime")]
        public string ModifyTime { get; set; } = string.Empty;
        [JsonProperty("extendedTags")]
        public ExtendedTags ExtendedTags { get; set; } = ExtendedTags.None;
        [JsonProperty("discernHidden")]
        public string DiscernHidden { get; set; } = "true";

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "pinPath")]
        public PinPath PinPath { get; set; }
        [JsonProperty("xpathSelector")]
        public string XpathSelector { get; set; } = "";
        [JsonProperty("desc")]
        public string AiDesc { get; set; } = "";
        public string GetRunUniqueID()
        {
            if (!string.IsNullOrEmpty(this.ResDirPath))
            {
                return this.ResDirPath.Substring(this.ResDirPath.LastIndexOf('\\') + 1);
            }
            return string.Empty;
        }
        public long GetModifyTime()
        {
            return long.TryParse(this.ModifyTime, out long longTimeStamp) ? longTimeStamp : 0;
        }
    }
    public class PinPath
    {
        [JsonProperty("mainWindowHandle")]
        public long MainWindowHandle { get; set; }
        [JsonProperty("tabId")]
        public int TabId { get; set; } = -1;
        [JsonProperty("guiType")]
        public eGuiType GuiType { get; set; }
        [JsonProperty("userDefinedID")]
        public string UserDefinedID { get; set; }
    }
    /// <summary>
    /// CommonProps 内使用的selector扩展标记信息，注意不要修改老版本已定的值，否则这可能会造成兼容问题，新增项请注释版本
    /// </summary>
    [Flags]
    public enum ExtendedTags
    {
        None = 0,
        /// <summary>
        /// 内嵌IE元素，700sp3与710版本增加
        /// </summary>
        EmbeddedIE = 1 << 0,
        /// <summary>
        /// 固定路径
        /// </summary>
        PinPath = 1 << 1,
        /// <summary>
        /// 窗口类型元素
        /// </summary>
        WindowSelectorType = 1 << 2,
        /// <summary>
        /// 循环相似元素列表返回的临时元素，禁止截图恢复及AI增强
        /// </summary>
        SimilarElementItem = 1 << 3

    }
    public class ExtractViewData
    {
        private static ILogger logger = Logger.OfContext();
        private const string WIN32_PROCESS_FIELD_NAME = "Name";
        private const string WIN32_PROCESS_FIELD_EXECUTABLEPATH = "ExecutablePath";

        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern bool QueryFullProcessImageName([In] IntPtr hProcess, [In] uint dwFlags, [Out] StringBuilder lpExeName, [In, Out] ref uint lpdwSize);
        [HandleProcessCorruptedStateExceptions]
        private static string GetMainModuleFileName(IntPtr processHandle, int buffer = 1024)
        {
            var fileNameBuilder = new StringBuilder(buffer);
            uint bufferLength = (uint)fileNameBuilder.Capacity + 1;
            return QueryFullProcessImageName(processHandle, 0, fileNameBuilder, ref bufferLength) ?
                fileNameBuilder.ToString() :
                null;
        }
        [HandleProcessCorruptedStateExceptions]
        public static string GetMainModuleFilepath(int processId, out string pName)
        {
            pName = string.Empty;
            // win32 api
            // https://stackoverflow.com/questions/5497064/how-to-get-the-full-path-of-running-process
            try
            {
                var handle = Process.GetProcessById(processId).Handle;
                var mainModuleFileName = GetMainModuleFileName(handle);
                if (!string.IsNullOrEmpty(mainModuleFileName))
                {
                    pName = Path.GetFileName(mainModuleFileName);
                    return mainModuleFileName;
                }
                else
                {
                    logger.LogError("");
                    pName = string.Empty;
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning($"使用QueryFullProcessImageName获取路径异常，将尝试使用WMI");
                Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, $"使用QueryFullProcessImageName获取路径异常，将尝试使用WMI");
            }
            // wmi api 
            try
            {
                string wmiQueryString = "SELECT ProcessId, ExecutablePath,Name FROM Win32_Process WHERE ProcessId = " + processId;
                using (var searcher = new ManagementObjectSearcher(wmiQueryString))
                {
                    using (var results = searcher.Get())
                    {
                        ManagementObject mo = results.Cast<ManagementObject>().FirstOrDefault();
                        if (mo != null)
                        {
                            pName = (string)mo[WIN32_PROCESS_FIELD_NAME];
                            return (string)mo[WIN32_PROCESS_FIELD_EXECUTABLEPATH];
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning($"使用WMI获取路径异常，获取应用地址失败");
                Microsoft.Extensions.Logging.LoggingExtensions.LogDebug(logger, ex, $"使用WMI获取路径异常，将尝试使用WMI");
            }
            return string.Empty;
        }
        public static string ExtractCommonProps(string selector)
        {
            if (!string.IsNullOrEmpty(selector))
            {
                JToken t = JToken.Parse(selector);
                JObject obj = null;
                JArray arr = null;
                if (t.Type == JTokenType.Object)
                {
                    obj = JObject.Parse(selector);
                    if (obj.TryGetValue("commonProps", StringComparison.InvariantCulture, out JToken token))
                    {
                        var common = JsonConvert.DeserializeObject<RPACommon>(selector);
                        return JsonConvert.SerializeObject(common.CommonProps);
                    }
                }
                else if (t.Type == JTokenType.Array)
                {
                    arr = JArray.Parse(selector);
                    if (arr.Contains("commonProps"))
                    {
                        var common = JsonConvert.DeserializeObject<RPACommon>(selector);
                        return common.Selector.ToString();
                    }
                }
            }
            return selector;
        }
        public static string ExtractSelectors(string selector)
        {
            return ExtractSelectors(selector, (guiTypeStr) => false);
        }
        public static string ExtractSelectors(string selector, bool keepUncheckNode)
        {
            return ExtractSelectors(selector, (guiTypeStr) => keepUncheckNode);
        }
        public static string ExtractSelectors(string selector, Func<string, bool> keepUncheckNodeFunc)
        {
            try
            {
                if (!string.IsNullOrEmpty(selector))
                {
                    JToken t = JToken.Parse(selector);
                    JObject obj = null;
                    JArray arr = null;
                    if (t.Type == JTokenType.Object)
                    {
                        obj = JObject.Parse(selector);
                        if (obj.TryGetValue("commonProps", StringComparison.InvariantCulture, out JToken token))
                        {
                            var common = JsonConvert.DeserializeObject<RPACommon>(selector);
                            #region 删除第一级节点 Accurate="false"的节点
                            var secProps = JsonConvert.DeserializeObject<List<SelectorProps>>(common.Selector.ToString());

                            bool keepUncheckNode = keepUncheckNodeFunc.Invoke(t?["commonProps"]?["GUIType"]?.ToString());
                            if (!keepUncheckNode)
                            {
                                for (int i = secProps.Count - 1; i >= 0; i--)
                                {
                                    if (!string.IsNullOrEmpty(secProps[i].Accurate))
                                    {
                                        if (secProps[i].Accurate.ToLower().Equals("false"))
                                        {
                                            secProps.Remove(secProps[i]);
                                        }
                                    }
                                }
                            }
                            #endregion
                            return JsonConvert.SerializeObject(secProps);
                            //return common.Selector.ToString();
                        }
                    }
                    else if (t.Type == JTokenType.Array)
                    {
                        arr = JArray.Parse(selector);
                        if (arr.Value<string>("commonProps") != null)
                        {
                            var common = JsonConvert.DeserializeObject<RPACommon>(selector);
                            return common.Selector.ToString();
                        }
                        else
                        {
                            if (arr.Value<string>("app") != null && arr.Value<string>("webCtrl") != null)
                            {
                                return selector;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
            }
            return selector;
        }
        public static bool SelectIsFindbyIndex(string selector, out CommonProps commonProps)
        {
            commonProps = null;
            try
            {
                if (!string.IsNullOrEmpty(selector))
                {
                    var common = ExtractCommonProps(selector);
                    commonProps = JsonConvert.DeserializeObject<CommonProps>(common);
                    if (!string.IsNullOrEmpty(commonProps.isFindbyIndex))
                    {
                        bool.TryParse(commonProps.isFindbyIndex, out bool result);
                        return result;
                    }
                }
            }
            catch (Exception e)
            {

            }
            return false;
        }
        public static string SelectorCssSelector(string selector)
        {
            try
            {
                var common = ExtractCommonProps(selector);
                var props = JsonConvert.DeserializeObject<CommonProps>(common);

                if (props.QueryMode == "cssSelector")
                    return props.CssSelector;
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }
        public static string GetQueryModeSelector(string selector, out string queryMode)
        {
            try
            {
                var common = ExtractCommonProps(selector);
                var props = JsonConvert.DeserializeObject<CommonProps>(common);
                queryMode = props.QueryMode;
                if (props.QueryMode == "cssSelector")
                    return props.CssSelector;
                if (props.QueryMode == "xpath")
                    return props.CssSelector;
                if (props.QueryMode == "fullXpath")
                {
                    //如果为 fullXpath 查询模式时，传来为空直接返回元素查找失败异常
                    if (string.IsNullOrEmpty(props.XpathSelector))
                    {
                        throw new RPAException(eErrorMessageType.Error_NotFoundElement);
                    }
                    return props.XpathSelector;
                }
                return null;
            }
            catch (RPAException rpxEx)
            {
                throw rpxEx;
            }
            catch (Exception)
            {
                queryMode = "default";
                return null;
            }
        }
        public static string SelectCommonProps(string selector, out string selectorType, out string version)
        {
            return SelectCommonProps(selector, out selectorType, out version, out _);
        }

        public static string SelectCommonProps(string selector, out string selectorType, out string version, out string processName)
        {
            return SelectCommonProps(selector, out selectorType, out version, out processName, out _);
        }
        public static string SelectCommonProps(string selector, out string selectorType, out string version, out string processName, out ExtendedTags extendedTags)
        {
            return SelectCommonProps(selector, out selectorType, out version, out processName, out extendedTags, out _);
        }
        public static string SelectCommonProps(string selector, out string selectorType, out string version, out string processName, out ExtendedTags extendedTags, out string discernHidden)
        {
            return SelectCommonProps(selector, out selectorType, out version, out processName, out extendedTags, out discernHidden, out _);
        }
        public static string SelectCommonProps(string selector, out string selectorType, out string version, out string processName, out ExtendedTags extendedTags, out string discernHidden, out CommonProps commonProps)
        {
            selectorType = "";
            version = "0.0";
            processName = string.Empty;
            extendedTags = ExtendedTags.None;
            discernHidden = "false";
            commonProps = null;
            try
            {
                if (!string.IsNullOrEmpty(selector))
                {
                    if (selector.Contains("app") && selector.Contains("webCtrl") || selector.Contains("winCtrl"))
                    {
                        version = "0.0";
                        return selector;
                    }
                    try
                    {
                        JArray j = JArray.Parse(selector);
                        if (j[0]["name"].ToString() == "tab")
                        {
                            version = "1.0";
                            return selector;
                        }
                    }
                    catch (Exception)
                    {

                    }

                    var common = ExtractCommonProps(selector);
                    try
                    {
                        var props = JsonConvert.DeserializeObject<CommonProps>(common);
                        selectorType = props.GUIType.ToString();
                        version = props.Version.ToString();
                        processName = props.ProcessName;
                        extendedTags = props.ExtendedTags;
                        discernHidden = props.DiscernHidden;
                        commonProps = props;
                        return selector;
                    }
                    catch (Exception)
                    {

                    }
                }
            }
            catch (Exception e)
            {
                selectorType = "";
                version = "0.0";
            }
            return selector;
        }
        public static string AddElementDetail(string selector)
        {

            return null;
        }
        public static string AddCommonProps(string selector, string type)
        {
            RPACommon common = new RPACommon();
            common.CommonProps = new CommonProps();
            common.CommonProps.GUIType = type;
            common.Selector = string.IsNullOrEmpty(selector.ToString()) ? new JArray() : JArray.Parse(selector);
            return JsonConvert.SerializeObject(common);
        }
        public static string AddCommonProps(string selector, eGuiType type, string ProcessFullpath, string specialType = null, string cssSelector = null, string filePath = null, string processName = null)
        {
            return SetCommonPropsAndDetail(selector, type, ProcessFullpath, specialType, cssSelector, filePath, processName).ToJson();
        }
        public static RPACommon SetCommonPropsAndDetail(string selector, eGuiType type, string ProcessFullpath, string specialType = null, string cssSelector = null, string filePath = null, string processName = null)
        {
            RPACommon common = new RPACommon();
            common.CommonProps = new CommonProps();
            common.CommonProps.GUIType = type.ToString();
            if (!string.IsNullOrEmpty(cssSelector))
                common.CommonProps.CssSelector = cssSelector;
            common.Selector = string.IsNullOrEmpty(selector.ToString()) ? new JArray() : JArray.Parse(selector);
            if (!string.IsNullOrEmpty(filePath))
                common.CommonProps.filePath = filePath;
            if (!string.IsNullOrEmpty(processName))
                common.CommonProps.ProcessName = processName;
            try
            {
                var lastTagName = common.Selector.Last["name"].ToString();
                if (type == eGuiType.Chrome || type == eGuiType.Edge || type == eGuiType.Web360se || type == eGuiType.FireFox || type == eGuiType.ZExplorer || type == eGuiType.IE || type == eGuiType.Sogou)
                {
                    var firstProps = common.Selector.First["props"];
                    foreach (var item in JArray.Parse(firstProps.ToString()))
                    {
                        if (item.Value<string>("name") == "name" || item.Value<string>("name") == "title")
                        {
                            common.ElementDetail.CategoryName = item.Value<string>("value").ToString();
                            common.ElementDetail.WindowName = item.Value<string>("value").ToString();
                            break;
                        }
                    }
                }
                else if (type == eGuiType.Windows || type == eGuiType.Java || type == eGuiType.SAP || type == eGuiType.WdtErp)
                {
                    var firstProps = common.Selector.First["props"];
                    foreach (var item in JArray.Parse(firstProps.ToString()))
                    {
                        if (item.Value<string>("name") == "name" && item.Value<string>("value") == "Program Manager" || item.Value<string>("name") == "name" && item.Value<string>("value") == "任务栏" || item.Value<string>("name") == "processname" && item.Value<string>("value") == "explorer")
                        {
                            common.ElementDetail.CategoryName = "Windows桌面".I18n("ExtractViewData_D0676E76");
                            common.ElementDetail.WindowName = "Windows桌面".I18n("ExtractViewData_D0676E76");
                            break;
                        }
                    }
                    if (string.IsNullOrEmpty(common.ElementDetail.CategoryName))
                    {
                        common.ElementDetail.CategoryName = Path.GetFileNameWithoutExtension(ProcessFullpath) == null ? "" : Path.GetFileNameWithoutExtension(ProcessFullpath);
                        common.ElementDetail.WindowName = Path.GetFileNameWithoutExtension(ProcessFullpath) == null ? "" : Path.GetFileNameWithoutExtension(ProcessFullpath);
                    }
                }
                else if (eGuiType.Mobile == type)
                {
                    common.ElementDetail.CategoryName = "mobile";
                    common.ElementDetail.WindowName = "mobile";
                }
                else if (type == eGuiType.CV)
                {
                    common.ElementDetail.CategoryName = "智能元素".I18n("ExtractViewData_7111BF6B");
                    common.ElementDetail.WindowName = Path.GetFileNameWithoutExtension(ProcessFullpath) == null ? "" : Path.GetFileNameWithoutExtension(ProcessFullpath);
                    if (!string.IsNullOrEmpty(specialType))
                    {
                        //common.ElementDetail.ElementName = specialType;
                        common.ElementDetail.WindowName = specialType;
                    }
                }
                else
                {
                    common.ElementDetail.CategoryName = Path.GetFileNameWithoutExtension(ProcessFullpath) == null ? "" : Path.GetFileNameWithoutExtension(ProcessFullpath);
                    common.ElementDetail.WindowName = Path.GetFileNameWithoutExtension(ProcessFullpath) == null ? "" : Path.GetFileNameWithoutExtension(ProcessFullpath);
                }

                common.ElementDetail.CategoryIconFile = ProcessFullpath ?? "";
                if (string.IsNullOrEmpty(common.ElementDetail.ElementName))
                {
                    var lastProps = common.Selector.Last["props"];
                    var lastText = string.Empty;
                    var arryPropsItems = JArray.Parse(lastProps.ToString());
                    var typeName = "";
                    if (lastTagName == "input" && arryPropsItems.Any(item => item.Value<string>("name") == "type"))
                    {
                        typeName = arryPropsItems.First(item => item.Value<string>("name") == "type").Value<string>("value").ToString();
                    }
                    List<string> attributeArry = new List<string> { "text", "outertext", "innertext", "title", "name", "id", "class" };
                    foreach (var attribute in attributeArry)
                    {
                        if (arryPropsItems.Any(item => item.Value<string>("name") == attribute))
                        {
                            lastText = arryPropsItems.First(item => item.Value<string>("name") == attribute).Value<string>("value").ToString();
                            break;
                        }
                        continue;
                    }
                    if (!string.IsNullOrEmpty(lastText) && lastText.Length > 20)
                    {
                        lastText = lastText.Substring(0, 20) + "...";
                    }
                    //CV部分有点特殊处理
                    if (type == eGuiType.CV)
                    {
                        if (string.IsNullOrEmpty(lastText))
                        {
                            if (arryPropsItems.Any(item => item.Value<string>("name") == "LocationX")
                               && arryPropsItems.Any(item => item.Value<string>("name") == "LocationY"))
                            {
                                lastText = $"x{arryPropsItems.First(item => item.Value<string>("name") == "LocationX").Value<string>("value")}y{arryPropsItems.First(item => item.Value<string>("name") == "LocationY").Value<string>("value")}";
                            }
                            else if (arryPropsItems.Any(item => item.Value<string>("name") == "x")
                                && arryPropsItems.Any(item => item.Value<string>("name") == "y"))
                            {
                                lastText = $"x{arryPropsItems.First(item => item.Value<string>("name") == "x").Value<string>("value")}y{arryPropsItems.First(item => item.Value<string>("name") == "y").Value<string>("value")}";
                            }
                        }
                        common.ElementDetail.ElementName = "CV" + (!string.IsNullOrEmpty(lastText) ? $"_{lastText}" : "");
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(typeName))
                        {
                            var attrInput = JObject.FromObject(new { type = typeName });
                            lastTagName = $"{lastTagName}/{attrInput.ToString()}";
                        }
                        common.ElementDetail.ElementName = TargrtXmlHelper.CreateInstance().GetTranslatedNameByCode(lastTagName, lastText);
                    }
                }
            }
            catch (Exception)
            {
            }

            return common;
        }

        public static string RetrieveOriginSelector(string commonPropsString)
        {
            try
            {
                var RPACommon = JsonHelper.JsonDesByNewtonJson<RPACommon>(commonPropsString);
                return RPACommon.Selector.ToJson();
            }
            catch (Exception ex)
            {
                logger.LogError($"RetrieveOriginSelector Failed:{ex}");
                return string.Empty;
            }
        }

        private static string JavaViewData(JArray array)
        {
            JArray wrapper = new JArray();
            JObject node = new JObject();
            JArray javaCtrl = new JArray();
            foreach (var items in array)
            {
                if (!string.IsNullOrEmpty(items.Value<string>("selector")))
                {
                    var nodeblock = new JObject();
                    foreach (JProperty item in items)
                    {
                        if (item.Name == "selector")
                            nodeblock.Add("subType", item.Value);
                        else
                            nodeblock.Add(item.Name, item.Value);
                    }
                    node.Add("app", nodeblock);
                    continue;
                }
                javaCtrl.Add(items);
            }
            node.Add("javaCtrl", javaCtrl);
            wrapper.Add(node);
            return wrapper.ToString();
        }
        private static string WinViewData(JArray array)
        {
            JArray wrapper = new JArray();
            JObject node = new JObject();
            JArray winCtrl = new JArray();
            foreach (var items in array)
            {
                if (!string.IsNullOrEmpty(items.Value<string>("selector")))
                {
                    var nodeblock = new JObject();
                    foreach (JProperty item in items)
                    {
                        if (item.Name == "selector")
                            nodeblock.Add("subType", item.Value);
                        else
                            nodeblock.Add(item.Name, item.Value);
                    }
                    node.Add("app", nodeblock);
                    continue;
                }
                winCtrl.Add(items);
            }
            node.Add("winCtrl", winCtrl);
            wrapper.Add(node);
            return wrapper.ToString();
        }
        private static string BuildCssSelectorViewData(JToken token)
        {
            var selector = new StringBuilder();
            foreach (var item in token)
            {
                var p = item as JProperty;
                if (p.Name == "tagName")
                {
                    selector.Append(p.Value.ToString());
                }
                else if (p.Name == "id")
                {
                    selector.Append($"#{p.Value.ToString()}");
                }
                else if (p.Name == "class")
                {
                    selector.Append($".{p.Value.ToString().Replace(" ", ".")}");
                }
                else if (p.Name == "nth-child")
                {
                    selector.Append(":nth-child" + "(" + p.Value + ")");
                }
                else if (p.Name == "nth-of-type")
                {
                    selector.Append(":nth-of-type" + "(" + p.Value + ")");
                }
                else if (!string.IsNullOrEmpty(p.Name))
                {
                    selector.Append($"[{p.Name}={p.Value}]");
                }
            }
            return selector.ToString();
        }
        private static JObject FrameViewData(JToken token)
        {
            JObject jObject = new JObject();

            if (token.Value<string>("title") != null)
            {
                jObject.Add("title", token.Value<string>("title"));
            }
            if (token.Value<string>("tagName") != null)
            {
                jObject.Add("tagName", token.Value<string>("tagName"));
            }
            if (token.Value<string>("id") != null)
            {
                jObject.Add("id", token.Value<string>("id"));
            }
            if (token.Value<string>("src") != null)
            {
                jObject.Add("src", token.Value<string>("src"));
            }

            if (token.Value<string>("type") != null)
            {

                jObject.Add("type", token.Value<string>("type"));
            }
            if (token.Value<string>("class") != null)
            {
                jObject.Add("class", token.Value<string>("class").Replace(" ", "."));
            }
            if (token.Value<string>("outertext") != null)
            {
                jObject.Add("outertext", token.Value<string>("outertext"));
            }
            if (token.Value<string>("name") != null)
            {
                jObject.Add("name", token.Value<string>("name"));
            }
            if (token.Value<string>("herf") != null)
            {
                jObject.Add("herf", token.Value<string>("herf"));
            }
            if (token.Value<string>("value") != null)
            {
                jObject.Add("value", token.Value<string>("value"));
            }
            if (token.Value<string>("hidden") != null)
            {
                jObject.Add("hidden", token.Value<string>("hidden"));
            }
            if (token.Value<string>("readonly") != null)
            {
                jObject.Add("readonly", token.Value<string>("readonly"));
            }

            if (token.Value<string>("nth-child") != null)
            {
                jObject.Add("nth-child", token.Value<string>("nth-child"));

            }
            if (token.Value<string>("nth-of-type") != null)
            {
                jObject.Add("nth-child", token.Value<string>("nth-of-type"));
            }
            if (token.Value<string>("index") != null)
            {
                jObject.Add("index", token.Value<string>("index"));
            }
            return jObject;
        }
        private static string FireFoxViewData(JArray array, bool isSelectorAll = true)
        {
            JArray wrapper = new JArray();
            JObject node = new JObject();
            JObject custom = new JObject();
            JArray frameArray = new JArray();
            JObject webCtrlnode = new JObject();
            JObject cssAll = new JObject();
            JArray backup = new JArray();
            JObject extraInfo = new JObject();
            List<string> selector = new List<string>();
            int num = 0;
            int level = 0;
            for (int i = 0; i < array.Count; i++)
            {
                if (array[i].Value<string>("tagName") != null || array[i].Value<string>("tagName") != null)
                    if (array[i].Value<string>("tagName").ToLower() == "iframe" || array[i].Value<string>("tagName").ToLower() == "frame")
                    {
                        num = i;
                    }
            }

            foreach (var item in array)
            {
                if (!string.IsNullOrEmpty(item.Value<string>("selector")))
                {
                    var nodeblock = new JObject();
                    nodeblock.Add("name", "firefox.exe");
                    nodeblock.Add("subType", item.Value<string>("selector"));
                    nodeblock.Add("type", "html");
                    if (isSelectorAll)
                    {
                        cssAll.Add("selector", item.Value<string>("cssSelector") ?? "");
                        custom.Add("selector", item.Value<string>("cssSelector") ?? "");
                        if (!string.IsNullOrEmpty(item.Value<string>("outertext")))
                        {
                            cssAll.Add("aaname", item.Value<string>("outertext") ?? "");
                            custom.Add("aaname", item.Value<string>("outertext") ?? "");
                        }
                    }
                    extraInfo.Add("xpath", item.Value<string>("xpath") ?? "");
                    extraInfo.Add("fullSelector", item.Value<JToken>("fullSelector") ?? "");
                    nodeblock.Add("title", item.Value<string>("title") ?? "");
                    nodeblock.Add("url", item.Value<string>("url") ?? "");
                    nodeblock.Add("filePath", item.Value<string>("filePath") ?? "");
                    if (!string.IsNullOrEmpty(item.Value<string>("remoteAddress")))
                    {
                        nodeblock.Add("remoteName", item.Value<string>("remoteName") ?? "");
                        nodeblock.Add("remoteAddress", item.Value<string>("remoteAddress") ?? "");
                    }
                    if (!string.IsNullOrEmpty(item.Value<string>("captureMode")))
                    {
                        nodeblock.Add("captureMode", item.Value<string>("captureMode") ?? "");
                    }

                    node.Add("app", nodeblock);
                }
                else if (!string.IsNullOrEmpty(item.Value<string>("tagName")))
                {
                    if (level <= num && num != 0)
                    {
                        frameArray.Add(FrameViewData(item));
                    }
                    else
                    {
                        selector.Add(BuildCssSelectorViewData(item));
                    }
                }
                level++;
            }

            if (selector.Count > 0)
            {
                if (selector[0].Contains("#"))
                {
                    var value = selector[0];
                    webCtrlnode = Resolver(value);
                    if (selector.Count == 1)
                    {
                        JObject j = new JObject();
                        foreach (var item in webCtrlnode)
                        {
                            if (item.Key == "parentId")
                            {
                                j.Add("id", item.Value);
                            }
                            else
                            {
                                j.Add(item.Key, item.Value);
                            }
                        }
                        webCtrlnode = j;
                    }
                    selector.Remove(selector[0]);
                }
                if (selector.Count > 0)
                {
                    if (selector[selector.Count - 1].Contains("outertext"))
                    {
                        var value = selector[selector.Count - 1];
                        var tag = value.Substring(0, value.IndexOf("["));
                        var text = value.Substring(value.IndexOf("["), value.LastIndexOf("]") - value.IndexOf("[")).Replace("[outertext=", "");
                        if (!string.IsNullOrEmpty(text))
                            webCtrlnode.Add("aaname", text);
                        int index = -1;
                        if (value.Contains("nth-child"))
                        {
                            index = Convert.ToInt32(value.Substring(value.LastIndexOf("(") + 1, value.LastIndexOf(")") - value.LastIndexOf("(") - 1));
                            if (index >= 0)
                                webCtrlnode.Add("index", index);
                        }
                        selector[selector.Count - 1] = tag;
                    }
                    else if (selector[selector.Count - 1].Contains("nth-child"))
                    {
                        var value = selector[selector.Count - 1];
                        var tag = value.Substring(0, value.IndexOf(":"));
                        int index = -1;
                        index = Convert.ToInt32(value.Substring(value.LastIndexOf("(") + 1, value.LastIndexOf(")") - value.LastIndexOf("(") - 1));
                        if (index >= 0)
                            webCtrlnode.Add("index", index);
                        selector[selector.Count - 1] = tag;
                    }

                    if (webCtrlnode.ContainsKey("parentId"))
                        webCtrlnode.Add("selector", string.Join(">", selector));
                    else if (selector[0] == "body" || selector[0] == "html")
                        webCtrlnode.Add("selector", string.Join(">", selector));
                    else
                        webCtrlnode.Add("selector", " body>" + string.Join(">", selector));
                }
            }
            if (frameArray.Count > 0)
                node.Add("iframe", frameArray);
            backup.Add(cssAll);
            if (isSelectorAll)
                webCtrlnode.Add("backup", backup);
            node.Add("webCtrl", webCtrlnode);
            if (isSelectorAll)
            {
                node.Add("custom", custom);
                node.Add("extraInfo", extraInfo);
            }
            wrapper.Add(node);
            return wrapper.ToString();
        }
        private static string ChromeViewData(JArray array, bool isSelectorAll = true)
        {
            JArray wrapper = new JArray();
            JObject node = new JObject();
            JObject custom = new JObject();
            JArray frameArray = new JArray();
            JObject webCtrlnode = new JObject();
            JObject cssAll = new JObject();
            JArray backup = new JArray();
            JObject extraInfo = new JObject();
            List<string> selector = new List<string>();
            int num = 0;
            int level = 0;
            for (int i = 0; i < array.Count; i++)
            {
                if (array[i].Value<string>("tagName") != null || array[i].Value<string>("tagName") != null)
                    if (array[i].Value<string>("tagName").ToLower() == "iframe" || array[i].Value<string>("tagName").ToLower() == "frame")
                    {
                        num = i;
                    }
            }

            foreach (var item in array)
            {
                if (!string.IsNullOrEmpty(item.Value<string>("selector")))
                {
                    var nodeblock = new JObject();
                    nodeblock.Add("name", "chrome.exe");
                    nodeblock.Add("subType", item.Value<string>("selector"));
                    nodeblock.Add("type", "html");
                    if (isSelectorAll)
                    {
                        cssAll.Add("selector", item.Value<string>("cssSelector") ?? "");
                        custom.Add("selector", item.Value<string>("cssSelector") ?? "");
                        if (!string.IsNullOrEmpty(item.Value<string>("outertext")))
                        {
                            cssAll.Add("aaname", item.Value<string>("outertext") ?? "");
                            custom.Add("aaname", item.Value<string>("outertext") ?? "");
                        }
                    }
                    extraInfo.Add("xpath", item.Value<string>("xpath") ?? "");
                    extraInfo.Add("fullSelector", item.Value<JToken>("fullSelector") ?? "");
                    nodeblock.Add("title", item.Value<string>("title") ?? "");
                    nodeblock.Add("url", item.Value<string>("url") ?? "");
                    nodeblock.Add("filePath", item.Value<string>("filePath") ?? "");
                    if (!string.IsNullOrEmpty(item.Value<string>("remoteAddress")))
                    {
                        nodeblock.Add("remoteName", item.Value<string>("remoteName") ?? "");
                        nodeblock.Add("remoteAddress", item.Value<string>("remoteAddress") ?? "");
                    }
                    if (!string.IsNullOrEmpty(item.Value<string>("captureMode")))
                    {
                        nodeblock.Add("captureMode", item.Value<string>("captureMode") ?? "");
                    }
                    node.Add("app", nodeblock);
                }
                else if (!string.IsNullOrEmpty(item.Value<string>("tagName")))
                {
                    if (level <= num && num != 0)
                    {
                        frameArray.Add(FrameViewData(item));
                    }
                    else
                    {
                        selector.Add(BuildCssSelectorViewData(item));
                    }
                }
                level++;
            }

            if (selector.Count > 0)
            {
                if (selector[0].Contains("#"))
                {
                    var value = selector[0];
                    webCtrlnode = Resolver(value);
                    if (selector.Count == 1)
                    {
                        JObject j = new JObject();
                        foreach (var item in webCtrlnode)
                        {
                            if (item.Key == "parentId")
                            {
                                j.Add("id", item.Value);
                            }
                            else
                            {
                                j.Add(item.Key, item.Value);
                            }
                        }
                        webCtrlnode = j;
                    }
                    selector.Remove(selector[0]);
                }
                if (selector.Count > 0)
                {
                    if (selector[selector.Count - 1].Contains("outertext"))
                    {
                        var value = selector[selector.Count - 1];
                        var tag = value.Substring(0, value.IndexOf("["));
                        var text = value.Substring(value.IndexOf("["), value.LastIndexOf("]") - value.IndexOf("[")).Replace("[outertext=", "");
                        if (!string.IsNullOrEmpty(text))
                            webCtrlnode.Add("aaname", text);
                        int index = -1;
                        if (value.Contains("nth-child"))
                        {
                            index = Convert.ToInt32(value.Substring(value.LastIndexOf("(") + 1, value.LastIndexOf(")") - value.LastIndexOf("(") - 1));
                            if (index >= 0)
                                webCtrlnode.Add("index", index);
                        }
                        selector[selector.Count - 1] = tag;
                    }
                    else if (selector[selector.Count - 1].Contains("nth-child"))
                    {
                        var value = selector[selector.Count - 1];
                        var tag = value.Substring(0, value.IndexOf(":"));
                        int index = -1;
                        index = Convert.ToInt32(value.Substring(value.LastIndexOf("(") + 1, value.LastIndexOf(")") - value.LastIndexOf("(") - 1));
                        if (index >= 0)
                            webCtrlnode.Add("index", index);
                        selector[selector.Count - 1] = tag;
                    }

                    if (webCtrlnode.ContainsKey("parentId"))
                        webCtrlnode.Add("selector", string.Join(">", selector));
                    else if (selector[0] == "body" || selector[0] == "html")
                        webCtrlnode.Add("selector", string.Join(">", selector));
                    else
                        webCtrlnode.Add("selector", " body>" + string.Join(">", selector));
                }
            }
            if (frameArray.Count > 0)
                node.Add("iframe", frameArray);
            backup.Add(cssAll);
            if (isSelectorAll)
                webCtrlnode.Add("backup", backup);
            node.Add("webCtrl", webCtrlnode);
            if (isSelectorAll)
            {
                node.Add("custom", custom);
                node.Add("extraInfo", extraInfo);
            }
            wrapper.Add(node);
            return wrapper.ToString();
        }
        private static string EdgeViewData(JArray array, bool isSelectorAll = true)
        {
            JArray wrapper = new JArray();
            JObject node = new JObject();
            JObject custom = new JObject();
            JArray frameArray = new JArray();
            JObject webCtrlnode = new JObject();
            JObject cssAll = new JObject();
            JArray backup = new JArray();
            JObject extraInfo = new JObject();
            List<string> selector = new List<string>();
            int num = 0;
            int level = 0;
            for (int i = 0; i < array.Count; i++)
            {
                if (array[i].Value<string>("tagName") != null || array[i].Value<string>("tagName") != null)
                    if (array[i].Value<string>("tagName").ToLower() == "iframe" || array[i].Value<string>("tagName").ToLower() == "frame")
                    {
                        num = i;
                    }
            }

            foreach (var item in array)
            {
                if (!string.IsNullOrEmpty(item.Value<string>("selector")))
                {
                    var nodeblock = new JObject();
                    nodeblock.Add("name", "msedge.exe");
                    nodeblock.Add("subType", item.Value<string>("selector"));
                    nodeblock.Add("type", "html");
                    if (isSelectorAll)
                    {
                        cssAll.Add("selector", item.Value<string>("cssSelector") ?? "");
                        custom.Add("selector", item.Value<string>("cssSelector") ?? "");
                        if (!string.IsNullOrEmpty(item.Value<string>("outertext")))
                        {
                            cssAll.Add("aaname", item.Value<string>("outertext") ?? "");
                            custom.Add("aaname", item.Value<string>("outertext") ?? "");
                        }
                    }
                    extraInfo.Add("xpath", item.Value<string>("xpath") ?? "");
                    extraInfo.Add("fullSelector", item.Value<JToken>("fullSelector") ?? "");
                    nodeblock.Add("title", item.Value<string>("title") ?? "");
                    nodeblock.Add("url", item.Value<string>("url") ?? "");
                    nodeblock.Add("filePath", item.Value<string>("filePath") ?? "");
                    if (!string.IsNullOrEmpty(item.Value<string>("remoteAddress")))
                    {
                        nodeblock.Add("remoteName", item.Value<string>("remoteName") ?? "");
                        nodeblock.Add("remoteAddress", item.Value<string>("remoteAddress") ?? "");
                    }
                    if (!string.IsNullOrEmpty(item.Value<string>("captureMode")))
                    {
                        nodeblock.Add("captureMode", item.Value<string>("captureMode") ?? "");
                    }
                    node.Add("app", nodeblock);
                }
                else if (!string.IsNullOrEmpty(item.Value<string>("tagName")))
                {
                    if (level <= num && num != 0)
                    {
                        frameArray.Add(FrameViewData(item));
                    }
                    else
                    {
                        selector.Add(BuildCssSelectorViewData(item));
                    }
                }
                level++;
            }

            if (selector.Count > 0)
            {
                if (selector[0].Contains("#"))
                {
                    var value = selector[0];
                    webCtrlnode = Resolver(value);
                    if (selector.Count == 1)
                    {
                        JObject j = new JObject();
                        foreach (var item in webCtrlnode)
                        {
                            if (item.Key == "parentId")
                            {
                                j.Add("id", item.Value);
                            }
                            else
                            {
                                j.Add(item.Key, item.Value);
                            }
                        }
                        webCtrlnode = j;
                    }
                    selector.Remove(selector[0]);
                }
                if (selector.Count > 0)
                {
                    if (selector[selector.Count - 1].Contains("outertext"))
                    {
                        var value = selector[selector.Count - 1];
                        var tag = value.Substring(0, value.IndexOf("["));
                        var text = value.Substring(value.IndexOf("["), value.LastIndexOf("]") - value.IndexOf("[")).Replace("[outertext=", "");
                        if (!string.IsNullOrEmpty(text))
                            webCtrlnode.Add("aaname", text);
                        int index = -1;
                        if (value.Contains("nth-child"))
                        {
                            index = Convert.ToInt32(value.Substring(value.LastIndexOf("(") + 1, value.LastIndexOf(")") - value.LastIndexOf("(") - 1));
                            if (index >= 0)
                                webCtrlnode.Add("index", index);
                        }
                        selector[selector.Count - 1] = tag;
                    }
                    else if (selector[selector.Count - 1].Contains("nth-child"))
                    {
                        var value = selector[selector.Count - 1];
                        var tag = value.Substring(0, value.IndexOf(":"));
                        int index = -1;
                        index = Convert.ToInt32(value.Substring(value.LastIndexOf("(") + 1, value.LastIndexOf(")") - value.LastIndexOf("(") - 1));
                        if (index >= 0)
                            webCtrlnode.Add("index", index);
                        selector[selector.Count - 1] = tag;
                    }

                    if (webCtrlnode.ContainsKey("parentId"))
                        webCtrlnode.Add("selector", string.Join(">", selector));
                    else if (selector[0] == "body" || selector[0] == "html")
                        webCtrlnode.Add("selector", string.Join(">", selector));
                    else
                        webCtrlnode.Add("selector", " body>" + string.Join(">", selector));
                }
            }
            if (frameArray.Count > 0)
                node.Add("iframe", frameArray);
            backup.Add(cssAll);
            if (isSelectorAll)
                webCtrlnode.Add("backup", backup);
            node.Add("webCtrl", webCtrlnode);
            if (isSelectorAll)
            {
                node.Add("custom", custom);
                node.Add("extraInfo", extraInfo);
            }
            wrapper.Add(node);
            return wrapper.ToString();
        }
        private static string ZExplorerViewData(JArray array)
        {
            JArray wrapper = new JArray();
            JObject node = new JObject();
            JObject custom = new JObject();
            JArray frameArray = new JArray();
            JObject webCtrlnode = new JObject();
            JObject cssAll = new JObject();
            JArray backup = new JArray();
            JObject extraInfo = new JObject();
            List<string> selector = new List<string>();
            int num = 0;
            int level = 0;
            for (int i = 0; i < array.Count; i++)
            {
                var tagName = array[i].Value<string>("tagName")?.ToLower();
                if (tagName != null && tagName.Contains("frame"))
                {
                    num = i;
                }
            }
            foreach (var item in array)
            {
                if (!string.IsNullOrEmpty(item.Value<string>("selector")))
                {
                    var nodeblock = new JObject();
                    nodeblock.Add("name", "zexplore.exe");
                    nodeblock.Add("subType", item.Value<string>("selector"));
                    nodeblock.Add("type", "html");
                    cssAll.Add("selector", item.Value<string>("cssSelector") ?? "");
                    custom.Add("selector", item.Value<string>("cssSelector") ?? "");
                    if (!string.IsNullOrEmpty(item.Value<string>("outertext")))
                    {
                        cssAll.Add("aaname", item.Value<string>("outertext") ?? "");
                        custom.Add("aaname", item.Value<string>("outertext") ?? "");
                    }
                    extraInfo.Add("xpath", item.Value<string>("xpath") ?? "");
                    nodeblock.Add("title", item.Value<string>("title") ?? "");
                    nodeblock.Add("url", item.Value<string>("url") ?? "");
                    nodeblock.Add("filePath", item.Value<string>("filePath") ?? "");
                    if (!string.IsNullOrEmpty(item.Value<string>("remoteAddress")))
                    {
                        nodeblock.Add("remoteName", item.Value<string>("remoteName") ?? "");
                        nodeblock.Add("remoteAddress", item.Value<string>("remoteAddress") ?? "");
                    }
                    if (!string.IsNullOrEmpty(item.Value<string>("captureMode")))
                    {
                        nodeblock.Add("captureMode", item.Value<string>("captureMode") ?? "");
                    }
                    node.Add("app", nodeblock);
                }
                else if (!string.IsNullOrEmpty(item.Value<string>("tagName")))
                {
                    if (level <= num && num != 0)
                    {
                        frameArray.Add(FrameViewData(item));
                    }
                    else
                    {
                        selector.Add(BuildCssSelectorViewData(item));
                    }
                }
                level++;
            }

            if (selector.Count > 0)
            {
                if (selector[0].Contains("#"))
                {
                    var value = selector[0];
                    webCtrlnode = Resolver(value);
                    if (selector.Count == 1)
                    {
                        JObject j = new JObject();
                        foreach (var item in webCtrlnode)
                        {
                            if (item.Key == "parentId")
                            {
                                j.Add("id", item.Value);
                            }
                            else
                            {
                                j.Add(item.Key, item.Value);
                            }
                        }
                        webCtrlnode = j;
                    }
                    selector.Remove(selector[0]);
                }
                if (selector[selector.Count - 1].Contains("outertext"))
                {
                    var value = selector[selector.Count - 1];
                    var tag = value.Substring(0, value.IndexOf("["));
                    var text = value.Substring(value.IndexOf("["), value.LastIndexOf("]") - value.IndexOf("[")).Replace("[outertext=", "");
                    if (!string.IsNullOrEmpty(text))
                        webCtrlnode.Add("aaname", text);
                    int index = -1;
                    if (value.Contains("nth-child"))
                    {
                        index = Convert.ToInt32(value.Substring(value.LastIndexOf("(") + 1, value.LastIndexOf(")") - value.LastIndexOf("(") - 1));
                        if (index >= 0)
                            webCtrlnode.Add("index", index);
                    }
                    selector[selector.Count - 1] = tag;
                }
                else if (selector[selector.Count - 1].Contains("nth-child"))
                {
                    var value = selector[selector.Count - 1];
                    var tag = value.Substring(0, value.IndexOf(":"));
                    int index = -1;
                    index = Convert.ToInt32(value.Substring(value.LastIndexOf("(") + 1, value.LastIndexOf(")") - value.LastIndexOf("(") - 1));
                    if (index >= 0)
                        webCtrlnode.Add("index", index);
                    selector[selector.Count - 1] = tag;
                }

                if (webCtrlnode.ContainsKey("parentId"))
                    webCtrlnode.Add("selector", string.Join(">", selector));
                else if (selector[0] == "body" || selector[0] == "html")
                    webCtrlnode.Add("selector", string.Join(">", selector));
                else
                    webCtrlnode.Add("selector", " body>" + string.Join(">", selector));
            }
            if (frameArray.Count > 0)
                node.Add("iframe", frameArray);
            backup.Add(cssAll);
            webCtrlnode.Add("backup", backup);
            node.Add("webCtrl", webCtrlnode);
            node.Add("custom", custom);
            node.Add("extraInfo", extraInfo);
            wrapper.Add(node);
            return wrapper.ToString();
        }
        private static string IEViewData(JArray array, bool isSelectorAll = true)
        {
            JArray wrapper = new JArray();
            JObject node = new JObject();
            JObject custom = new JObject();
            JArray frameArray = new JArray();
            JObject webCtrlnode = new JObject();
            JObject cssAll = new JObject();
            JArray backup = new JArray();
            JObject extraInfo = new JObject();
            List<string> selector = new List<string>();
            int num = 0;
            int level = 0;
            for (int i = 0; i < array.Count; i++)
            {
                if (array[i].Value<string>("tagName") != null || array[i].Value<string>("tagName") != null)
                    if (array[i].Value<string>("tagName").ToLower() == "iframe" || array[i].Value<string>("tagName").ToLower() == "frame")
                    {
                        num = i;
                    }
            }

            foreach (var item in array)
            {
                if (!string.IsNullOrEmpty(item.Value<string>("selector")))
                {
                    var nodeblock = new JObject();
                    nodeblock.Add("name", "iexplore.exe");
                    nodeblock.Add("subType", item.Value<string>("selector"));
                    nodeblock.Add("type", "html");
                    if (isSelectorAll)
                    {
                        cssAll.Add("selector", item.Value<string>("cssSelector") ?? "");
                        custom.Add("selector", item.Value<string>("cssSelector") ?? "");
                        if (!string.IsNullOrEmpty(item.Value<string>("outertext")))
                        {
                            cssAll.Add("aaname", item.Value<string>("outertext") ?? "");
                            custom.Add("aaname", item.Value<string>("outertext") ?? "");
                        }
                    }
                    extraInfo.Add("xpath", item.Value<string>("xpath") ?? "");
                    extraInfo.Add("fullSelector", item.Value<JToken>("fullSelector") ?? "");
                    nodeblock.Add("title", item.Value<string>("title") ?? "");
                    nodeblock.Add("url", item.Value<string>("url") ?? "");
                    nodeblock.Add("filePath", item.Value<string>("filePath") ?? "");
                    if (!string.IsNullOrEmpty(item.Value<string>("remoteAddress")))
                    {
                        nodeblock.Add("remoteName", item.Value<string>("remoteName") ?? "");
                        nodeblock.Add("remoteAddress", item.Value<string>("remoteAddress") ?? "");
                    }
                    if (!string.IsNullOrEmpty(item.Value<string>("captureMode")))
                    {
                        nodeblock.Add("captureMode", item.Value<string>("captureMode") ?? "");
                    }
                    node.Add("app", nodeblock);
                }
                else if (!string.IsNullOrEmpty(item.Value<string>("tagName")))
                {
                    if (level <= num && num != 0)
                    {
                        frameArray.Add(FrameViewData(item));
                    }
                    else
                    {
                        selector.Add(BuildCssSelectorViewData(item));
                    }
                }
                level++;
            }

            if (selector.Count > 0)
            {
                if (selector[0].Contains("#"))
                {
                    var value = selector[0];
                    webCtrlnode = Resolver(value);
                    if (selector.Count == 1)
                    {
                        JObject j = new JObject();
                        foreach (var item in webCtrlnode)
                        {
                            if (item.Key == "parentId")
                            {
                                j.Add("id", item.Value);
                            }
                            else
                            {
                                j.Add(item.Key, item.Value);
                            }
                        }
                        webCtrlnode = j;
                    }
                    selector.Remove(selector[0]);
                }
                if (selector.Count > 0)
                {
                    if (selector[selector.Count - 1].Contains("outertext"))
                    {
                        var value = selector[selector.Count - 1];
                        var tag = value.Substring(0, value.IndexOf("["));
                        var text = value.Substring(value.IndexOf("["), value.LastIndexOf("]") - value.IndexOf("[")).Replace("[outertext=", "");
                        if (!string.IsNullOrEmpty(text))
                            webCtrlnode.Add("aaname", text);
                        int index = -1;
                        if (value.Contains("nth-child"))
                        {
                            index = Convert.ToInt32(value.Substring(value.LastIndexOf("(") + 1, value.LastIndexOf(")") - value.LastIndexOf("(") - 1));
                            if (index >= 0)
                                webCtrlnode.Add("index", index);
                        }
                        selector[selector.Count - 1] = tag;
                    }
                    else if (selector[selector.Count - 1].Contains("nth-child"))
                    {
                        var value = selector[selector.Count - 1];
                        var tag = value.Substring(0, value.IndexOf(":"));
                        int index = -1;
                        index = Convert.ToInt32(value.Substring(value.LastIndexOf("(") + 1, value.LastIndexOf(")") - value.LastIndexOf("(") - 1));
                        if (index >= 0)
                            webCtrlnode.Add("index", index);
                        selector[selector.Count - 1] = tag;
                    }

                    if (webCtrlnode.ContainsKey("parentId"))
                        webCtrlnode.Add("selector", string.Join(">", selector));
                    else if (selector[0] == "body" || selector[0] == "html")
                        webCtrlnode.Add("selector", string.Join(">", selector));
                    else
                        webCtrlnode.Add("selector", " body>" + string.Join(">", selector));
                }
            }
            if (frameArray.Count > 0)
                node.Add("iframe", frameArray);
            backup.Add(cssAll);
            if (isSelectorAll)
                webCtrlnode.Add("backup", backup);
            node.Add("webCtrl", webCtrlnode);
            if (isSelectorAll)
            {
                node.Add("custom", custom);
                node.Add("extraInfo", extraInfo);
            }
            wrapper.Add(node);
            return wrapper.ToString();
        }
        //private static string IEViewData(JArray array)
        //{
        //    JArray wrapper = new JArray();
        //    JObject node = new JObject();
        //    JObject custom = new JObject();
        //    JArray frameArray = new JArray();
        //    JObject webCtrlnode = new JObject();
        //    JObject cssAll = new JObject();
        //    JArray backup = new JArray();
        //    List<string> selector = new List<string>();
        //    int num = 0;
        //    int level = 0;
        //    for (int i = 0; i < array.Count; i++)
        //    {
        //        if (array[i].Value<string>("tagName") != null || array[i].Value<string>("tagName") != null)
        //            if (array[i].Value<string>("tagName").ToLower() == "iframe" || array[i].Value<string>("tagName").ToLower() == "frame")
        //            {
        //                num = i;
        //            }
        //    }
        //    foreach (var item in array)
        //    {
        //        if (!string.IsNullOrEmpty(item.Value<string>("selector")))
        //        {
        //            var nodeblock = new JObject();
        //            nodeblock.Add("name", "iexplore.exe");
        //            nodeblock.Add("subType", item.Value<string>("selector"));
        //            nodeblock.Add("type", "html");
        //            cssAll.Add("selector", item.Value<string>("cssSelector") ?? "");
        //            custom.Add("selector", item.Value<string>("cssSelector") ?? "");
        //            if (!string.IsNullOrEmpty(item.Value<string>("outertext")))
        //            {
        //                cssAll.Add("aaname", item.Value<string>("outertext") ?? "");
        //                custom.Add("aaname", item.Value<string>("outertext") ?? "");
        //            }
        //            cssAll.Add("index", item.Value<string>("index") ?? "");
        //            custom.Add("index", item.Value<string>("index") ?? "");
        //            nodeblock.Add("title", item.Value<string>("title") ?? "");
        //            nodeblock.Add("url", item.Value<string>("url") ?? "");
        //            nodeblock.Add("filePath", item.Value<string>("filePath") ?? "");
        //            if (!string.IsNullOrEmpty(item.Value<string>("remoteAddress")))
        //            {
        //                nodeblock.Add("remoteName", item.Value<string>("remoteName") ?? "");
        //                nodeblock.Add("remoteAddress", item.Value<string>("remoteAddress") ?? "");
        //            }
        //            node.Add("app", nodeblock);
        //        }
        //        else if (!string.IsNullOrEmpty(item.Value<string>("tagName")))
        //        {
        //            if (level <= num && num != 0)
        //            {
        //                frameArray.Add(FrameViewData(item));
        //            }
        //            else
        //            {
        //                selector.Add(BuildCssSelectorViewData(item));
        //            }
        //        }
        //        level++;
        //    }

        //    if (selector.Count > 0)
        //    {
        //        if (selector[0].Contains("#"))
        //        {
        //            webCtrlnode.Add("tagName", selector[0].Substring(0, selector[0].IndexOf("#")));
        //            if (selector[0].Contains(":nth-of-type("))
        //                webCtrlnode.Add("parentId", selector[0].Substring(selector[0].IndexOf("#") + 1, selector[0].Length - selector[0].IndexOf(":") - 1));
        //            else
        //                webCtrlnode.Add("parentId", selector[0].Substring(selector[0].IndexOf("#") + 1, selector[0].Length - selector[0].IndexOf("#") - 1));
        //            selector.Remove(selector[0]);
        //        }
        //        if (selector.Count > 0)
        //        {
        //            if (selector[selector.Count - 1].Contains("outertext"))
        //            {
        //                var value = selector[selector.Count - 1];
        //                var tag = value.Substring(0, value.IndexOf("["));
        //                var text = value.Substring(value.IndexOf("["), value.LastIndexOf("]") - value.IndexOf("[")).Replace("[outertext=", "");
        //                if (!string.IsNullOrEmpty(text))
        //                    webCtrlnode.Add("aaname", text);
        //                int index = -1;
        //                if (value.Contains("nth-of-type"))
        //                {
        //                    index = Convert.ToInt32(value.Substring(value.LastIndexOf("(") + 1, value.LastIndexOf(")") - value.LastIndexOf("(") - 1));
        //                    if (index >= 0)
        //                        webCtrlnode.Add("index", index);
        //                }
        //                selector[selector.Count - 1] = tag;
        //            }
        //            else
        //            {
        //                var value = selector[selector.Count - 1];
        //                var tag = value.Substring(0, value.IndexOf(":"));
        //                int index = -1;
        //                index = Convert.ToInt32(value.Substring(value.LastIndexOf("(") + 1, value.LastIndexOf(")") - value.LastIndexOf("(") - 1));
        //                if (index >= 0)
        //                    webCtrlnode.Add("index", index);
        //                selector[selector.Count - 1] = tag;
        //            }
        //        }
        //        webCtrlnode.Add("selector", string.Join(">", selector));
        //    }
        //    if (frameArray.Count > 0)
        //        node.Add("iframe", frameArray);
        //    backup.Add(cssAll);
        //    webCtrlnode.Add("backup", backup);
        //    node.Add("webCtrl", webCtrlnode);
        //    node.Add("custom", custom);
        //    wrapper.Add(node);
        //    return wrapper.ToString();
        //}

        public static string VisualViewData(string selector)
        {
            return VisualViewData(selector, true);
        }
        public static string VisualViewData(string selector, bool isSelectorAll)
        {
            /* 
             {
            "app":{
            },
            "Frame":{
            },
            "webCtrl":{
            }
            }*/
            try
            {
                if (string.IsNullOrEmpty(selector))
                    return selector;
                var array = JArray.Parse(selector);
                if (array[0]["selector"].Value<string>().ToLower().ToString() == "ie")
                {
                    return IEViewData(array, isSelectorAll);
                }
                else if (array[0]["selector"].Value<string>().ToLower().ToString() == "edge")
                {
                    return EdgeViewData(array, isSelectorAll);
                }
                else if (array[0]["selector"].Value<string>().ToLower().ToString() == "chrome")
                {
                    return ChromeViewData(array, isSelectorAll);
                }
                else if (array[0]["selector"].Value<string>().ToLower().ToString() == "firefox")
                {
                    return FireFoxViewData(array, isSelectorAll);
                }
                if (array[0]["selector"].Value<string>().ToLower().ToString() == "zexplorer")
                {
                    return ZExplorerViewData(array);
                }
                if (array[0]["selector"].Value<string>().ToLower().ToString() == "java")
                {
                    return JavaViewData(array);
                }
                if (array[0]["selector"].Value<string>().ToLower().ToString() == "windows")
                {
                    return WinViewData(array);
                }
                if (array[0]["selector"].Value<string>().ToLower().ToString() == "web360se")
                {
                    return Chrome360ViewData(array);
                }
                return selector;
            }
            catch (Exception e)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, e, "元素格式错误");
                return selector;
            }
        }
        public static JObject Resolver(string prop, string outertext = null)
        {
            JObject node = new JObject();
            if (!prop.Contains("#") && !prop.Contains(":nth-child(") && !prop.Contains(":nth-of-type(")
                && !prop.Contains("[outertext=") && !prop.Contains("[type=") && !prop.Contains("[name=") && !prop.Contains("[src=") && !prop.Contains("[href="))
            {
                if (prop.Contains("."))
                {
                    node.Add("tagName", prop.Substring(0, prop.IndexOf(".")));
                    node.Add("class", prop.Substring(prop.IndexOf(".") + 1, prop.Length - prop.IndexOf(".") - 1));
                }
                else
                {
                    node.Add("tagName", prop);
                }
            }
            else if (prop.Contains("#") && !prop.Contains(":nth-child(") && !prop.Contains(":nth-of-type(") && !prop.Contains("[outertext=") && !prop.Contains("[type=") && !prop.Contains("[name=") && !prop.Contains("[src=") && !prop.Contains("[href="))
            {
                node.Add("tagName", prop.Substring(0, prop.IndexOf("#")));
                if (prop.Contains("."))
                {
                    node.Add("parentId", prop.Substring(prop.IndexOf("#") + 1, prop.IndexOf(".") - prop.IndexOf("#") - 1));
                    node.Add("class", prop.Substring(prop.IndexOf(".") + 1, prop.Length - prop.IndexOf(".") - 1));
                }
                else
                    node.Add("parentId", prop.Substring(prop.IndexOf("#") + 1, prop.Length - prop.IndexOf("#") - 1));
            }
            else
            {
                if (prop.Contains("#") && (prop.Contains(":nth-child(") || prop.Contains(":nth-of-type(")))
                {
                    node.Add("tagName", prop.Substring(0, prop.IndexOf("#")));
                    if (prop.Contains("[outertext=") || prop.Contains("[type=") || prop.Contains("[name=") || prop.Contains("[src=") || prop.Contains("[href="))
                    {
                        var value = prop.Substring(prop.IndexOf("#") + 1, prop.IndexOf("[") - prop.IndexOf("#") - 1);
                        if (value.Contains(":nth-child(") || value.Contains(":nth-of-type("))
                        {
                            if (value.Contains("."))
                                node.Add("parentId", value.Substring(value.IndexOf("#") + 1, value.IndexOf(".") - value.IndexOf("#") - 1));
                            else
                                node.Add("parentId", value.Substring(value.IndexOf("#") + 1, value.IndexOf(":") - value.IndexOf("#") - 1));
                        }
                        else
                        {
                            if (value.Contains(".") && !prop.Contains("[type=") && !prop.Contains("[name=") && !prop.Contains("[src=") && !prop.Contains("[href="))
                            {
                                node.Add("parentId", value.Substring(value.IndexOf("#") + 1, value.IndexOf(".") - value.IndexOf("#") - 1));
                            }
                            else if (value.Contains(".") && (prop.Contains("[outertext=") || prop.Contains("[type=") || prop.Contains("[name=") || prop.Contains("[src=") || prop.Contains("[href=")))
                            {
                                if (value.IndexOf(".") < value.IndexOf("["))
                                    node.Add("parentId", value.Substring(value.IndexOf("#") + 1, value.IndexOf(".") - value.IndexOf("#") - 1));
                                else
                                    node.Add("parentId", value.Substring(value.IndexOf("#") + 1, value.IndexOf("[") - value.IndexOf("#") - 1));
                            }
                            else
                            {
                                node.Add("parentId", value);
                            }
                        }
                    }
                    else
                        node.Add("parentId", prop.Substring(prop.IndexOf("#") + 1, prop.IndexOf(":") - prop.IndexOf("#") - 1));
                }
                else if (prop.Contains("#") && (prop.Contains("[type=") || prop.Contains("[name=") || prop.Contains("[src=") || prop.Contains("[href=")))
                {
                    if (prop.IndexOf("#") < prop.IndexOf("["))
                        node.Add("tagName", prop.Substring(0, prop.IndexOf("#")));
                    else
                        node.Add("tagName", prop.Substring(0, prop.IndexOf("[")));
                    if (prop.Contains(":nth-child(") || prop.Contains(":nth-of-type("))
                    {
                        var value = prop.Substring(prop.IndexOf("#") + 1, prop.IndexOf(":") - prop.IndexOf("#") - 1);
                        if (value.Contains("[type=") || value.Contains("[name=") || value.Contains("[src=") || value.Contains("[href="))
                        {
                            if (value.Contains("."))
                            {
                                if (value.IndexOf(".") < value.IndexOf("["))
                                    node.Add("parentId", value.Substring(value.IndexOf("#") + 1, value.IndexOf(".") - value.IndexOf("#") - 1));
                                else
                                    node.Add("parentId", value.Substring(value.IndexOf("#") + 1, value.IndexOf("[") - value.IndexOf("#") - 1));
                            }
                            else
                            {
                                node.Add("parentId", value.Substring(value.IndexOf("#") + 1, value.IndexOf("[") - value.IndexOf("#") - 1));
                            }
                        }
                        else
                            node.Add("parentId", value.Substring(value.IndexOf("#") + 1, value.IndexOf(":") - value.IndexOf("#") - 1));
                    }
                    else
                    {
                        if (prop.IndexOf("#") < prop.IndexOf("["))
                            node.Add("parentId", prop.Substring(prop.IndexOf("#") + 1, prop.IndexOf("[") - prop.IndexOf("#") - 1));
                        else
                            node.Add("parentId", prop.Substring(prop.IndexOf("#") + 1, prop.Length - prop.IndexOf("#") - 1));
                    }
                }
                else if (!prop.Contains("#") && (prop.Contains("[type=") || prop.Contains("[name=") || prop.Contains("[src=") || prop.Contains("[href=")))
                {
                    if (prop.Contains(":nth-child(") || prop.Contains(":nth-of-type("))
                    {
                        var value = prop.Substring(0, prop.IndexOf(":"));
                        if (value.Contains("[outertext=") || value.Contains("[type=") || value.Contains("[name=") || value.Contains("[src=") || value.Contains("[href="))
                            if (value.Contains("."))
                            {
                                if (value.IndexOf(".") < value.IndexOf("["))
                                    node.Add("tagName", value.Substring(0, value.IndexOf(".")));
                                else
                                    node.Add("tagName", value.Substring(0, value.IndexOf("[")));
                            }
                            else
                            {
                                node.Add("tagName", value.Substring(0, value.IndexOf("[")));
                            }
                    }
                    else
                    {
                        if (prop.Contains("."))
                        {
                            if (prop.IndexOf(".") < prop.IndexOf("["))
                                node.Add("tagName", prop.Substring(0, prop.IndexOf(".")));
                            else if (prop.IndexOf(".") > prop.IndexOf("["))
                                node.Add("tagName", prop.Substring(0, prop.IndexOf("[")));
                        }
                        else
                        {
                            node.Add("tagName", prop.Substring(0, prop.IndexOf("[")));
                        }
                    }

                }
                else if (prop.Contains(":nth-child(") || prop.Contains(":nth-of-type("))
                {

                    if (prop.Contains("[type=") || prop.Contains("[name=") || prop.Contains("[src=") || prop.Contains("[href="))
                    {
                        var value = prop.Substring(0, prop.IndexOf("["));
                        if (value.Contains(":nth-child(") || value.Contains(":nth-of-type("))
                            node.Add("tagName", value.Substring(0, value.IndexOf(":")));
                        else
                            node.Add("tagName", value.Substring(0, value.IndexOf("[")));
                    }
                    else
                    {
                        //node.Add("tagName", prop.Substring(0, prop.IndexOf(":")));
                        if (prop.Contains("."))
                        {
                            if (prop.IndexOf(".") < prop.IndexOf(":"))
                            {
                                node.Add("tagName", prop.Substring(0, prop.IndexOf(".")));
                                node.Add("class", prop.Substring(prop.IndexOf(".") + 1, prop.IndexOf(":") - prop.IndexOf(".") - 1));
                            }
                        }
                        else
                        {
                            node.Add("tagName", prop.Substring(0, prop.IndexOf(":")));
                        }
                    }
                }
            }
            if (prop.Contains("[type=") || prop.Contains("[name=") || prop.Contains("[src=") || prop.Contains("[href="))
            {
                var props = prop.Split('[', ']');
                foreach (var item in props)
                {
                    if (item.Contains("name="))
                    {
                        var index = item.Replace("name=", "");
                        node.Add("name", index);
                    }
                    else if (item.Contains("type="))
                    {
                        var index = item.Replace("type=", "");
                        node.Add("type", index);
                    }
                    else if (item.Contains("src"))
                    {
                        var index = item.Replace("src=", "");
                        node.Add("src", index);
                    }
                    else if (item.Contains("href"))
                    {
                        var index = item.Replace("href=", "");
                        node.Add("href", index);
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(item) && item.Contains("."))
                            node.Add("class", item.Substring(1, item.Length - 1));
                    }
                }
            }
            if (!string.IsNullOrEmpty(outertext))
                node.Add("outertext", outertext);
            if (prop.Contains(":nth-child(") || prop.Contains(":nth-of-type("))
            {
                var index = prop.Substring(prop.IndexOf("(") + 1, prop.IndexOf(")") - prop.IndexOf("(") - 1);
                int result = 0;
                if (int.TryParse(index, out result))
                {
                    if (prop.Contains(":nth-child("))
                        node.Add("nth-child", result);
                    else
                        node.Add("nth-of-type", result);
                }
                else
                {
                    if (prop.Contains(":nth-child("))
                        node.Add("nth-child", index);
                    else
                        node.Add("nth-of-type", index);
                }
            }
            return node;
        }
        private static string Chrome360ViewData(JArray array, bool isSelectorAll = true)
        {
            JArray wrapper = new JArray();
            JObject node = new JObject();
            JObject custom = new JObject();
            JArray frameArray = new JArray();
            JObject webCtrlnode = new JObject();
            JObject cssAll = new JObject();
            JArray backup = new JArray();
            JObject extraInfo = new JObject();
            List<string> selector = new List<string>();
            int num = 0;
            int level = 0;
            for (int i = 0; i < array.Count; i++)
            {
                if (array[i].Value<string>("tagName") != null || array[i].Value<string>("tagName") != null)
                    if (array[i].Value<string>("tagName").ToLower() == "iframe" || array[i].Value<string>("tagName").ToLower() == "frame")
                    {
                        num = i;
                    }
            }

            foreach (var item in array)
            {
                if (!string.IsNullOrEmpty(item.Value<string>("selector")))
                {
                    var nodeblock = new JObject();
                    nodeblock.Add("name", "chrome.exe");
                    nodeblock.Add("subType", item.Value<string>("selector"));
                    nodeblock.Add("type", "html");
                    if (isSelectorAll)
                    {
                        cssAll.Add("selector", item.Value<string>("cssSelector") ?? "");
                        custom.Add("selector", item.Value<string>("cssSelector") ?? "");
                        if (!string.IsNullOrEmpty(item.Value<string>("outertext")))
                        {
                            cssAll.Add("aaname", item.Value<string>("outertext") ?? "");
                            custom.Add("aaname", item.Value<string>("outertext") ?? "");
                        }
                    }
                    extraInfo.Add("xpath", item.Value<string>("xpath") ?? "");
                    extraInfo.Add("fullSelector", item.Value<JToken>("fullSelector") ?? "");
                    nodeblock.Add("title", item.Value<string>("title") ?? "");
                    nodeblock.Add("url", item.Value<string>("url") ?? "");
                    nodeblock.Add("filePath", item.Value<string>("filePath") ?? "");
                    if (!string.IsNullOrEmpty(item.Value<string>("remoteAddress")))
                    {
                        nodeblock.Add("remoteName", item.Value<string>("remoteName") ?? "");
                        nodeblock.Add("remoteAddress", item.Value<string>("remoteAddress") ?? "");
                    }
                    if (!string.IsNullOrEmpty(item.Value<string>("captureMode")))
                    {
                        nodeblock.Add("captureMode", item.Value<string>("captureMode") ?? "");
                    }
                    node.Add("app", nodeblock);
                }
                else if (!string.IsNullOrEmpty(item.Value<string>("tagName")))
                {
                    if (level <= num && num != 0)
                    {
                        frameArray.Add(FrameViewData(item));
                    }
                    else
                    {
                        selector.Add(BuildCssSelectorViewData(item));
                    }
                }
                level++;
            }

            if (selector.Count > 0)
            {
                if (selector[0].Contains("#"))
                {
                    var value = selector[0];
                    webCtrlnode = Resolver(value);
                    if (selector.Count == 1)
                    {
                        JObject j = new JObject();
                        foreach (var item in webCtrlnode)
                        {
                            if (item.Key == "parentId")
                            {
                                j.Add("id", item.Value);
                            }
                            else
                            {
                                j.Add(item.Key, item.Value);
                            }
                        }
                        webCtrlnode = j;
                    }
                    selector.Remove(selector[0]);
                }
                if (selector.Count > 0)
                {
                    if (selector[selector.Count - 1].Contains("outertext"))
                    {
                        var value = selector[selector.Count - 1];
                        var tag = value.Substring(0, value.IndexOf("["));
                        var text = value.Substring(value.IndexOf("["), value.LastIndexOf("]") - value.IndexOf("[")).Replace("[outertext=", "");
                        if (!string.IsNullOrEmpty(text))
                            webCtrlnode.Add("aaname", text);
                        int index = -1;
                        if (value.Contains("nth-child"))
                        {
                            index = Convert.ToInt32(value.Substring(value.LastIndexOf("(") + 1, value.LastIndexOf(")") - value.LastIndexOf("(") - 1));
                            if (index >= 0)
                                webCtrlnode.Add("index", index);
                        }
                        selector[selector.Count - 1] = tag;
                    }
                    else if (selector[selector.Count - 1].Contains("nth-child"))
                    {
                        var value = selector[selector.Count - 1];
                        var tag = value.Substring(0, value.IndexOf(":"));
                        int index = -1;
                        index = Convert.ToInt32(value.Substring(value.LastIndexOf("(") + 1, value.LastIndexOf(")") - value.LastIndexOf("(") - 1));
                        if (index >= 0)
                            webCtrlnode.Add("index", index);
                        selector[selector.Count - 1] = tag;
                    }

                    if (webCtrlnode.ContainsKey("parentId"))
                        webCtrlnode.Add("selector", string.Join(">", selector));
                    else if (selector[0] == "body" || selector[0] == "html")
                        webCtrlnode.Add("selector", string.Join(">", selector));
                    else
                        webCtrlnode.Add("selector", " body>" + string.Join(">", selector));
                }
            }
            if (frameArray.Count > 0)
                node.Add("iframe", frameArray);
            backup.Add(cssAll);
            if (isSelectorAll)
                webCtrlnode.Add("backup", backup);
            node.Add("webCtrl", webCtrlnode);
            if (isSelectorAll)
            {
                node.Add("custom", custom);
                node.Add("extraInfo", extraInfo);
            }
            wrapper.Add(node);
            return wrapper.ToString();
        }

    }
}
namespace II.RPA.WinAutomation.CommonUtility.Parse
{
    public class SelectorProps
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("type")]
        public string Type { get; set; } = "";
        [JsonProperty("props")]
        public List<Props> Props { get; set; }
        [JsonProperty("accurate")]
        public string Accurate { get; set; }
    }
    public class Props
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("value")]
        public string Value { get; set; } = "";
        [JsonProperty("pattern")]
        public string Pattern { get; set; }
        [JsonProperty("accurate")]
        public string Accurate { get; set; }
    }
}

