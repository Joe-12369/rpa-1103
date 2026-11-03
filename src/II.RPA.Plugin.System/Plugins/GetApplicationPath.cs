using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.RPASystem.Params;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;

namespace II.RPA.Plugin.RPASystem.Plugins
{
    public class GetApplicationPath : SystemComponentBase<ParamGetApplicationPathModel>
    {
        private static ILogger logger = Logger.OfContext();

        protected override eComponentType ComponentType => eComponentType.GetApplicationPath;

        protected override void NormalizeParameter(ParamGetApplicationPathModel paramModel)
        {
            if (paramModel.AppProcessNames == null)
            {
                paramModel.AppProcessNames = new string[0];
            }
            if (paramModel.DisplayNames == null)
            {
                paramModel.DisplayNames = new string[0];
            }
            if (paramModel.ExceFileNames == null)
            {
                paramModel.ExceFileNames = new string[0];
            }
        }

        protected override JMessage Execute(ParamGetApplicationPathModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext context)
        {
            JMessage msg = new JMessage();
            try
            {
                string sourceType = "ProcessName";
                string appPatch = GetAppPathFromProcessList(paramModel);
                if (string.IsNullOrEmpty(appPatch))
                {
                    sourceType = "Lnk";
                    appPatch = GetAppPathFromLnk(paramModel);
                }
                if (string.IsNullOrEmpty(appPatch))
                {
                    sourceType = "Regedit";
                    appPatch = GetAppPathFromRegedit(paramModel);
                }
                if (string.IsNullOrEmpty(appPatch))
                {
                    sourceType = string.Empty;
                }
                msg.Info = new JObject() {
                    { "InstallLocation",appPatch},
                    { "SourceType",sourceType}
                }.ToString();
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                Exception(ex, eErrorMessageType.Error_GetApplicationPathError, ref msg);
            }

            return msg;
        }

