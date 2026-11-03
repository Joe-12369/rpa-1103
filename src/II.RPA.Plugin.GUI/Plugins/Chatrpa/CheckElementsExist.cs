using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Params;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Xml;
using System.Xml.Linq;
using II.RPA.Plugin.Abstractions;

namespace II.RPA.Plugin.GUI.Plugins.Chatrpa
{
    public class CheckElementsExist : GuiComponentBase<ParamCheckElementsExistModel>
    {
        private static ILogger logger = Logger.OfContext();
        private readonly IPluginInvoker pluginInvoker;

        public CheckElementsExist(IPluginInvoker pluginInvoker)
        {
            this.pluginInvoker = pluginInvoker ?? throw new ArgumentNullException(nameof(pluginInvoker));
        }

        protected override eComponentType ComponentType => eComponentType.IsElementExist;

        protected override async Task<JMessage> ExecuteAsync(ParamCheckElementsExistModel paramModel, PluginExecutionContext executionContext)
        {
            JMessage msg = new JMessage();
            msg.Info = string.Empty;
            GuiElement element = null;
            try
            {
                Dictionary<string, object> hashtable = new Dictionary<string, object>{
                        {"processId",paramModel.processId }
                    };
                //先通过进程信息去取本地安装目录的版本号
                logger.LogDebug($"目标软件进程名称:{paramModel.ProcessName}");
                string version = await this.GetAppVersionByProcessNameAsync(paramModel.ProcessName, executionContext).ConfigureAwait(false);
                logger.LogDebug($"目标软件当前版本号:{version}");
                if (!string.IsNullOrEmpty(version))
                {
                    var matchElementsVersion = paramModel.Selectors.Where(a => a.Version.EqualIgnoreCase(version));
                    if (matchElementsVersion.Count() > 0)
                    {
                        foreach (var ele in matchElementsVersion)
                        {
                            logger.LogDebug($"版本号匹配校验，id:{ele.Id}");
                            var param = new GetElementFromSelectorParams
                            {
                                Selector = ele.Selector,
                                SelectorType = paramModel.SelectorType,
                                FindOnce = true,
                                CVExecutionServiceURL = $"{GlobleConfigurations.ZBrainServicePath}{CVRouteConstFields.CV_EXECUTION_SERVICE}",
                                ExtendParam = hashtable,
                                MatchType = paramModel.MatchType,
                                RunParam = paramModel.RunParam,
                                WindowInfo = paramModel.WindowInfo
                            };
                            try
                            {
                                element = GuiElement.FromSelector(param);
                                if (element != null)
                                {
                                    logger.LogInformation($"根据版本号找到了元素，id为:{ele.Id}");
                                    var data = new
                                    {
                                        id = ele.Id
                                    };
                                    msg.Info = JsonConvert.SerializeObject(data);
                                    return msg;
                                }
                            }
                            catch (Exception ex)
                            {
                                Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, $"根据版本号匹配到的元素结构查找元素异常，id为:{ele.Id}");
                            }
                        }
                    }
                }
                var matchElements = paramModel.Selectors.Where(a => !a.Version.EqualIgnoreCase(version));
                foreach (var selectorInfo in matchElements)
                {
                    logger.LogInformation($"进入列表遍历,遍历id:{selectorInfo.Id}");
                    var param = new GetElementFromSelectorParams
                    {
                        Selector = selectorInfo.Selector,
                        SelectorType = paramModel.SelectorType,
                        FindOnce = true,
                        CVExecutionServiceURL = $"{GlobleConfigurations.ZBrainServicePath}{CVRouteConstFields.CV_EXECUTION_SERVICE}",
                        ExtendParam = hashtable,
                        MatchType = paramModel.MatchType,
                        RunParam = paramModel.RunParam,
                        WindowInfo = paramModel.WindowInfo
                    };
                    try
                    {
                        element = GuiElement.FromSelector(param);
                        if (element != null)
                        {
                            logger.LogInformation($"遍历元素id:{selectorInfo.Id}有效");
                            var data = new
                            {
                                id = selectorInfo.Id
                            };
                            msg.Info = JsonConvert.SerializeObject(data);
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, $"遍历元素结构查找元素异常，id为:{selectorInfo.Id}");
                    }
                }
                if (string.IsNullOrEmpty(msg.Info))
                {
                    logger.LogDebug($"未匹配到有效元素结构");
                    var data = new
                    {
                        id = ""
                    };
                    msg.Info = JsonConvert.SerializeObject(data);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                Exception(ex, eErrorMessageType.Error_ElementDeterminationFailed, ref msg);
            }
            return msg;
        }

