using II.RPA.Logging;
using II.RPA.WinAutomation.CommonUtility.Utils;
using IWshRuntimeLibrary;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using File = System.IO.File;

namespace II.RPA.Plugin.GUI.ProcessCreator
{
    public static class LocalDataModels
    {
        public const string LOGIN_COMP_KEY = "common_login";
        private static readonly ILogger logger = Logger.OfContext();

        private readonly static List<string> s_LinkNames = new List<string>();

        public static Dictionary<string, List<ElementAction>> NativeElementActions
            = new Dictionary<string, List<ElementAction>>();

        public static Dictionary<string, List<ElementAction>> CvElementActions
            = new Dictionary<string, List<ElementAction>>();

        public static Dictionary<string, List<ElementAction>> SpecialElementActions
            = new Dictionary<string, List<ElementAction>>();

        public static Dictionary<string, List<ElementAction>> DefaultActions
            = new Dictionary<string, List<ElementAction>>();

        public static Dictionary<string, List<ElementAction>> WebActions
            = new Dictionary<string, List<ElementAction>>();

        public static List<int> FactoryProcessIds = new List<int>();

        public static Dictionary<string, LoginDataModel> LoginDatas
           = new Dictionary<string, LoginDataModel>();

        public static List<string> SearchHistoryList = new List<string>();
        public static IntPtr FactoryWindowHandle;

        public readonly static Dictionary<string, ComponentModel> BusinessComponents 
            = new Dictionary<string, ComponentModel>();
        public readonly static string Login_Comp_Type;

        /// <summary>
        /// windows program数据
        /// </summary>
        public static Dictionary<string, string> StartMenuPrograms
        {
            get;
            private set;
        }
        public static string FactoryProcessName
        {
            get 
            {
                if (RPAEnvironment.GetInstance().CurrentLanguage == LanguageCultureEnum.zh_CN)
                {
                    return "实在设计器";
                }
                else
                {
                    return "Z-Factory";
                }
            }
        }


        public static string ActionCVJsonPath
        {
            get
            {
                if (RPAEnvironment.GetInstance().CurrentLanguage == LanguageCultureEnum.zh_CN)
                {
                    return "action_cv.json";
                }
                else
                {
                    return "action_cv_JP.json";
                }
            }
        }

        public static string ActionSpecialPath
        {
            get
            {
                if (RPAEnvironment.GetInstance().CurrentLanguage == LanguageCultureEnum.zh_CN)
                {
                    return "action_special.json";
                }
                else
                {
                    return "action_special_JP.json";
                }
            }
        }