        #region 业务代码
        private string GetAppPathFromProcessList(ParamGetApplicationPathModel paramModel)
        {
            string appPatch = string.Empty;
            try
            {
                foreach (var processName in paramModel.AppProcessNames)
                {
                    var processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(processName));
                    if (processes.Length > 0)
                    {
                        appPatch = ExtractViewData.GetMainModuleFilepath(processes[0].Id, out _);
                    }
                    processes.Dispose();
                    if (!string.IsNullOrEmpty(appPatch))
                    {
                        return appPatch;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning($"通过进程信息获取应用路径异常：{ex}");
            }
            return appPatch;
        }
        private string GetAppPathFromRegedit(ParamGetApplicationPathModel paramModel)
        {
            if (paramModel.DisplayNames.Length == 0)
            {
                return string.Empty;
            }
            string regeditPath;
            if (Distinguish64or32System() == "32")
            {
                regeditPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall";
            }
            else
            {
                regeditPath = @"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall";
            }
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(regeditPath))
            {
                if (key != null)
                {
                    // 获取所有子键（即各个已安装程序的键）
                    string[] subKeyNames = key.GetSubKeyNames();

                    foreach (string subKeyName in subKeyNames)
                    {
                        // 打开每个程序对应的子键
                        using (RegistryKey subKey = key.OpenSubKey(subKeyName))
                        {
                            // 读取显示名称和安装位置信息
                            string displayName = subKey.GetValue("DisplayName") as string;
                            // 输出程序信息
                            if (!string.IsNullOrEmpty(displayName) && paramModel.DisplayNames.Any(name => displayName.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0))
                            {
                                string displayIcon = subKey.GetValue("DisplayIcon") as string;
                                if (!string.IsNullOrEmpty(displayIcon))
                                {
                                    if (displayIcon.EndsWith(".exe"))
                                    {
                                        return displayIcon;
                                    }
                                    if (displayIcon.EndsWith("o") || displayIcon.IndexOf(".exe") == -1)
                                    {
                                        continue;
                                    }
                                    if ("01".IndexOf(displayIcon[displayIcon.Length - 1]) != -1)
                                    {
                                        return displayIcon.Substring(0, displayIcon.LastIndexOf(".exe") + 4);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return string.Empty;
        }
        private string Distinguish64or32System()
        {
            try
            {
                string addressWidth = String.Empty;
                ConnectionOptions mConnOption = new ConnectionOptions();
                ManagementScope mMs = new ManagementScope("\\\\localhost", mConnOption);
                ObjectQuery mQuery = new ObjectQuery("select AddressWidth from Win32_Processor");
                ManagementObjectSearcher mSearcher = new ManagementObjectSearcher(mMs, mQuery);
                ManagementObjectCollection mObjectCollection = mSearcher.Get();
                foreach (ManagementObject mObject in mObjectCollection)
                {
                    addressWidth = mObject["AddressWidth"].ToString();
                }
                return addressWidth;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return String.Empty;
            }
        }
        string currentUserIdentity = null;
        private string GetAppPathFromLnk(ParamGetApplicationPathModel paramModel)
        {
            if (string.IsNullOrEmpty(currentUserIdentity))
            {
                currentUserIdentity = Path.Combine(Environment.UserDomainName, Environment.UserName);
            }
            string desktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.DesktopDirectory);
            string commonDesktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.CommonDesktopDirectory);
            string startMenu = System.Environment.GetFolderPath(System.Environment.SpecialFolder.StartMenu);//开始菜单
            string commonStartMenu = System.Environment.GetFolderPath(System.Environment.SpecialFolder.CommonStartMenu);//开始菜单
            string programs = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Programs);//开始菜单——程序
            string commonPprograms = System.Environment.GetFolderPath(System.Environment.SpecialFolder.CommonPrograms);//开始菜单——程序
            var pathList = new string[] {
                desktopPath,commonDesktopPath,startMenu,commonStartMenu,programs,commonPprograms
            };
            string targetPath = string.Empty;
            foreach (var path in pathList)
            {
                logger.LogDebug($"获取快捷方式目录：{path}");
                targetPath = GetAppPathFromLnk(paramModel, path, true);
                if (!string.IsNullOrEmpty(targetPath))
                {
                    return targetPath;
                }
            }
            return targetPath;
        }
        private string GetAppPathFromLnk(ParamGetApplicationPathModel paramModel, string path, bool matchfirstLayer)
        {
            try
            {

                if (!Directory.Exists(path))//系统逻辑名
                {
                    return string.Empty;
                }
                //DirectorySecurity fileAcl = Directory.GetAccessControl(path);
                //var rules = fileAcl.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount))
                //    .OfType<FileSystemAccessRule>()
                //    .Where(i => i.IdentityReference.Value == currentUserIdentity)
                //    .ToList();
                //if (rules.Any(i => i.AccessControlType == AccessControlType.Deny))
                //{
                //    logger.LogDebug($"此目录无访问权限：{path}");
                //    return string.Empty;
                //}
                string[] lnkFilePathArr = Directory.GetFiles(path, "*.lnk");
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null)
                {
                    return string.Empty;
                }

                dynamic shell = Activator.CreateInstance(shellType);
                foreach (var lnkFilePath in lnkFilePathArr)
                {
                    dynamic lnkPath = shell.CreateShortcut(lnkFilePath);
                    string targetPath = lnkPath.TargetPath;
                    var targetFileName = Path.GetFileName(targetPath);
                    if (paramModel.ExceFileNames.Any(fn => fn.EqualIgnoreCase(targetFileName)))
                    {
                        logger.LogDebug($"使用快捷方式：{lnkFilePath}");
                        if (!System.IO.File.Exists(targetPath))
                        {
                            if (targetPath.Contains("Program Files (x86)"))
                            {
                                targetPath = targetPath.Replace("Program Files (x86)", "Program Files");
                            }
                        }
                        return targetPath;
                    }
                }
                if (!matchfirstLayer)
                {
                    return string.Empty;
                }
                var dirPaths = Directory.GetDirectories(path);
                foreach (var dirPath in dirPaths)
                {
                    string targetPath = GetAppPathFromLnk(paramModel, dirPath, false);
                    if (!string.IsNullOrEmpty(targetPath))
                    {
                        return targetPath;
                    }
                }

            }
            catch (Exception ex)
            {
                logger.LogDebug($"{ex.Message}");
            }
            return string.Empty;
        }
        #endregion
    }
}