        protected override void ValidateParameter(ParamCheckElementsExistModel paramModel) {
            try
            {
                if (string.IsNullOrEmpty(paramModel.ProcessName))
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError);
                }
                if (paramModel.Selectors == null || paramModel.Selectors.Count < 1)
                {
                    throw new RPAException(eErrorMessageType.Error_ParamsError);
                }
                return;
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogInformation(logger, ex, $"CheckElementsExist 参数格式不符合要求：{paramModel.ToJson()}");
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }
        public async Task<string> GetAppVersionByProcessNameAsync(string processName, PluginExecutionContext executionContext)
        {
            //DingTalk 钉钉
            string version = "";
            try
            {
                #region 获取应用版本号-- 暂时先这样，后续根据app应用做成接口
                //钉钉
                if (processName.EqualIgnoreCase("DingTalk") || processName.EqualIgnoreCase("DingTalk.exe") || processName.EqualIgnoreCase("DingtalkLauncher"))
                {
                    string[] appprocess = { "DingtalkLauncher", "DingTalk", "dingding" };
                    var data = new Dictionary<string, object>
                    {
                        {"AppProcessNames", appprocess }
                    };
                    logger.LogInformation($"data：{JsonHelper.JsonByNewtonJson(data)}");
                    string result = await pluginInvoker.InvokeAsync(
                        new PluginRoute("RPASystem", "GetApplicationPath"),
                        JsonHelper.JsonByNewtonJson(data)).ConfigureAwait(false);
                    logger.LogDebug($"获取应用路径返回信息：{result}");
                    var info = JObject.Parse(result)["info"];
                    var appPath = (string)JObject.Parse(info.ToString())["InstallLocation"];
                    logger.LogDebug($"启动程序路径:{appPath}");
                    string pathRoot = Path.GetDirectoryName(appPath);
                    string dirPath = "main/current/configurations/staticconfig.xml";
                    if (appPath.Contains("DingTalk.exe"))
                    {
                        dirPath = "configurations/staticconfig.xml";
                        //pathRoot = pathRoot.Replace("current_new", "current");
                    }
                    logger.LogDebug($"版本文件路由:{dirPath}");
                    logger.LogDebug($"启动程序根目录:{pathRoot}");
                    string versionFilePath = Path.Combine(pathRoot, dirPath);
                    if (!File.Exists(versionFilePath))
                    {
                        logger.LogWarning($"钉钉版本文件不存在:{versionFilePath}");
                        return "";
                    }
                    XDocument doc = XDocument.Load(versionFilePath);
                    var versionNode = doc.Root
                                    ?.Elements("item")
                                    ?.FirstOrDefault(item => item.Attribute("id")?.Value == "VersionString");
                    if (versionNode != null)
                    {
                        version = versionNode.Value;
                    }
                    return version;
                }
                //微信&企微
                if (processName.EqualIgnoreCase("WeChat") || processName.EqualIgnoreCase("Weixin") || processName.EqualIgnoreCase("WXWork"))
                {
                    string[] appprocess = { $"{processName}" };
                    var data = new Dictionary<string, object>
                    {
                        {"AppProcessNames", appprocess }
                    };
                    logger.LogInformation($"data：{JsonHelper.JsonByNewtonJson(data)}");
                    string result = await pluginInvoker.InvokeAsync(
                        new PluginRoute("RPASystem", "GetApplicationPath"),
                        JsonHelper.JsonByNewtonJson(data)).ConfigureAwait(false);
                    logger.LogDebug($"获取应用路径返回信息：{result}");
                    var info = JObject.Parse(result)["info"];
                    var installPath = (string)JObject.Parse(info.ToString())["InstallLocation"];
                    if (!string.IsNullOrEmpty(installPath) && File.Exists(installPath))
                    {
                        FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(installPath);
                        if (versionInfo != null && !string.IsNullOrEmpty(versionInfo.ProductVersion))
                        {
                            return versionInfo.ProductVersion;
                        }
                    }
                }
                #endregion
            }
            catch (Exception ex)
            {
                logger.LogError($"获取应用启动路径异常", ex);
            }
            return version;
        }
    }
}