        static LocalDataModels()
        {
            using (FileStream fileStream = File.Open(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "action_window.json"), FileMode.Open, FileAccess.Read))
            {
                using (StreamReader sw = new StreamReader(fileStream))
                {
                    using (JsonTextReader jw = new JsonTextReader(sw))
                    {
                        JsonSerializer serializer = new JsonSerializer();
                        NativeElementActions = serializer.Deserialize<Dictionary<string, List<ElementAction>>>(jw);
                    }
                }
            }
            using (FileStream fileStream = File.Open(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ActionCVJsonPath), FileMode.Open, FileAccess.Read))
            {
                using (StreamReader sw = new StreamReader(fileStream))
                {
                    using (JsonTextReader jw = new JsonTextReader(sw))
                    {
                        JsonSerializer serializer = new JsonSerializer();
                        CvElementActions = serializer.Deserialize<Dictionary<string, List<ElementAction>>>(jw);
                    }
                }
            }
            using (FileStream fileStream = File.Open(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ActionSpecialPath), FileMode.Open, FileAccess.Read))
            {
                using (StreamReader sw = new StreamReader(fileStream))
                {
                    using (JsonTextReader jw = new JsonTextReader(sw))
                    {
                        JsonSerializer serializer = new JsonSerializer();
                        SpecialElementActions = serializer.Deserialize<Dictionary<string, List<ElementAction>>>(jw);
                    }
                }
            }
            using (FileStream fileStream = File.Open(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "action_default.json"), FileMode.Open, FileAccess.Read))
            {
                using (StreamReader sw = new StreamReader(fileStream))
                {
                    using (JsonTextReader jw = new JsonTextReader(sw))
                    {
                        JsonSerializer serializer = new JsonSerializer();
                        DefaultActions = serializer.Deserialize<Dictionary<string, List<ElementAction>>>(jw);
                    }
                }
            }
            using (FileStream fileStream = File.Open(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "action_web.json"), FileMode.Open, FileAccess.Read))
            {
                using (StreamReader sw = new StreamReader(fileStream))
                {
                    using (JsonTextReader jw = new JsonTextReader(sw))
                    {
                        JsonSerializer serializer = new JsonSerializer();
                        WebActions = serializer.Deserialize<Dictionary<string, List<ElementAction>>>(jw);
                    }
                }
            }
            if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "login_data.json")))
            {
                try
                {
                    using (FileStream fileStream = File.Open(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "login_data.json"), FileMode.Open, FileAccess.Read))
                    {
                        using (StreamReader sw = new StreamReader(fileStream))
                        {
                            using (JsonTextReader jw = new JsonTextReader(sw))
                            {
                                JsonSerializer serializer = new JsonSerializer();
                                LoginDatas = serializer.Deserialize<Dictionary<string, LoginDataModel>>(jw);
                            }
                        }
                    }
                }
                catch(Exception ex) 
                {
                    logger.LogError(ex);
                }
            }

            if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "custom_action.json")))
            {
                try
                {
                    using (FileStream fileStream = File.Open(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "custom_action.json"), FileMode.Open, FileAccess.Read))
                    {
                        using (StreamReader sw = new StreamReader(fileStream))
                        {
                            using (JsonTextReader jw = new JsonTextReader(sw))
                            {
                                JsonSerializer serializer = new JsonSerializer();
                                BusinessComponents = serializer.Deserialize<Dictionary<string, ComponentModel>>(jw);
                                if (BusinessComponents.TryGetValue(LOGIN_COMP_KEY, out ComponentModel loginComp))
                                {
                                    Login_Comp_Type = loginComp.ComponentId;
                                    logger.LogInformation($"{LOGIN_COMP_KEY} is {Login_Comp_Type}");
                                }
                                else
                                {
                                    logger.LogError($"{LOGIN_COMP_KEY} is empty!");
                                }
                            }
                        }
                    }
                }
                catch(Exception ex) 
                {
                    logger.LogError(ex);
                }
            }

            #region get startmenu program
            StartMenuPrograms = new Dictionary<string, string>();
            var userPrograms = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
            var commonPrograms = Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms);
            var deskTop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            GetFileFullPath(userPrograms);
            GetFileFullPath(commonPrograms);
            GetFileFullPath(deskTop,false);

            if (s_LinkNames.Any())
            {
                s_LinkNames.ForEach(x =>
                {
                    if (x.EndsWith(".lnk"))
                    {
                        var pointpath = GetShortcutTargetFileWithWshShell(x);
                        if (!string.IsNullOrEmpty(pointpath) && !pointpath.StartsWith(@"\\"))
                        {
                            if (!File.Exists(pointpath))
                            {
                                if (pointpath.Contains("Program Files (x86)"))
                                {
                                    pointpath = pointpath.Replace("Program Files (x86)", "Program Files");
                                    //logger.LogWarning($"Remove (x86):{x};{pointpath}");
                                }
                            }
                            if (File.Exists(pointpath))
                            {
                                var lnkname = Path.GetFileNameWithoutExtension(x);
                                if (!StartMenuPrograms.ContainsKey(lnkname))
                                {
                                    StartMenuPrograms.Add(lnkname, pointpath);
                                }
                            }
                            else
                            {
                                //logger.LogWarning($"Find invalid path,ignore:{x};{pointpath}");
                            }
                        }
                    }
                });
                logger.LogDebug($"StartMenuPrograms:{StartMenuPrograms.Count}");
            }
            #endregion
        }
        public static List<string> LoadSearchHistory()
        {
            if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "search.json")))
            {
                using (FileStream fileStream = File.Open(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "search.json"), FileMode.Open, FileAccess.Read))
                {
                    using (StreamReader sw = new StreamReader(fileStream))
                    {
                        using (JsonTextReader jw = new JsonTextReader(sw))
                        {
                            JsonSerializer serializer = new JsonSerializer();
                            return serializer.Deserialize<List<string>>(jw);
                        }
                    }
                }
            }
            else
            {
                logger.LogWarning("Can not find search.json");
                return SearchHistoryList;
            }
        }
        public static void SaveSearchHistory(List<string> historyList)
        {
            if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "search.json")))
            {
                File.Delete("search.json");
            }
            using (var fileStream = File.Open("search.json", FileMode.OpenOrCreate))
            {
                using (var sw = new StreamWriter(fileStream))
                {
                    using (var jw = new JsonTextWriter(sw))
                    {
                        JsonSerializer serializer = new JsonSerializer();
                        serializer.Serialize(jw, historyList);
                        logger.LogInformation($"save search: {string.Join(";", historyList)}");
                    }
                }
            }
        }
        public static void Init()
        {
            Task.Factory.StartNew(() =>
            {
                var processes = Process.GetProcessesByName(FactoryProcessName);
                if (processes != null && processes.Any())
                {
                    FactoryProcessIds = processes.Select(p => p.Id).ToList();
                    logger.LogInformation($"FactoryProcessIds:{string.Join(";", FactoryProcessIds)}");
                }
            });
        }
        private static void GetFileFullPath(string dirs, bool isLoop = true)
        {
            try
            {
                DirectoryInfo dir = new DirectoryInfo(dirs);
                FileSystemInfo[] fsinfos = dir.GetFileSystemInfos();

                foreach (FileSystemInfo fsinfo in fsinfos)
                {
                    if ((fsinfo.Attributes | FileAttributes.Hidden | FileAttributes.System) == fsinfo.Attributes)
                    {
                        continue;
                    }
                    if (fsinfo is DirectoryInfo && isLoop)
                    {
                        //递归调用
                        GetFileFullPath(fsinfo.FullName);
                    }
                    else
                    {
                        //Console.WriteLine(fsinfo.FullName);
                        s_LinkNames.Add(fsinfo.FullName);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
            }
        }
        /// <summary>
        /// 此方法可以无需管理员权限
        /// </summary>
        /// <param name="shortcutFilename"></param>
        /// <returns></returns>
        private static string GetShortcutTargetFileWithWshShell(string shortcutFilename)
        {
            try
            {
                if (File.Exists(shortcutFilename))
                {
                    WshShell shell = new WshShell();
                    IWshShortcut link = (IWshShortcut)shell.CreateShortcut(shortcutFilename);
                    return link.TargetPath;
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggingExtensions.LogError(logger, ex, "GetShortcutTargetFileWithWshShell");
            }
            return string.Empty;
        }
    }
}

