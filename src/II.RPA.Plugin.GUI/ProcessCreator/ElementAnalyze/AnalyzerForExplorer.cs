using II.RPA.Logging;
using II.RPA.Plugin.GUI.ProcessCreator.ElementAnalyze.Abstract;
using II.RPA.Plugin.GUI.ProcessCreator.ElementTrack;
using II.RPA.WinAutomation.CommonUtility;
using IWshRuntimeLibrary;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.ProcessCreator.ElementAnalyze
{
    internal class AnalyzerForExplorer : AnalyzerBase
    {
        private static readonly ILogger logger = Logger.OfContext();
        private readonly string[] m_ExcelSuffixes = new string[] 
        {
            "xls",
            "xlsx",
            "xlt",
            "xlsm",
            "csv"
        };
        private readonly string[] m_ExcelWpsSuffixes = new string[]
        {
            "dt"
        };
        private readonly string[] m_WordSuffixes = new string[] 
        {
            "doc",
            "docx"
        };
        private readonly string[] m_PdfSuffixes = new string[]
        {
            "pdf",
        };
        private readonly string[] m_PictureSuffixes = new string[]
{
            "jpg","jpeg","png","svg","gif","ico","bmp","tif",
};
        private Dictionary<string, string> m_CurrentOpenFolders;
        private Dictionary<string, string> m_CurrentSubFolders;
        public AnalyzerForExplorer()
        {
            //单例、构造方法不要使用
        }
        public override string[] SupportGuiTypes => new string[] { "windows"};

        public override bool TryExecuteAnalyzer(JObject aSelectorJO,
            ActionParamModel actionParamModel,
            ElementCacheProvider anElementCacheProvider,
            out List<ExecuteAction> recommendActionExecutions, 
            out Dictionary<string, string> recommendActionExecutionParams)
        {
            recommendActionExecutions = null;
            recommendActionExecutionParams = null;
            try
            {
                var processName = aSelectorJO["commonProps"]["processName"].ToString();
                if (!processName.Equals("explorer.exe", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
                else
                {
                    m_CurrentOpenFolders = RetriveOpenedFolder();
                    m_CurrentSubFolders = GetAllSubDirectory(m_CurrentOpenFolders);

                    JArray selectorJA = aSelectorJO["selector"] as JArray;
                    var selectorJAReverse = selectorJA.Reverse().ToArray();
                    var lastNode = selectorJA.Last;
                    var firstNode = selectorJA.First();
                    //任务栏
                    if (firstNode!=null && firstNode["name"].ToString() == "Pane")
                    {
                        var nameprop = firstNode["props"].FirstOrDefault(p => p["name"].ToString() == "name");
                        if (nameprop != null && nameprop["value"].ToString() == "任务栏")
                        {
                            logger.LogInformation("Find Taskbar element");
                            if (lastNode["name"].ToString().Equals("Button", StringComparison.OrdinalIgnoreCase))
                            {
                                var lastnameprop = lastNode["props"].FirstOrDefault(p => p["name"].ToString() == "name");
                                if (lastnameprop["value"].ToString().Contains("Google Chrome"))
                                {
                                    recommendActionExecutionParams = new Dictionary<string, string>
                                    {
                                        { "args0.type", "\"chrome\"" }
                                    };
                                    if (LocalDataModels.SpecialElementActions.TryGetValue("explorer.taskbar",
                                        out List<ElementAction> anElementActions))
                                    {
                                        recommendActionExecutions = anElementActions.First().Actions;
                                        logger.LogInformation("Find Taskbar element:chrome");
                                        return true;
                                    }
                                }
                                else if (lastnameprop["value"].ToString().Contains("Microsoft Edge"))
                                {
                                    recommendActionExecutionParams = new Dictionary<string, string>
                                    {
                                        { "args0.type", "\"edge\"" }
                                    };
                                    if (LocalDataModels.SpecialElementActions.TryGetValue("explorer.taskbar",
                                       out List<ElementAction> anElementActions))
                                    {
                                        recommendActionExecutions = anElementActions.First().Actions;
                                        logger.LogInformation("Find Taskbar element:edge");
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                    //文件资源管理器
                    //当前元素是listitem
                    if (lastNode["name"].ToString().Equals("listitem", StringComparison.OrdinalIgnoreCase))
                    {
                        var namenode = lastNode["props"].FirstOrDefault(p => p["name"].ToString() == "name");
                        if (namenode["value"] != null && !string.IsNullOrEmpty(namenode["value"].ToString()))
                        {
                            var name = namenode["value"].ToString();
                            if (m_CurrentSubFolders.ContainsKey(name))
                            {
                                logger.LogInformation($"Find sub folder:{name}");
                                //if (actionParamModel.UIAElement.Properties.HelpText.IsSupported)
                                //{
                                    logger.LogDebug($"Find sub folder:{actionParamModel.UIAElement.Properties.HelpText.ValueOrDefault}");
                                    if (LocalDataModels.SpecialElementActions.TryGetValue("explorer.folder", out List<ElementAction> anElementActions))
                                    {
                                        recommendActionExecutions = anElementActions.First().Actions;
                                        recommendActionExecutionParams = new Dictionary<string, string>
                                    {
                                        { "args0.path", JsonConvert.ToString(m_CurrentSubFolders[name]) },
                                    };
                                        recommendActionExecutionParams = GetDefaultActionParams(recommendActionExecutions, recommendActionExecutionParams);
                                        return true;
                                    }
                                //}
                                //else
                                //{
                                //    logger.LogWarning($"Folder and file with the same name:{name}");
                                //    logger.LogWarning($"Take {name} is file as its propertry [HelpText] is not supported!");
                                //}

                            }
                            if (name.Split('.').Length > 1 && m_PictureSuffixes.Contains(name.Split('.').Last().ToLower()))
                            {
                                if (LocalDataModels.NativeElementActions.TryGetValue("listitem", out List<ElementAction> anElementActions))
                                {
                                    recommendActionExecutions = anElementActions.FirstOrDefault(a=>a.Rules.Contains("windows.explorer.picture")).Actions;
                                    var aWindowNode = selectorJA.First;
                                    recommendActionExecutionParams = new Dictionary<string, string>();
                                    if (aWindowNode["name"].ToString().Equals("window", StringComparison.OrdinalIgnoreCase))
                                    {
                                        var nameNode = aWindowNode["props"].FirstOrDefault(p => p["name"].ToString().Equals("name"));
                                        var foldername = nameNode["value"]?.ToString();
                                        if (!string.IsNullOrEmpty(foldername))
                                        {
                                            var filepath = GetFileFullPath(foldername, name);
                                            recommendActionExecutionParams.Add("args0.pathSrc", JsonConvert.ToString(filepath));
                                            recommendActionExecutionParams.Add("args0.file_path", JsonConvert.ToString(filepath));
                                            logger.LogDebug($"windows.explorer.picture.{filepath}");
                                            return true;
                                        }
                                    }
                                    else if (aWindowNode["props"].Any(p => p["name"].ToString() == "class" && p["value"].ToString() == "Progman|WorkerW"))
                                    {
                                        //桌面文件
                                        var filepath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), name);
                                        recommendActionExecutionParams.Add("args0.pathSrc", JsonConvert.ToString(filepath));
                                        recommendActionExecutionParams.Add("args0.file_path", JsonConvert.ToString(filepath));
                                        logger.LogDebug($"windows.explorer.picture.{filepath}");
                                        return true;
                                    }


                                    return true;
                                }
                            }
                        }
                        if (TryGetWantedActionAndParams(lastNode, selectorJA.First, out string actionKey, out string actionParams))
                        {
                            if (LocalDataModels.SpecialElementActions.TryGetValue(actionKey, out List<ElementAction> anElementActions))
                            {
                                recommendActionExecutions = anElementActions.First().Actions;
                                recommendActionExecutionParams = new Dictionary<string, string>
                                {
                                    { "args0.file", JsonConvert.ToString(actionParams) },
                                    { "args0.filePath", JsonConvert.ToString(actionParams) },
                                    { "args0.fileInPath", JsonConvert.ToString(actionParams) },
                                    { "args0.pathSrc", JsonConvert.ToString(actionParams) },
                                    { "args0.file_path", JsonConvert.ToString(actionParams) }
                                };
                                recommendActionExecutionParams = GetDefaultActionParams(recommendActionExecutions, recommendActionExecutionParams);
                                return true;
                            }
                        }
                        else
                        {
                            if (LocalDataModels.SpecialElementActions.TryGetValue("explorer.default", out List<ElementAction> anElementActions))
                            {
                                recommendActionExecutions = anElementActions.First().Actions;
                                recommendActionExecutionParams = GetDefaultActionParams(recommendActionExecutions, recommendActionExecutionParams);
                                return true;
                            }
                        }
                    }
                    //桌面右键菜单
                    //else if (lastNode["name"].ToString().Equals("menuitem", StringComparison.OrdinalIgnoreCase)
                    //    && firstNode["name"].ToString().Equals("pane", StringComparison.OrdinalIgnoreCase)
                    //    && selectorJA.Count() == 3
                    //    && selectorJA[1]["name"].ToString().Equals("menu", StringComparison.OrdinalIgnoreCase)
                    //    && selectorJA[1]["props"].Any(p => p["name"].ToString() == "name" && (p["value"].ToString().StartsWith("New") || p["value"].ToString().StartsWith("新建")))
                    //    && firstNode["props"].Any(p => p["name"].ToString() == "class" && (p["value"].ToString() == "Progman" || p["value"].ToString() == "WorkerW")))
                    else if (SomeUtils.IsWindowsContextMenuNew(aSelectorJO))
                    {

                        //桌面文件
                        var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                        recommendActionExecutionParams = new Dictionary<string, string>
                        {
                            { "args0.filePath", $"{JsonConvert.ToString(desktopPath)}" },
                            { "args0.path", $"{JsonConvert.ToString(desktopPath)}" }
                        };
                        if (LocalDataModels.SpecialElementActions.TryGetValue("explorer.contextmenu.new",
                            out List<ElementAction> anElementActions))
                        {
                            recommendActionExecutions = anElementActions.First().Actions;
                            recommendActionExecutionParams = GetDefaultActionParams(recommendActionExecutions, recommendActionExecutionParams);
                            logger.LogInformation("Find contextmenu new");
                            return true;
                        }
                    }
                    else if (selectorJAReverse.Length > 1 && selectorJAReverse[1]["name"].ToString().Equals("listitem", StringComparison.OrdinalIgnoreCase))
                    {
                        var namenode = selectorJAReverse[1]["props"].FirstOrDefault(p => p["name"].ToString() == "name");
                        if (namenode["value"] != null && !string.IsNullOrEmpty(namenode["value"].ToString()))
                        {
                            var name = namenode["value"].ToString();
                            if (m_CurrentSubFolders.ContainsKey(name))
                            {
                                //
                                logger.LogInformation($"Find sub folder:{name}");
                                if (LocalDataModels.SpecialElementActions.TryGetValue("explorer.folder", out List<ElementAction> anElementActions))
                                {
                                    recommendActionExecutions = anElementActions.First().Actions;
                                    recommendActionExecutionParams = new Dictionary<string, string>
                                    {
                                        { "args0.path", JsonConvert.ToString(m_CurrentSubFolders[name]) },
                                    };
                                    recommendActionExecutionParams = GetDefaultActionParams(recommendActionExecutions, recommendActionExecutionParams);
                                    return true;
                                }
                            }
                        }
                    }
                    else
                    {
                        //当前元素不是特殊的资源管理器元素
                        if (selectorJAReverse.Count() < 2)
                        {
                            return false;
                        }
                        lastNode = selectorJAReverse[1];
                        if (lastNode["name"].ToString().Equals("listitem", StringComparison.OrdinalIgnoreCase))
                        {
                            if (TryGetWantedActionAndParams(lastNode, selectorJA.First, out string actionKey, out string actionParams))
                            {
                                if (LocalDataModels.SpecialElementActions.TryGetValue(actionKey, out List<ElementAction> anElementActions))
                                {
                                    recommendActionExecutions = anElementActions.First().Actions;
                                    recommendActionExecutionParams = new Dictionary<string, string>
                                    {
                                        { "args0.file", JsonConvert.ToString(actionParams) },
                                        { "args0.filePath", JsonConvert.ToString(actionParams) },
                                        { "args0.pathSrc", JsonConvert.ToString(actionParams) },
                                        { "args0.file_path", JsonConvert.ToString(actionParams) }
                                    };
                                    recommendActionExecutionParams = GetDefaultActionParams(recommendActionExecutions, recommendActionExecutionParams);
                                    return true;
                                }
                            }
                            else
                            {
                                if (LocalDataModels.SpecialElementActions.TryGetValue("explorer.default", out List<ElementAction> anElementActions))
                                {
                                    recommendActionExecutions = anElementActions.First().Actions;
                                    recommendActionExecutionParams = GetDefaultActionParams(recommendActionExecutions, recommendActionExecutionParams);
                                    return true;
                                }
                            }
                        }
                    }
                }
               
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
            }
            return false;
        }
        private bool TryGetWantedActionAndParams(JToken aListitemNode, JToken aWindowNode,out string actionKey,out string actionParams)
        {
            actionParams = string.Empty;
            if (TryGetWantedFileName(aListitemNode, out actionKey, out string aFileName))
            {
                if (aWindowNode["name"].ToString().Equals("window", StringComparison.OrdinalIgnoreCase))
                {
                    var nameNode = aWindowNode["props"].FirstOrDefault(p => p["name"].ToString().Equals("name"));
                    var foldername = nameNode["value"]?.ToString();
                    if (!string.IsNullOrEmpty(foldername))
                    {
                        actionParams = GetFileFullPath(foldername, aFileName);
                        logger.LogWarning($"TryGetWantedActionAndParams:true.{actionKey};{actionParams}");
                        return true;
                    }
                }
                else if (aWindowNode["props"].Any(p => p["name"].ToString() == "class" && p["value"].ToString() == "Progman|WorkerW"))
                {
                    //桌面文件
                    actionParams = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), aFileName);
                    logger.LogWarning($"TryGetWantedActionAndParams:true (desktop).{actionKey};{actionParams}");
                    return true;
                }
            }
            logger.LogWarning("TryGetWantedActionAndParams:false");
            return false;
        }
        private bool TryGetWantedFileName(JToken aListitemNode,out string actionKey,out string aFileName)
        {
            actionKey = string.Empty;
            aFileName = string.Empty;
            var propNode = aListitemNode["props"].FirstOrDefault(p => p["name"].ToString().Equals("name"));
            var elementName = propNode["value"].ToString();
            if (Path.HasExtension(elementName))
            {
                var suffix = elementName.Split('.').Last().ToLower();
                if (m_ExcelSuffixes.Contains(suffix, new AnalyzerEqualityComparer()))
                {
                    actionKey = "explorer.excel";
                    aFileName = elementName;
                    return true;
                }
                else if (m_WordSuffixes.Contains(suffix, new AnalyzerEqualityComparer()))
                {
                    actionKey = "explorer.word";
                    aFileName = elementName;
                    return true;
                }
                else if (m_ExcelWpsSuffixes.Contains(suffix, new AnalyzerEqualityComparer()))
                {
                    actionKey = "explorer.excel.wps";
                    aFileName = elementName;
                    return true;
                }
                else if (m_PdfSuffixes.Contains(suffix, new AnalyzerEqualityComparer()))
                {
                    actionKey = "explorer.pdf";
                    aFileName = elementName;
                    return true;
                }
                else
                {
                    logger.LogDebug($"Not recognized file suffix:{suffix}");
                    return false;
                }
            }
            else
            {
                logger.LogDebug($"Can not get extension name:{elementName}");
            }
            return false;
        }
        private string GetFileFullPath(string aWindowName,string aFileName)
        {
            if (m_CurrentOpenFolders.TryGetValue(aWindowName,out string aValue))
            {
                return Path.Combine(aValue,aFileName);
            }
            return null;
        }
        /// <summary>
        /// 文一提供的方法
        /// </summary>
        /// <returns></returns>
        private Dictionary<string,string> RetriveOpenedFolder()
        {
            var stopwatch = Stopwatch.StartNew();
            var folders = new Dictionary<string,string>();
            using (PowerShell powershell = PowerShell.Create())
            {
                powershell.AddScript("@((New-Object -com shell.application).Windows()) | select LocationName, LocationURL");
                Collection<PSObject> PSOutput = powershell.Invoke();

                foreach (PSObject outputItem in PSOutput)
                {
                    if (outputItem != null)
                    {
                        var _name = $"{outputItem.Members["LocationName"].Value}";
                        if (!folders.ContainsKey(_name))
                        {
                            var _path = $"{outputItem.Members["LocationURL"].Value}";
                            if (!string.IsNullOrEmpty(_path))
                            {

                                //远程路径是file:// 开头，这里不做替换
                                folders.Add(_name, _path.Replace(@"file:///", ""));
                            }
                            else
                            {
                                //忽略控制面板等非文件夹的资源管理器窗口
                            }
                        }
                    }
                }
            };
            logger.LogInformation($"Find {folders.Count} folders.Cost {stopwatch.ElapsedMilliseconds} ms");
            return folders;

        }
        private Dictionary<string, string> GetAllSubDirectory(Dictionary<string, string> anAllFolders)
        {
            var stopwatch = Stopwatch.StartNew();
            Dictionary<string, string> subDirectories = new Dictionary<string, string>();

            foreach (var foldername in anAllFolders.Values)
            {
                try
                {
                    var subs = GetSubDirectory(foldername);
                    if (subs != null && subs.Any())
                    {
                        foreach (var sub in subs)
                        {
                            if (!subDirectories.ContainsKey(sub.Key))
                            {
                                subDirectories.Add(sub.Key, sub.Value);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, "GetSubDirectory Error");
                    continue;
                }
            }
            logger.LogInformation($"Find {subDirectories.Count} sub folders.Cost {stopwatch.ElapsedMilliseconds} ms");
            return subDirectories;
        }
        private Dictionary<string,string> GetSubDirectory(string aFolderName)
        {
            Dictionary<string, string> subFolderNames = new Dictionary<string, string>();
            DirectoryInfo theFolder = new DirectoryInfo(aFolderName);
            var  subFolders = theFolder.GetDirectories();
            if (subFolders != null && subFolders.Any())
            {
                foreach (var sub in subFolders)
                {
                    if (!subFolderNames.ContainsKey(sub.Name))
                    {
                        subFolderNames.Add(sub.Name, sub.FullName);
                    }
                }
            }
            return subFolderNames;
        }
    }
}

