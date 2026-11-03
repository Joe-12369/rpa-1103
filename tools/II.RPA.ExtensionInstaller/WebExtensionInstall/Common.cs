using IWshRuntimeLibrary;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Management;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Xml.Linq;
using File = System.IO.File;
using Path = System.IO.Path;

namespace WebExtensionInstall
{
    public class Common
    {
        public static string InstallPath = string.Empty;
        public static string key;
        public static string ChromeExtensionId = "pcopnibgkbdnlaeagepigbboebdfejmb";
        public const string ChromeExtensionIdV2 = "nfiffhdmjkohbbolbamdboplicimjnpm";
        private static string ChromeExtensionVersion = "4.0.0.0";
        private static string ChromeExtensionVersionName = "4.0.0.0+beta-251028.60";
        private static string ChromeExtensionPrefJson = "";

        public static string regeditPath;
        private static string installPath;
        private static string installValue;
        private static string extesionPath;
        public const string REG_PATH = "Software\\ZiXun_SuperBrowser\\SuperBrowser";
        private static readonly NameValueCollection AppSettings = ConfigurationManager.AppSettings;
        public static int ChromeV3MinVersion = 128;
        public static string ChromeExFilePath;
        public static string ChromeExVersion;
        public static readonly string ChromeExResultFilePath;
        public static int CurrentSessionId => Process.GetCurrentProcess().SessionId;
        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern bool QueryFullProcessImageName(IntPtr hProcess, uint flags, StringBuilder exeName, ref uint nameLen);
        static Common()
        {
            ChromeExResultFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Z-Core", $"{GetSafeFileName()}.json");
            if (AppSettings != null && AppSettings.Count > 0)
            {
                if (AppSettings["Chrome.V3MinVersion"] != null)
                {
                    if (int.TryParse(AppSettings["Chrome.V3MinVersion"], out int minVer))
                    {
                        ChromeV3MinVersion = minVer;
                    }
                }
            }
        }
        public static void InitParams()
        {
            if (AppSettings != null && AppSettings.Count > 0)
            {
                if (AppSettings["Chrome.Id"] != null)
                {
                    ChromeExtensionId = AppSettings["Chrome.Id"];
                }
                if (AppSettings["Chrome.Version"] != null)
                {
                    ChromeExtensionVersion = AppSettings["Chrome.Version"];
                }
                if (AppSettings["Chrome.Version_Name"] != null)
                {
                    ChromeExtensionVersionName = AppSettings["Chrome.Version_Name"];
                }
                if (AppSettings["Chrome.Pf"] != null)
                {
                    ChromeExtensionPrefJson = AppSettings["Chrome.Pf"];
                }
            }
            ChromeExFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IIRPA\\RPAChromeExtension\\IIRPAChromeExtension.crx");
            key = "ii.rpa.chromenativemsg";
            regeditPath = "SOFTWARE\\Google\\Chrome\\NativeMessagingHosts";
            installPath = "SOFTWARE\\Policies\\Google\\Chrome\\ExtensionInstallWhitelist";
            installValue = "dkoieojamolcmboailagjcpjeibdiahd";
            extesionPath = "Software\\Google\\Chrome\\Extensions";
        }
        private static void InitParamsV2()
        {
            ChromeExFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IIRPA\\RPAChromeExtension\\IIRPAChromeExtensionV2.crx");
            ChromeExtensionId = "nfiffhdmjkohbbolbamdboplicimjnpm";
            ChromeExtensionVersion = "2.7";
            key = "ii.rpa.chromenativemsg";
            regeditPath = "SOFTWARE\\Google\\Chrome\\NativeMessagingHosts";
            installPath = "SOFTWARE\\Policies\\Google\\Chrome\\ExtensionInstallWhitelist";
            installValue = "dkoieojamolcmboailagjcpjeibdiahd";
            extesionPath = "Software\\Google\\Chrome\\Extensions";
        }
        public static void InitParams(bool isV3)
        {
            if (isV3)
            {
                InitParams();
                ChromeExVersion = "V3";
            }
            else
            {
                InitParamsV2();
                ChromeExVersion = "V2";
            }
        }

        public static bool IsHighVersionSogouExplorer(string path)
        {
            if (path != null)
            {
                var version = PrintFileVersionInfo(path);
                if (!string.IsNullOrEmpty(version))
                {
                    int.TryParse(version, out int result);
                    return result > 11;
                }
                return false;
            }
            return false;
        }
        /// <summary>
        /// 打印指定文件的详细信息
        /// </summary>
        /// <param name="path">指定文件的路径</param>
        private static string PrintFileVersionInfo(string path)
        {
            System.IO.FileInfo fileInfo = null;
            try
            {
                fileInfo = new System.IO.FileInfo(path);
            }
            catch (Exception e)
            {
                LogError(e);
                // 其他处理异常的代码
            }
            // 如果文件存在
            if (fileInfo != null && fileInfo.Exists)
            {
                System.Diagnostics.FileVersionInfo info = System.Diagnostics.FileVersionInfo.GetVersionInfo(path);
                return info.FileVersion.Split('.')[0];
            }
            else
            {
                return "";
            }

        }

        public static bool VerifyWebBrowserVersion(string browserName, out bool fileMissing)
        {
            fileMissing = false;
            string filepath = string.Empty;
            int minSupportVersioin = 80;
            switch (browserName)
            {
                case "chrome":
                    filepath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Google\Chrome\Application\" + browserName + ".exe");
                    break;
                case "msedge":
                    filepath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\Edge\Application\" + browserName + ".exe");
                    break;
                case "firefox":
                    minSupportVersioin = 52;
                    filepath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Mozilla\Mozilla Firefox\" + browserName + ".exe");
                    break;
                default:
                    break;
            }

            if (!File.Exists(filepath))
            {
                RegistryKey key = Registry.LocalMachine;
                RegistryKey software = key.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\App Paths\\" + browserName + ".exe", false);
                filepath = software.GetValue("").ToString();
            }
            if (!string.IsNullOrEmpty(filepath) && File.Exists(filepath))
            {
                var version = PrintFileVersionInfo(filepath);
                int.TryParse(version, out int value);
                if (value >= minSupportVersioin)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            fileMissing = true;
            return false;

        }

        private static bool IsExistRegeditKey(string key, string path, RegistryKey registry)
        {
            RegistryKey keyNode = registry;
            RegistryKey software = keyNode.OpenSubKey(path);
            if (software != null)
            {
                RegistryKey registryKey = software.OpenSubKey(key);
                if (registryKey != null)
                {
                    registryKey.Close();
                    return true;
                }
                keyNode.Close();
                return false;
            }
            keyNode.Close();
            return false;
        }
        private static bool IsExist360RegeditKey(string key, string path, RegistryKey registry)
        {
            RegistryKey keyNode = registry;
            RegistryKey software = keyNode.OpenSubKey(path);
            if (software != null)
            {
                RegistryKey registryKey = software.OpenSubKey(key);
                if (registryKey != null)
                {
                    registryKey.Close();
                    return true;
                }
                keyNode.Close();
                return false;
            }
            keyNode.Close();
            return false;
        }
        private static RegistryKey OpenOrCreateSubKey(RegistryKey registryKey, string name, RegistryKeyPermissionCheck permissionCheck)
        {
            RegistryKey registry = registryKey.OpenSubKey(name, writable: true);
            if (registry == null)
            {
                registry = registryKey.CreateSubKey(name, permissionCheck);
            }
            return registry;
        }
        /// <summary>
        /// 写注册表 插件位置及版本信息
        /// </summary>
        private static void UserExtensionDetail()
        {
            using (RegistryKey keyNode = OpenOrCreateSubKey(Registry.CurrentUser, extesionPath, RegistryKeyPermissionCheck.ReadWriteSubTree))
            {
                RegistryKey node;
                keyNode.CreateSubKey(ChromeExtensionId);
                node = keyNode.OpenSubKey(ChromeExtensionId, true);
                node.SetValue("path", ChromeExFilePath);
                node.SetValue("version", ChromeExtensionVersion);
            }
        }
        private static void UserExtensionDetail(string extensionPath)
        {
            using (RegistryKey keyNode = OpenOrCreateSubKey(Registry.CurrentUser, extesionPath, RegistryKeyPermissionCheck.ReadWriteSubTree))
            {
                RegistryKey node;
                keyNode.CreateSubKey(ChromeExtensionId);
                node = keyNode.OpenSubKey(ChromeExtensionId, true);
                node.SetValue("path", extensionPath);
                node.SetValue("version", ChromeExtensionVersion);
            }
        }
        private static void UserInstallNativeMessage()
        {
            using (RegistryKey keyNode = OpenOrCreateSubKey(Registry.CurrentUser, regeditPath, RegistryKeyPermissionCheck.ReadWriteSubTree))
            {
                RegistryKey node;
                keyNode.CreateSubKey(key);
                node = keyNode.OpenSubKey(key, true);
                node.SetValue(null, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IIRPA\\RPAChromeExtension\\manifest.json"));
            }
        }
        public static bool IsExistRegeditKeyFireFox()
        {
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Mozilla\NativeMessagingHosts\ii.rpa.chromenativemsg");
            if (registryKey != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public static bool IsExistRegeditKeyEdge()
        {
            if (!TryGetEdgeExtensionSelection(out EdgeExtensionSelection selection))
            {
                return false;
            }

            string installDirectory = GetEdgeExtensionInstallDirectory();
            EdgeExtensionRegistryPlan plan = EdgeExtensionRegistryPlan.Create(selection, installDirectory);
            using (RegistryKey extensionKey = Registry.CurrentUser.OpenSubKey(plan.ExtensionRegistryPath))
            using (RegistryKey nativeHostKey = Registry.CurrentUser.OpenSubKey(plan.NativeHostRegistryPath))
            {
                return EdgeExtensionInstallation.IsInstalled(
                    selection,
                    installDirectory,
                    extensionKey?.GetValue("path") as string,
                    extensionKey?.GetValue("version") as string,
                    nativeHostKey?.GetValue(null) as string);
            }
        }
        public static bool FireFoxRegedit()
        {
            try
            {
                var xpiPath = Common.FindFireFoxXpiPath();
                if (xpiPath != null)
                    new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = Common.InstallPath,
                            Arguments = string.Format("\"{0}\"", xpiPath)
                        }
                    }.Start();


                RegistryKey registryKey = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Mozilla\NativeMessagingHosts\ii.rpa.chromenativemsg");
                if (registryKey != null)
                {
                    registryKey.SetValue("", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IIRPA\\RPAFireFoxExtension\\manifest.json"));

                    registryKey.SetValue("version", "2.7.8");
                }
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }
        public static bool SougouWriteRegedit()
        {
            try
            {
                using (RegistryKey keyNode = OpenOrCreateSubKey(Registry.CurrentUser, @"SOFTWARE\Google\Chrome\Extensions", RegistryKeyPermissionCheck.ReadWriteSubTree))
                {
                    RegistryKey node;
                    keyNode.CreateSubKey("dkjhpjbcmlpcffmplnoiajimipemjkgg");
                    node = keyNode.OpenSubKey("dkjhpjbcmlpcffmplnoiajimipemjkgg", true);
                    node.SetValue("path", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IIRPA\\RPASogouExtension\\IIRPASogouExtension.crx"));
                    node.SetValue("version", "2.7");
                }
                using (RegistryKey keyNode = OpenOrCreateSubKey(Registry.CurrentUser, @"SOFTWARE\Google\Chrome\NativeMessagingHosts", RegistryKeyPermissionCheck.ReadWriteSubTree))
                {
                    RegistryKey node;
                    keyNode.CreateSubKey("ii.rpa.sogounativemsg");
                    node = keyNode.OpenSubKey("ii.rpa.sogounativemsg", true);
                    node.SetValue(null, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IIRPA\\RPASogouExtension\\manifest.json"));
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public static bool EdgeWriteRegedit(EdgeExtensionSelection selection)
        {
            try
            {
                EdgeExtensionRegistryPlan plan = EdgeExtensionRegistryPlan.Create(
                    selection,
                    GetEdgeExtensionInstallDirectory());
                using (RegistryKey node = Registry.CurrentUser.CreateSubKey(plan.ExtensionRegistryPath))
                {
                    node.SetValue("path", plan.CrxPath);
                    node.SetValue("version", plan.ExtensionVersion);
                }
                using (RegistryKey node = Registry.CurrentUser.CreateSubKey(plan.NativeHostRegistryPath))
                {
                    node.SetValue(null, plan.NativeHostManifestPath);
                }
                return true;
            }
            catch (Exception e)
            {
                LogError(e);
                return false;
            }
        }
        public static bool WriteNativeMessageRegedit()
        {
            try
            {
                if (!IsExistRegeditKey(key, regeditPath, Registry.CurrentUser))
                {
                    UserInstallNativeMessage();
                }
                return true;
            }
            catch (Exception e)
            {
                LogError(e);
                return false;
            }
        }
        public static bool WriteRegedit()
        {
            try
            {
                //计算机\HKEY_CURRENT_USER\Software\Google\Chrome\Extensions  
                //计算机\HKEY_CURRENT_USER\Software\Google\Chrome\NativeMessagingHosts\ii.rpa.chromenativemsg
                //计算机\HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Google\Chrome\ExtensionInstallWhitelist
                if (!IsExistRegeditKey())
                {
                    UserExtensionDetail();
                    UserInstallNativeMessage();
                    //MachineInstallExtension();
                }
                return true;
            }
            catch (Exception e)
            {
                LogError(e);
                return false;
            }
        }
        public static bool WriteRegedit(string extensionPath)
        {
            try
            {
                if (!IsExistRegeditKey())
                {
                    UserExtensionDetail(extensionPath);
                    UserInstallNativeMessage();
                }
                return true;
            }
            catch (Exception e)
            {
                LogError(e);
                return false;
            }
        }


        public static void EnsureV2ExtensionId()
        {
            try
            {
                if (!IsExistRegeditKey(ChromeExtensionIdV2, extesionPath, Registry.CurrentUser))
                {
                    using (RegistryKey keyNode = OpenOrCreateSubKey(Registry.CurrentUser, extesionPath, RegistryKeyPermissionCheck.ReadWriteSubTree))
                    {
                        keyNode.CreateSubKey(ChromeExtensionIdV2);
                    }
                }
            }
            catch
            {
                //ignore
            }
        }
        public static bool IsExistSogouRegeditKey()
        {
            string path = "SOFTWARE\\Google\\Chrome\\Extensions";
            if (IsExistRegeditKey("dkjhpjbcmlpcffmplnoiajimipemjkgg", path, Registry.CurrentUser) ||
               IsExistRegeditKey("ii.rpa.sogounativemsg", "SOFTWARE\\Google\\Chrome\\NativeMessagingHosts", Registry.CurrentUser))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public static bool IsExistRegeditKey()
        {
            if (IsExistRegeditKey(ChromeExtensionId, extesionPath, Registry.CurrentUser) &&
               IsExistRegeditKey(key, regeditPath, Registry.CurrentUser))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public static bool IsExist360XRegeditKey()
        {
            string path = "Software\\360ChromeX\\Chrome\\Extensions";
            if ((IsExist360RegeditKey("leabadfdkgaidioombodahloaffmjidi", path, Registry.CurrentUser) ||
                IsExist360RegeditKey("leabadfdkgaidioombodahloaffmjidi", "Software\\360chrome\\Chrome\\Extensions\\", Registry.CurrentUser)) &&
               IsExist360RegeditKey("ii.rpa.chromenativemsg360X", regeditPath, Registry.CurrentUser))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public static bool IsExist360RegeditKey()
        {
            string path = "Software\\360\\360se6\\Chrome\\Extensions";
            if (IsExist360RegeditKey("decdnjjmiebjighpimpjhcppghkgbgbk", path, Registry.CurrentUser) &&
               IsExist360RegeditKey("ii.rpa.chromenativemsg360", regeditPath, Registry.CurrentUser))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public static void DeleteEdgeExtension()
        {
            try
            {
                using (RegistryKey extensions = Registry.CurrentUser.OpenSubKey(
                    @"SOFTWARE\Microsoft\Edge\Extensions",
                    writable: true))
                {
                    extensions?.DeleteSubKeyTree(EdgeExtensionPolicy.V2ExtensionId, throwOnMissingSubKey: false);
                    extensions?.DeleteSubKeyTree(EdgeExtensionPolicy.V3ExtensionId, throwOnMissingSubKey: false);
                    extensions?.DeleteSubKeyTree(EdgeExtensionPolicy.LegacyTemporaryV3ExtensionId, throwOnMissingSubKey: false);
                }
                using (RegistryKey nativeHosts = Registry.CurrentUser.OpenSubKey(
                    @"SOFTWARE\Microsoft\Edge\NativeMessagingHosts",
                    writable: true))
                {
                    nativeHosts?.DeleteSubKeyTree(EdgeExtensionInstallation.NativeHostName, throwOnMissingSubKey: false);
                }

                string userDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Microsoft",
                    "Edge",
                    "User Data");
                foreach (string profilePath in GetProfilePathArray(userDataPath))
                {
                    ChromiumExtensionPreferences.TryCleanProfile(
                        profilePath,
                        new[]
                        {
                            EdgeExtensionPolicy.V2ExtensionId,
                            EdgeExtensionPolicy.V3ExtensionId,
                            EdgeExtensionPolicy.LegacyTemporaryV3ExtensionId
                        });
                }
            }
            catch (Exception e)
            {
                LogError(e);
            }
        }
        public static void DeleteExtension(bool isv2 = true)
        {
            if (IsExistRegeditKey("dkoieojamolcmboailagjcpjeibdiahd", extesionPath, Registry.CurrentUser))
            {
                using (RegistryKey keyNode = OpenOrCreateSubKey(Registry.CurrentUser, extesionPath, RegistryKeyPermissionCheck.ReadWriteSubTree))
                {
                    keyNode.DeleteSubKeyTree("dkoieojamolcmboailagjcpjeibdiahd");
                }
            }

            if (IsExistRegeditKey(ChromeExtensionId, extesionPath, Registry.CurrentUser))
            {
                using (RegistryKey keyNode = OpenOrCreateSubKey(Registry.CurrentUser, extesionPath, RegistryKeyPermissionCheck.ReadWriteSubTree))
                {
                    keyNode.DeleteSubKeyTree(ChromeExtensionId);
                }
            }

            if (IsExistRegeditKey(key, regeditPath, Registry.CurrentUser))
            {
                using (RegistryKey keyNode = OpenOrCreateSubKey(Registry.CurrentUser, regeditPath, RegistryKeyPermissionCheck.ReadWriteSubTree))
                {
                    keyNode.DeleteSubKeyTree("ii.rpa.chromenativemsg");
                }
            }
            if (isv2)
            {
                Common.ClearPreference("Google\\Chrome\\User Data", "dkoieojamolcmboailagjcpjeibdiahd");
                Common.ClearPreference("Google\\Chrome\\User Data", ChromeExtensionId);
            }         

            //if (IsExistRegeditKey(value, installPath))
            //{
            //    using (RegistryKey keyNode = OpenOrCreateSubKey(Registry.LocalMachine, installPath, RegistryKeyPermissionCheck.ReadWriteSubTree))
            //    {
            //        foreach (var item in keyNode.GetValueNames())
            //        {
            //            if (keyNode.GetValue(item).Equals(key))
            //            {
            //                keyNode.DeleteSubKeyTree(item);
            //            }
            //        }
            //    }
            //}


        }

        //public static bool IsExistRegeditKey(string key, string path)
        //{
        //    RegistryKey keyNode = Registry.LocalMachine;
        //    RegistryKey software = keyNode.OpenSubKey(path);
        //    if (software != null)
        //    {
        //        foreach (var item in software.GetValueNames())
        //        {
        //            if (software.GetValue(item).Equals(key))
        //            {
        //                keyNode.Close();
        //                return true;
        //            }
        //        }
        //        keyNode.Close();
        //        return false;
        //    }
        //    keyNode.Close();
        //    return false;
        //}
        private static void DeleteUninstallRecordIfNeed(string filepath, string crxId)
        {
            bool needRewrite = false;
            string prefencesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), filepath + "\\Default\\Preferences");
            bool flag = File.Exists(prefencesPath);
            if (flag)
            {
                string content = File.ReadAllText(prefencesPath);
                JObject jContent = JObject.Parse(content);
                JToken jtoken = jContent["extensions"];
                List<string> list;
                if (jtoken == null)
                {
                    list = null;
                }
                else
                {
                    JToken jtoken2 = jtoken["external_uninstalls"];
                    list = ((jtoken2 != null) ? jtoken2.ToObject<List<string>>() : null);
                }
                List<string> uninstallExtensionLists = list;
                bool flag2 = uninstallExtensionLists != null && uninstallExtensionLists.IndexOf(crxId) != -1;
                if (flag2)
                {
                    uninstallExtensionLists.Remove(crxId);
                    jContent["extensions"]["external_uninstalls"] = new JArray(uninstallExtensionLists);
                    needRewrite = true;
                }
                bool flag3 = needRewrite;
                if (flag3)
                {
                    content = JsonConvert.SerializeObject(jContent);
                    File.WriteAllText(prefencesPath, content);
                }
            }
        }

        public static void DeleteUninstallRecordIfNeeded(string userDataPath)
        {
            string baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), userDataPath);

            var dirs1 = Directory.GetDirectories(baseDir, "Default", SearchOption.TopDirectoryOnly);
            var dirs2 = Directory.GetDirectories(baseDir, "Profile*", SearchOption.TopDirectoryOnly);

            var allProfiles = dirs1.Concat(dirs2).ToArray();
            foreach (string path in allProfiles)
            {
                var extPath = Path.Combine(path, "Extensions", ChromeExtensionId);
                if (Directory.Exists(extPath))
                {
                    Directory.Delete(extPath, true);
                }
                string prefencePath = Path.Combine(path, "Preferences");
                string content = File.ReadAllText(prefencePath);
                JObject jContent = JObject.Parse(content);
                JToken jtoken = jContent["extensions"];
                List<string> list;
                if (jtoken == null)
                {
                    list = null;
                    continue;
                }
                else
                {
                    JToken jtoken2 = jtoken["external_uninstalls"];
                    list = ((jtoken2 != null) ? jtoken2.ToObject<List<string>>() : new List<string>());
                }
                if (list?.IndexOf(ChromeExtensionId) >= 0)
                {
                    list.Remove(ChromeExtensionId);
                }
                else
                {
                    continue;
                }
                jContent["extensions"]["external_uninstalls"] = new JArray(list);
                content = JsonConvert.SerializeObject(jContent);
                File.WriteAllText(prefencePath, content);
            }
        }
        private static List<string> GetProfilePathArray(string userDataPath)
        {
            if (!new DirectoryInfo(userDataPath).Exists)
            {
                return new List<string>();
            }
            JObject jContent = JObject.Parse(File.ReadAllText(Path.Combine(userDataPath, "Local State")));
            JToken jtoken = jContent["profile"];
            if (((jtoken != null) ? jtoken["info_cache"] : null) == null)
            {
                return new List<string>();
            }
            List<string> result = new List<string>();
            JToken jtoken2 = jContent["profile"];
            foreach (JToken jtoken3 in ((IEnumerable<JToken>)((jtoken2 != null) ? jtoken2["info_cache"] : null)))
            {
                JProperty jProperty = jtoken3.ToObject<JProperty>();
                string dirName = Path.Combine(userDataPath, jProperty.Name);
                result.Add(dirName);
            }
            return result;
        }
        public static void ClearPreference(string filepath, string crxId)
        {
            //DeleteUninstallRecordIfNeed(filepath, crxId);
            foreach (string path in Directory.GetDirectories(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), filepath)))
            {
                var p = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), filepath);
                try
                {
                    string extpath = Path.Combine(path, $"Extensions\\{crxId}");
                    if (Directory.Exists(extpath))
                    {
                        Directory.Delete(extpath, true);
                    }
                }
                catch (Exception)
                {
                }
                string sppath = Path.Combine(path, "Secure Preferences");
                if (File.Exists(sppath))
                {
                    try
                    {
                        // 获取当前文件属性
                        FileAttributes attributes = File.GetAttributes(sppath);

                        // 检查是否包含“隐藏”属性
                        if ((attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                        {
                            // 通过位运算去除隐藏属性
                            attributes &= ~FileAttributes.Hidden;
                            File.SetAttributes(sppath, attributes);

                            Console.WriteLine("文件隐藏状态已取消。");
                        }

                        string json = "";
                        using (StreamReader streamReader = File.OpenText(sppath))
                        {
                            json = streamReader.ReadToEnd();
                        }
                        JObject jobject = JObject.Parse(json);
                        try
                        {
                            JObject extJobj = (JObject)jobject["extensions"]["settings"];
                            if (extJobj != null)
                            {
                                extJobj.Remove(crxId);
                            }
                        }
                        catch (Exception)
                        {
                        }
                        try
                        {
                            JObject proJobj = (JObject)jobject["protection"]["macs"]["extensions"]["settings"];
                            if (proJobj != null)
                            {
                                proJobj.Remove(crxId);
                            }
                        }
                        catch (Exception)
                        {
                        }
                        using (StreamWriter streamWriter = new StreamWriter(new FileStream(sppath, FileMode.Create, FileAccess.ReadWrite, FileShare.None)))
                        {
                            string value = jobject.ToString(Formatting.None, new JsonConverter[0]);
                            streamWriter.Write(value);
                        }
                    }
                    catch (Exception arg)
                    {

                    }
                }
                string prepath = Path.Combine(path, "Preferences");
                if (File.Exists(prepath))
                {

                    try
                    {
                        string jsonpre = "";
                        using (StreamReader streamReader2 = File.OpenText(prepath))
                        {
                            jsonpre = streamReader2.ReadToEnd();
                        }
                        JObject jobj = JObject.Parse(jsonpre);
                        try
                        {

                            try
                            {
                                JToken jtoken = jobj["extensions"]["install_signature"];
                                if (jtoken != null)
                                {
                                    jtoken.Remove();
                                }
                            }
                            catch (Exception)
                            {
                            }
                            try
                            {
                                JToken jtoken2 = jobj["extensions"]["external_uninstalls"];
                                if (jtoken2 is JArray)
                                {
                                    ((JArray)jtoken2).RemoveAll();
                                }
                            }
                            catch (Exception)
                            {
                            }
                            try
                            {
                                JToken jtoken3 = jobj["extensions"]["toolbar"];
                                if (jtoken3 is JArray)
                                {
                                    JArray jarray = (JArray)jtoken3;
                                    if (jarray != null && jarray.Contains(crxId))
                                    {
                                        jarray.Remove(crxId);
                                    }
                                }
                            }
                            catch (Exception)
                            {
                            }
                            try
                            {
                                JObject updateJobj = (JObject)jobj["updateclientdata"]["apps"];
                                if (updateJobj != null)
                                {
                                    updateJobj.Remove(crxId);
                                }
                            }
                            catch (Exception)
                            {
                            }

                        }
                        catch (Exception)
                        {

                        }
                        using (StreamWriter streamWriter2 = new StreamWriter(new FileStream(prepath, FileMode.Create, FileAccess.ReadWrite, FileShare.None)))
                        {
                            string value2 = jobj.ToString(Formatting.None, new JsonConverter[0]);
                            streamWriter2.Write(value2);
                        }

                    }
                    catch (Exception arg2)
                    {
                    }
                }
            }
        }


        public static string FindFireFoxXpiPath()
        {
            var localPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            DirectoryInfo dir = new DirectoryInfo(Path.Combine(localPath, "IIRPA\\RPAFireFoxExtension"));
            FileSystemInfo[] fileinfo = dir.GetFileSystemInfos();
            foreach (var file in fileinfo)
            {
                if (file.Name.Contains(".xpi"))
                    return file.FullName;
            }
            return null;
        }
        public static void EdgeLoggin(object obj)
        {
            string content = JsonConvert.SerializeObject(obj);
            string logfile = AppDomain.CurrentDomain.BaseDirectory + "EdgeExtension.txt";
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(content);
            File.WriteAllText(logfile, stringBuilder.ToString());
        }
        public static void FireFoxLoggin(object obj)
        {
            string content = JsonConvert.SerializeObject(obj);
            string logfile = AppDomain.CurrentDomain.BaseDirectory + "FireFoxExtension.txt";
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(content);
            File.WriteAllText(logfile, stringBuilder.ToString());
        }
        public static void ChromiumLoggin(object obj)
        {
            string content = JsonConvert.SerializeObject(obj);
            string logfile = AppDomain.CurrentDomain.BaseDirectory + "ChromiumExtension.txt";
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(content);
            File.WriteAllText(logfile, stringBuilder.ToString());
        }
        public static void ChromeLoggin(object obj)
        {
            string content = JsonConvert.SerializeObject(obj);
            string logfile = AppDomain.CurrentDomain.BaseDirectory + "ChromeExtension.txt";
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(content);
            File.WriteAllText(logfile, stringBuilder.ToString());
        }
        public static void SogouLoggin(object obj)
        {
            string content = JsonConvert.SerializeObject(obj);
            string logfile = AppDomain.CurrentDomain.BaseDirectory + "SogouExtension.txt";
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(content);
            File.WriteAllText(logfile, stringBuilder.ToString());
        }
        public static void SuperBrowserLoggin(object obj)
        {
            string content = JsonConvert.SerializeObject(obj);
            string logfile = AppDomain.CurrentDomain.BaseDirectory + "SuperBrowserExtension.txt";
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(content);
            File.WriteAllText(logfile, stringBuilder.ToString());
        }
        public static void LogError(Exception ex)
        {
            string logfile = Path.Combine(Path.GetTempPath(), "Z-RPAWebExtension.log");
            StringBuilder stringBuilder = new StringBuilder();
            do
            {
                stringBuilder.AppendLine(ex.Message);
                stringBuilder.AppendLine(ex.StackTrace);
                ex = ex.InnerException;
            }
            while (ex != null);
            File.WriteAllText(logfile, stringBuilder.ToString());
        }
        public static void DirectoryCopy(string sourceDirectory,
            string targetDirectory,
            string[] ignoreFileNames = null)
        {
            var considerIgnore = ignoreFileNames != null && ignoreFileNames.Any();
            DirectoryInfo dir = new DirectoryInfo(sourceDirectory);
            //获取目录下（不包含子目录）的文件和子目录
            FileSystemInfo[] fileinfo = dir.GetFileSystemInfos();
            foreach (var file in fileinfo)
            {
                //判断是否文件夹
                if (file is DirectoryInfo)
                {
                    if (!Directory.Exists(Path.Combine(targetDirectory, file.Name)))
                    {
                        //目标目录下不存在此文件夹即创建子文件夹
                        Directory.CreateDirectory(Path.Combine(targetDirectory, file.Name));
                    }
                    //递归调用复制子文件夹
                    DirectoryCopy(file.FullName, Path.Combine(targetDirectory, file.Name));
                }
                else
                {
                    if (!Directory.Exists(targetDirectory))
                    {
                        Directory.CreateDirectory(targetDirectory);
                    }
                    if (considerIgnore && ignoreFileNames.Contains(file.Name))
                    {
                        try
                        {
                            Console.WriteLine($"Ignore file:{file.Name}");
                            File.Copy(file.FullName, Path.Combine(targetDirectory, file.Name), false);
                        }
                        catch (IOException)
                        {
                            //ignore
                        }
                    }
                    else
                    {
                        File.Copy(file.FullName, Path.Combine(targetDirectory, file.Name), true);
                    }
                }
            }
        }
        private static bool DeleteXpi(string name)
        {
            try
            {
                var file = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), $"IIRPA\\RPA{name}Extension");
                var files = Directory.GetFiles(file).Where(e => e.Contains(".xpi"));
                foreach (var item in files)
                {
                    if (item.Contains(".xpi"))
                        if (File.Exists(item))
                            File.Delete(item);
                }
                return true;
            }
            catch (Exception e)
            {
                LogError(e);
            }
            return true;
        }
        public static bool CopyHostFileToPath(string name, string[] ignoreFileNames = null)
        {
            try
            {

                try
                {
                    DeleteXpi(name);
                    DirectoryCopy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"RPA{name}Extension"),
                                  Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), $"IIRPA\\RPA{name}Extension"),
                                  ignoreFileNames);

                    return true;
                }
                catch (IOException)
                {
                    if (name == "Chrome")
                    {
                        string destDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), $"IIRPA\\RPA{name}Extension");
                        //string fileName = Path.GetFileName(destDirectory);
                        var parentDir = Directory.GetParent(destDirectory);
                        string backupdestDirectory = Path.Combine(parentDir.FullName, $"iibak");
                        if (Directory.Exists(destDirectory))
                        {
                            var destFiles = Directory.GetFiles(destDirectory);
                            if (destFiles.Any())
                            {
                                if (Directory.Exists(backupdestDirectory))
                                {
                                    Directory.Delete(backupdestDirectory, true);
                                }
                                Directory.CreateDirectory(backupdestDirectory);
                                foreach (var destFile in destFiles)
                                {
                                    var backupdestFileName = Path.Combine(backupdestDirectory, new FileInfo(destFile).Name);
                                    if (File.Exists(destFile))
                                    {
                                        File.Move(destFile, backupdestFileName);
                                    }
                                }
                                DirectoryCopy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"RPA{name}Extension"),
                                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), $"IIRPA\\RPA{name}Extension"));
                                return true;
                            }
                        }
                    }
                    else
                    {
                        throw;
                    }
                }

            }
            catch (Exception e)
            {
                LogError(e);
                return false;
            }
            return false;
        }
        public static bool FindEdgePath()
        {
            return TryGetEdgePath(out _);
        }

        public static bool TryGetEdgePath(out string path)
        {
            try
            {
                path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    @"Microsoft\Edge\Application\msedge.exe");
                if (!File.Exists(path))
                {
                    using (RegistryKey software = Registry.LocalMachine.OpenSubKey(
                        "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\App Paths\\msedge.exe",
                        false))
                    {
                        path = software?.GetValue(null) as string;
                    }
                }
                return !string.IsNullOrWhiteSpace(path) && File.Exists(path);
            }
            catch (Exception e)
            {
                LogError(e);
                path = null;
                return false;
            }
        }

        public static bool TryGetEdgeExtensionSelection(out EdgeExtensionSelection selection)
        {
            selection = null;
            if (!TryGetEdgePath(out string path))
            {
                return false;
            }

            selection = EdgeExtensionPolicy.Select(GetMainVersion(path));
            return true;
        }

        public static string GetEdgeExtensionInstallDirectory()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "IIRPA",
                "RPAEdgeExtension");
        }

        public static bool RemoveLegacyEdgeNativeHostFiles()
        {
            try
            {
                EdgeInstallDirectoryCleaner.RemoveLegacyNativeHostFiles(GetEdgeExtensionInstallDirectory());
                return true;
            }
            catch (Exception e)
            {
                LogError(e);
                return false;
            }
        }

        public static bool FindSogouPath(out string path)
        {
            path = null;
            try
            {
                var SogouPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"SogouExplorer\SogouExplorer.exe");
                if (File.Exists(SogouPath))
                {
                    path = SogouPath;
                    return true;
                }
                else
                {
                    try
                    {
                        //计算机\HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\Sogou.exe
                        RegistryKey key = Registry.CurrentUser;
                        RegistryKey software = key.OpenSubKey("SOFTWARE\\SogouExplorer", false);
                        if (software == null)
                        {
                            software = key.OpenSubKey("SOFTWARE\\Sogou\\SogouExplorer", false);
                            path = software.GetValue("").ToString() + "\\SogouExplorer.exe";
                            return true;
                        }
                        else
                        {
                            var result = software.GetValue("")?.ToString();
                            if (string.IsNullOrEmpty(result))
                            {
                                software = key.OpenSubKey("SOFTWARE\\Sogou\\SogouExplorer", false);
                                path = software.GetValue("").ToString() + "\\SogouExplorer.exe";
                                return true;
                            }
                        }
                        path = software.GetValue("").ToString() + "\\SogouExplorer.exe";
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
                return false;
            }
        }
        public static bool FindChromePath()
        {
            try
            {
                var chromePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Google\Chrome\Application\chrome.exe");
                if (!File.Exists(chromePath))
                {
                    RegistryKey key = Registry.LocalMachine;
                    RegistryKey software = key.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\App Paths\\chrome.exe", false);
                    if (software != null)
                        return true;
                    else
                        return false;
                }
                return true;
            }
            catch (Exception e)
            {
                LogError(e);
                return false;
            }
        }
        public static bool FindFireFoxPath()
        {
            try
            {
                RegistryKey key = Registry.LocalMachine;
                RegistryKey software = key.OpenSubKey($"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\App Paths\\firefox.exe", false);
                if (software != null)
                {
                    InstallPath = software.GetValue("").ToString();
                    return true;
                }
                else
                {
                    InstallPath = null;
                    return false;
                }
            }
            catch (Exception e)
            {
                LogError(e);
                return false;
            }
        }
        /// <summary>
        /// 获取进程 
        /// </summary>
        /// <returns></returns>
        public static bool IsExistBrowserProcess(string processName)
        {
            Process[] processes = Process.GetProcessesByName(processName);
            if (processes.Length > 0 && processes.Any(p => p.SessionId == CurrentSessionId))
            {
                return true;
            }
            return false;
        }
        public static string Verify360XPath(string exePath)
        {
            var chromePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), exePath);
            var chromeLocalPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), exePath);
            if (!File.Exists(chromePath) && !File.Exists(chromeLocalPath))
            {
                RegistryKey key = Registry.LocalMachine;
                RegistryKey software = key.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\App Paths\\360chrome.exe", false);
                if (software != null)
                {
                    return software.GetValue("").ToString();

                }
                else
                {
                    RegistryKey keyUser = Registry.CurrentUser;
                    RegistryKey softwareUser = keyUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\App Paths\\360chrome.exe", false);
                    if (softwareUser != null)
                    {
                        return softwareUser.GetValue("").ToString();

                    }
                }
            }
            if (File.Exists(chromePath))
            {
                return chromePath;
            }
            else if (File.Exists(chromeLocalPath))
            {
                return chromeLocalPath;
            }
            return string.Empty;
        }

        public static bool Find360XPath(out string path)
        {
            path = null;
            try
            {
                //360Chrome\Chrome\Application\360chrome.exe
                //360ChromeX\Chrome\Application\360ChromeX.exe
                path = Verify360XPath("360ChromeX\\Chrome\\Application\\360ChromeX.exe");
                if (string.IsNullOrEmpty(path))
                    path = Verify360XPath("360Chrome\\Chrome\\Application\\360chrome.exe");

                if (string.IsNullOrEmpty(path))
                    return false;
                //var chromePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"360Chrome\Chrome\Application\360chrome.exe");
                //var chromeLocalPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"360Chrome\Chrome\Application\360chrome.exe");
                //if (!File.Exists(chromePath) && !File.Exists(chromeLocalPath))
                //{
                //    RegistryKey key = Registry.LocalMachine;
                //    RegistryKey software = key.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\App Paths\\360chrome.exe", false);
                //    if (software != null)
                //    {
                //        path = software.GetValue("").ToString();
                //        return true;
                //    }
                //    else
                //    {
                //        RegistryKey keyUser = Registry.CurrentUser;
                //        RegistryKey softwareUser = keyUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\App Paths\\360chrome.exe", false);
                //        if (softwareUser != null)
                //        {
                //            path = softwareUser.GetValue("").ToString();
                //            return true;
                //        }
                //    }
                //    return false;
                //}
                //path = File.Exists(chromePath) ? chromePath : chromeLocalPath;
                return true;
            }
            catch (Exception e)
            {
                LogError(e);
                return false;
            }
        }

        public static bool Find360sePath(out string path)
        {
            path = null;
            try
            {
                var chromePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"360se6\Application\360se.exe");
                var chromeLocalPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"360se6\Application\360se.exe");
                if (!File.Exists(chromePath) && !File.Exists(chromeLocalPath))
                {
                    RegistryKey key = Registry.LocalMachine;
                    RegistryKey software = key.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\App Paths\\360se6.exe", false);
                    if (software != null)
                    {
                        path = software.GetValue("").ToString();
                        return true;
                    }
                    else
                    {
                        RegistryKey keyUser = Registry.CurrentUser;
                        RegistryKey softwareUser = keyUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\App Paths\\360se6.exe", false);
                        if (softwareUser != null)
                        {
                            path = softwareUser.GetValue("").ToString();
                            return true;
                        }
                    }
                    return false;
                }
                path = File.Exists(chromePath) ? chromePath : chromeLocalPath;
                return true;
            }
            catch (Exception e)
            {
                LogError(e);
                return false;
            }
        }
        public static string FindSogoucrxPath()
        {
            var localPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localPath, "IIRPA\\RPASogouExtension\\IIRPASogouExplorerExtension.crx");
        }
        public static string Find360XcrxPath()
        {
            var localPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            DirectoryInfo dir = new DirectoryInfo(Path.Combine(localPath, "IIRPA\\RPA360XExtension"));
            FileSystemInfo[] fileinfo = dir.GetFileSystemInfos();
            foreach (var file in fileinfo)
            {
                if (file.Name.Contains(".crx"))
                    return file.FullName;
            }
            return null;
        }
        public static bool Write360XRegedit()
        {
            try
            {
                try
                {

                    Find360XPath(out string browser);
                    var xpiPath = Common.Find360XcrxPath();
                    if (xpiPath != null)
                        new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = browser,
                                //FileName = Common.InstallPath,
                                //Arguments = string.Format("\"{0}\"", xpiPath)
                                Arguments = xpiPath
                            }
                        }.Start();

                }
                catch (Exception e)
                {
                    LogError(e);
                }
                RegistryKey registryKey = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\360ChromeX\Chrome\Extensions\leabadfdkgaidioombodahloaffmjidi");
                //RegistryKey registryKey = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\360Chrome\Chrome\Extensions\ddgokbflgnaepdnleolcgheoeejccolh");
                //RegistryKey registryKey = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\360\360se6\NativeMessagingHosts\ii.rpa.chromenativemsg");
                if (registryKey != null)
                {
                    registryKey.SetValue("path", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IIRPA\\RPA360XExtension\\IIRPA360XExtension.crx"));

                    registryKey.SetValue("version", "2.7");
                }
                registryKey = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\360chrome\Chrome\Extensions\leabadfdkgaidioombodahloaffmjidi");
                //RegistryKey registryKey = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\360Chrome\Chrome\Extensions\ddgokbflgnaepdnleolcgheoeejccolh");
                //RegistryKey registryKey = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\360\360se6\NativeMessagingHosts\ii.rpa.chromenativemsg");
                if (registryKey != null)
                {
                    registryKey.SetValue("path", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IIRPA\\RPA360XExtension\\IIRPA360XExtension.crx"));

                    registryKey.SetValue("version", "2.7");
                }
                RegistryKey registryKey1 = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Google\Chrome\NativeMessagingHosts\ii.rpa.chromenativemsg360x");
                //RegistryKey registryKey = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\360\360se6\NativeMessagingHosts\ii.rpa.chromenativemsg");
                if (registryKey1 != null)
                {
                    registryKey1.SetValue("", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IIRPA\\RPA360XExtension\\manifest.json"));
                }
                return true;
            }
            catch (Exception e)
            {
                LogError(e);
                return false;
            }
        }
        public static bool Write360seRegedit()
        {
            //try
            //{
            //   // using (RegistryKey keyNode = OpenOrCreateSubKey(Registry.CurrentUser, @"SOFTWARE\360\360se6\Chrome\Extensions", RegistryKeyPermissionCheck.ReadWriteSubTree))
            //    using (RegistryKey keyNode = OpenOrCreateSubKey(Registry.LocalMachine, @"SOFTWARE\Google\Chrome\Extensions", RegistryKeyPermissionCheck.ReadWriteSubTree))
            //    {
            //        RegistryKey node;
            //        keyNode.CreateSubKey("nfiffhdmjkohbbolbamdboplicimjnpm");
            //        node = keyNode.OpenSubKey("nfiffhdmjkohbbolbamdboplicimjnpm", true);
            //        node.SetValue("path", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IIRPA\\RPA360seExtension\\RPA360seExtension\\IIRPA360seExtension.crx"));
            //        node.SetValue("version", "2.4");
            //    }
            //    //using (RegistryKey keyNode = OpenOrCreateSubKey(Registry.CurrentUser, @"SOFTWARE\360\360se6\Chrome\NativeMessagingHosts", RegistryKeyPermissionCheck.ReadWriteSubTree))
            //    using (RegistryKey keyNode = OpenOrCreateSubKey(Registry.LocalMachine, @"SOFTWARE\Google\Chrome\NativeMessagingHosts", RegistryKeyPermissionCheck.ReadWriteSubTree))
            //    {
            //        RegistryKey node;
            //        keyNode.CreateSubKey("ii.rpa.chromenativemsg");
            //        node = keyNode.OpenSubKey("ii.rpa.chromenativemsg", true);
            //        node.SetValue(null, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IIRPA\\RPA360seExtension\\RPA360seExtension\\manifest.json"));
            //    }

            //    return true;
            //}
            //catch (Exception e)
            //{
            //    LogError(e);
            //    return false;
            //}
            try
            {
                try
                {

                    Find360sePath(out string browser);
                    var xpiPath = Common.Find360secrxPath();
                    if (xpiPath != null)
                        new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = browser,
                                //FileName = Common.InstallPath,
                                //Arguments = string.Format("\"{0}\"", xpiPath)
                                Arguments = xpiPath
                            }
                        }.Start();

                }
                catch (Exception e)
                {
                    LogError(e);
                }
                RegistryKey registryKey = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\360\360se6\Chrome\Extensions\decdnjjmiebjighpimpjhcppghkgbgbk");
                //RegistryKey registryKey = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\360\360se6\NativeMessagingHosts\ii.rpa.chromenativemsg");
                if (registryKey != null)
                {
                    registryKey.SetValue("path", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IIRPA\\RPA360seExtension\\IIRPA360seExtension.crx"));

                    registryKey.SetValue("version", "2.7");
                }
                RegistryKey registryKey1 = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Google\Chrome\NativeMessagingHosts\ii.rpa.chromenativemsg360");
                //RegistryKey registryKey = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\360\360se6\NativeMessagingHosts\ii.rpa.chromenativemsg");
                if (registryKey1 != null)
                {
                    registryKey1.SetValue("", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IIRPA\\RPA360seExtension\\manifest.json"));
                }
                return true;
            }
            catch (Exception e)
            {
                LogError(e);
                return false;
            }
        }

        public static string Find360secrxPath()
        {
            var localPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            DirectoryInfo dir = new DirectoryInfo(Path.Combine(localPath, "IIRPA\\RPA360seExtension"));
            FileSystemInfo[] fileinfo = dir.GetFileSystemInfos();
            foreach (var file in fileinfo)
            {
                if (file.Name.Contains(".crx"))
                    return file.FullName;
            }
            return null;
        }
        public static void Loggin360X(object obj)
        {
            string content = JsonConvert.SerializeObject(obj);
            string logfile = AppDomain.CurrentDomain.BaseDirectory + "360XExtension.txt";
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(content);
            File.WriteAllText(logfile, stringBuilder.ToString());
        }
        public static void Loggin360se(object obj)
        {
            string content = JsonConvert.SerializeObject(obj);
            string logfile = AppDomain.CurrentDomain.BaseDirectory + "360seExtension.txt";
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(content);
            File.WriteAllText(logfile, stringBuilder.ToString());
        }

        public static int GetBrowserService()
        {
            var _service = System.ServiceProcess.ServiceController.GetServices().Where(s => s.ServiceName.Equals("BrowserAutomationSvr"))?.FirstOrDefault();
            if (_service != null)
            {
                return (int)_service.Status;
            }
            return -1;
        }
        public static string GetBrowserServicePath()
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RPASogouExtension", "services");
            var file = path + "\\BrowserAutomationSvr.exe";
            if (File.Exists(file))
                return file;
            return "";
        }
        public static string GetSogouExtensionPath()
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RPASogouExtension");
            var file = path + "\\com.indeed.sogou.crx";
            if (File.Exists(file))
                return file;
            return "";
        }
        public static void StartBrowserService()
        {

            var _service = System.ServiceProcess.ServiceController.GetServices().Where(s => s.ServiceName.Equals("BrowserAutomationSvr"))?.FirstOrDefault();
            _service.Start();
        }

        public static bool FindSuperBrowser(out string browserPath)
        {
            browserPath = null;
            try
            {
                var publicUser = Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments));

                if (publicUser != null && publicUser.Exists)
                {
                    var superBrowserRoot = Path.Combine(publicUser.FullName, "SuperBrowser");
                    if (!string.IsNullOrEmpty(superBrowserRoot) && Directory.Exists(superBrowserRoot))
                    {
                        var subDirectoies = Directory.GetDirectories(superBrowserRoot);
                        if (subDirectoies != null && subDirectoies.Any())
                        {
                            var exePath = Path.Combine(superBrowserRoot, subDirectoies[0], "Core", "Launcher.exe");
                            if (File.Exists(exePath))
                            {
                                browserPath = exePath;
                                return true;
                            }
                        }
                    }
                }
                if (string.IsNullOrEmpty(browserPath) || !File.Exists(browserPath))
                {
                    RegistryKey key = Registry.CurrentUser;
                    RegistryKey software = key.OpenSubKey(REG_PATH, false);
                    if (software != null)
                    {
                        var value = software.GetValue("MainExeCoreFullPath");
                        if (value != null)
                        {
                            var mainExePath = value.ToString();
                            var mainExeDir = Path.GetDirectoryName(mainExePath);
                            browserPath = Path.Combine(mainExeDir, "Launcher.exe");
                        }
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public static bool IsExistRegeditKeySuperBrowser()
        {
            if (IsExistRegeditKey(key, regeditPath, Registry.CurrentUser))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public static void DeleteExtensionSuperBrowser()
        {
            if (IsExistRegeditKey(key, regeditPath, Registry.CurrentUser))
            {
                using (RegistryKey keyNode = OpenOrCreateSubKey(Registry.CurrentUser, regeditPath, RegistryKeyPermissionCheck.ReadWriteSubTree))
                {
                    keyNode.DeleteSubKeyTree("ii.rpa.chromenativemsg");
                }
            }

        }
        public static bool WriteRegeditSuperBrowser()
        {
            try
            {
                if (!IsExistRegeditKeySuperBrowser())
                {
                    UserInstallNativeMessage();
                }
                return true;
            }
            catch (Exception e)
            {
                LogError(e);
                return false;
            }
        }

        public static bool CopyHostFileToPathSuperBrowser(string name)
        {
            try
            {
                var file = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), $"IIRPA\\RPA{name}Extension");
                try
                {
                    DirectoryCopy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"RPA{name}Extension"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), $"IIRPA\\RPA{name}Extension"));
                }
                catch (System.IO.IOException)
                {
                    try
                    {
                        string destDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), $"IIRPA\\RPA{name}Extension");
                        //string fileName = Path.GetFileName(destDirectory);
                        var parentDir = Directory.GetParent(destDirectory);
                        string backupdestDirectory = Path.Combine(parentDir.FullName, $"iibak");
                        if (Directory.Exists(destDirectory))
                        {
                            var destFiles = Directory.GetFiles(destDirectory);
                            if (destFiles.Any())
                            {
                                if (Directory.Exists(backupdestDirectory))
                                {
                                    Directory.Delete(backupdestDirectory, true);
                                }
                                Directory.CreateDirectory(backupdestDirectory);
                                foreach (var destFile in destFiles)
                                {
                                    var backupdestFileName = Path.Combine(backupdestDirectory, new FileInfo(destFile).Name);
                                    if (File.Exists(destFile))
                                    {
                                        File.Move(destFile, backupdestFileName);
                                    }
                                }
                                DirectoryCopy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"RPA{name}Extension"),
                                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), $"IIRPA\\RPA{name}Extension"));
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        LogError(ex);
                        return false;
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                LogError(e);
                return false;
            }
        }

        /// <summary>
        /// IsProcessRunning 考虑会话id
        /// </summary>
        /// <param name="processName"></param>
        /// <returns></returns>
        public static bool IsProcessRunning(string processName)
        {
            try
            {
                var processes = Process.GetProcessesByName(processName);
                if (processes != null && processes.Length > 0)
                {
                    var sessionProcesses = processes.Where(p => p.SessionId == CurrentSessionId);
                    return sessionProcesses != null && sessionProcesses.Any();
                }
                return false;
            }
            catch (Exception e)
            {
                LogError(e);
                return false;
            }
        }

        /// <summary>
        /// CloseProcessWithName 考虑会话id
        /// </summary>
        /// <param name="processName"></param>
        public static void CloseProcessWithName(string processName, string exePath = null)
        {
            var processes = Process.GetProcessesByName(processName);
            if (processes != null && processes.Length > 0)
            {
                var sessionProcesses = processes.Where(p => p.SessionId == CurrentSessionId);
                if (sessionProcesses != null && sessionProcesses.Any())
                {
                    foreach (var item in sessionProcesses)
                    {
                        if (!string.IsNullOrEmpty(exePath) && !exePath.Equals(Common.GetExeFilePath(item), StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                        item.Kill();
                    }
                }
            }
        }

        public static bool TryGetChromePath(out string path)
        {
            try
            {
                var chromePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Google\Chrome\Application\chrome.exe");
                if (!File.Exists(chromePath))
                {
                    RegistryKey key = Registry.LocalMachine;
                    RegistryKey software = key.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\App Paths\\chrome.exe", false);
                    chromePath = software.GetValue("").ToString();
                }
                path = chromePath;
            }
            catch (Exception e)
            {
                LogError(e);
                path = "";
            }
            return !string.IsNullOrEmpty(path);
        }
        public static int GetMainVersion(string path)
        {
            FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(path);
            return versionInfo.FileMajorPart;
        }
        /// <summary>
        /// V2OrV3
        /// </summary>
        /// <returns>true:V3;false:V2</returns>
        public static bool ChromeV2OrV3()
        {
            if (TryGetChromePath(out string path))
            {
                var major = GetMainVersion(path);
                if (major < ChromeV3MinVersion)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            return false;
        }

        public static void SaveLatestChromeExVersionInfo()
        {
            JObject data = new JObject
            {
                { "version", ChromeExVersion }
            };
            string filePath = ChromeExResultFilePath;
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            string jsonString = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(filePath, jsonString);
        }
        public static JObject GetLatestChromeExVersionInfo()
        {
            string filePath = ChromeExResultFilePath;
            if (File.Exists(filePath))
            {
                string jsonString = File.ReadAllText(filePath);
                var data = JsonConvert.DeserializeObject<JObject>(jsonString);
                if (data.ContainsKey("version"))
                {
                    return data;
                }
            }
            return null;
        }

        public static string GetSafeFileName()
        {
            string invalidChars = new string(Path.GetInvalidFileNameChars());
            string pattern = $"[{Regex.Escape(invalidChars)}]";

            return Regex.Replace(Environment.UserName, pattern, "_");
        }

        public static List<string> GetLnkPaths()
        {
            string currentUserIdentity = Path.Combine(Environment.UserDomainName, Environment.UserName); ;
            string desktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.DesktopDirectory);
            string commonDesktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.CommonDesktopDirectory);
            string startMenu = System.Environment.GetFolderPath(System.Environment.SpecialFolder.StartMenu);//开始菜单
            string commonStartMenu = System.Environment.GetFolderPath(System.Environment.SpecialFolder.CommonStartMenu);//开始菜单
            string programs = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Programs);//开始菜单——程序
            string commonPprograms = System.Environment.GetFolderPath(System.Environment.SpecialFolder.CommonPrograms);//开始菜单——程序
            var pathList = new string[] {
                desktopPath,commonDesktopPath,startMenu,commonStartMenu,programs,commonPprograms
            };
            List<string> paths = new List<string>();
            foreach (var path in pathList)
            {
                paths.AddRange(GetLnkPaths(path, true));
            }
            return paths;
        }
        public static List<string> GetLnkTargetPaths(List<string> lnkpaths)
        {
            List<string> paths = new List<string>();
            WshShell shell = new WshShell();
            
            foreach (var lnkFilePath in lnkpaths)
            {
                IWshShortcut lnkPath = (IWshShortcut)shell.CreateShortcut(lnkFilePath);
                var targetPath = lnkPath.TargetPath;
                if (!System.IO.File.Exists(targetPath))
                {
                    if (targetPath.Contains("Program Files (x86)"))
                    {
                        targetPath = targetPath.Replace("Program Files (x86)", "Program Files");
                        if (!System.IO.File.Exists(targetPath))
                        {
                            targetPath = string.Empty; //如果仍然不存在，则设置为空
                        }
                    }
                    else
                    {
                        targetPath = string.Empty;
                    }
                }
                paths.Add(targetPath);
            }
            return paths;
        }
        public static List<string> GetLnkPaths(string path, bool matchfirstLayer)
        {
            List<string> paths = new List<string>();
            try
            {
                if (!Directory.Exists(path))//系统逻辑名
                {
                    return paths;
                }
                string[] lnkFilePathArr = Directory.GetFiles(path, "*.lnk");
                paths.AddRange(lnkFilePathArr);
                //只扫一层子文件夹
                if (!matchfirstLayer)
                {
                    return paths;
                }
                var dirPaths = Directory.GetDirectories(path);
                foreach (var dirPath in dirPaths)
                {
                    var targetPaths = GetLnkPaths(dirPath, false);
                    paths.AddRange(targetPaths);
                }

            }
            catch (Exception ex)
            {

            }
            return paths;
        }
        internal static List<string> GetRunningBrowserPaths(HashSet<string> allowExeNames)
        {
            var hit = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in Process.GetProcesses())
            {
                try
                {
                    var exeName = Path.GetFileNameWithoutExtension(p.ProcessName);
                    if (allowExeNames.Contains(exeName))          
                    {
                        var path = GetExeFilePath(p);
                        if (!string.IsNullOrEmpty(path))
                            hit.Add(path);
                    }
                }
                catch { /* 无权限 */ }
                finally
                {
                    p.Dispose();   // 立即释放句柄
                }
            }
            return hit.ToList();
        }
        internal static JObject GetUserConfigObj()
        {
            var localApplication = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var configFilePath = Path.Combine(localApplication, "IIRPA\\ChromiumBrowserConfig.json");
            if (!File.Exists(configFilePath))
            {
                var configFilePath_old = Path.Combine(localApplication, "IIRPA\\ChromiunBrowserConfig.json");
                if (File.Exists(configFilePath_old))
                {
                    File.Copy(configFilePath_old, configFilePath);
                }
            }
            if (File.Exists(configFilePath))
            {
                return JObject.Parse(File.ReadAllText(configFilePath, Encoding.UTF8));
            }
            else
            {
                return new JObject
                {
                    { "BrowserConfigMap", new JObject() }
                };
            }
        }
        internal static Dictionary<string, JObject> GetDefConfigMap()
        {
            Dictionary<string, JObject> map = null;
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                using (Stream sm = asm.GetManifestResourceStream($"WebExtensionInstall.ChromiumBrowserConfig.json"))//文件的路径,程序集.路径.文件名 
                using (StreamReader sr = new StreamReader(sm, Encoding.UTF8))
                {
                    var configObj = JObject.Parse(sr.ReadToEnd());
                    map = configObj["BrowserConfigMap"].ToObject<Dictionary<string, JObject>>();
                }
            }
            catch
            {
            }
            return map;
        }

        #region 保存文件信息
        public static void SaveConfigFile(string targetFilePath, string content)
        {
            string targetDirectory = Path.GetDirectoryName(targetFilePath);
            if (!Directory.Exists(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }
            string tempFileName = Path.GetRandomFileName(); // 生成一个随机文件名
            string tempFilePath = Path.Combine(targetDirectory, tempFileName);
            string bakFilePath = $"{targetFilePath}.bak";
            try
            {
                File.WriteAllText(tempFilePath, content, Encoding.UTF8);
                if (File.Exists(targetFilePath))
                {
                    File.Replace(tempFilePath, targetFilePath, bakFilePath, ignoreMetadataErrors: false);
                }
                else
                {
                    File.Move(tempFilePath, targetFilePath);
                }

                if (File.Exists(targetFilePath) && File.Exists(bakFilePath))
                {
                    File.Delete(bakFilePath);
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
                RollbackFiles(tempFilePath, targetFilePath, bakFilePath);
                throw new ApplicationException("保存文件失败", ex);
            }
        }
        private static void RollbackFiles(string tempFilePath, string targetFilePath, string bakFilePath)
        {
            try
            {
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
                if (File.Exists(targetFilePath))
                {
                    File.Delete(targetFilePath);
                }
                if (File.Exists(bakFilePath))
                {
                    File.Move(bakFilePath, targetFilePath);
                }
            }
            catch (Exception ex2)
            {
                LogError(ex2);
            }
        }
        #endregion
        public static void SaveChromiumBrowserConfig(object obj)
        {
            var localApplication = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var targetFilePath = Path.Combine(localApplication, "IIRPA\\ChromiumBrowserConfig.json");
            string targetDirectory = Path.GetDirectoryName(targetFilePath);

            string content = JsonConvert.SerializeObject(obj);
            SaveConfigFile(targetFilePath, content);

        }
        internal static Dictionary<string, string> ParseParameters(string[] args)
        {
            var parameters = new Dictionary<string, string>();
            foreach (var arg in args)
            {
                string[] parts = arg.Split('=');
                if (parts.Length == 2)
                {
                    //参数和值兼容两个客户端情况
                    parameters[parts[0].Replace("--", "")] = parts[1]?.Trim('"');
                }
            }
            return parameters;
        }
        public static string GetExeFilePath(Process process)
        {
            try
            {

                // 获取exe文件的完整路径
                var fileNameBuilder = new StringBuilder(1024 * 2);
                uint bufferLength = (uint)fileNameBuilder.Capacity + 1;
                string exeFilePath = QueryFullProcessImageName(process.Handle, 0, fileNameBuilder, ref bufferLength) ? fileNameBuilder.ToString() : string.Empty;
                return exeFilePath;
            }
            catch
            {
            }
            try
            {
                string query = $"SELECT ExecutablePath FROM Win32_Process WHERE ProcessId = {process.Id}";
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query))
                {
                    foreach (ManagementObject proc in searcher.Get())
                    {
                        return proc["ExecutablePath"]?.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
            }
            return string.Empty;
        }

        public static void SaveBrowserIcon(string browserExePath, string productName)
        {
            try
            {
                var localApplication = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string iconFileName = Path.Combine(localApplication, "IIRPA", "RPAChromiumExtension", $"{productName.Replace(" ", "_")}.ico");
                string targetDirectory = Path.GetDirectoryName(iconFileName);
                if (!Directory.Exists(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                }
                if (!File.Exists(browserExePath))
                {
                    return;
                }
                if (IsFileLocked(iconFileName))
                {
                    return;
                }
                var icon = Icon.ExtractAssociatedIcon(browserExePath);
                using (Bitmap bmp = icon.ToBitmap())
                {
                    bmp.Save(iconFileName, System.Drawing.Imaging.ImageFormat.Icon);
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }
        public static bool IsFileLocked(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return false;
                }
                using (FileStream stream = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    // 文件未被锁定
                    return false;
                }
            }
            catch (IOException)
            {
                // 文件被锁定
                return true;
            }
        }
        public static bool IsExistBrowserProcess(string processName, string exePath)
        {
            var processes = Process.GetProcessesByName(processName);
            if (processes != null && processes.Length > 0)
            {
                var sessionProcesses = processes.Where(p => p.SessionId == CurrentSessionId);
                if (sessionProcesses != null && sessionProcesses.Any())
                {
                    foreach (var item in sessionProcesses)
                    {
                        if (!string.IsNullOrEmpty(exePath) && exePath.Equals(Common.GetExeFilePath(item), StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        private static void CopyExtensionToUserData(string sourcePath)
        {
            string extensionPath = Path.Combine(Utils.GetProfileDirectory(Utils.GetChromeUserDataPath()), "Extensions", ChromeExtensionId, $"{ChromeExtensionVersion}_0");
            if (Directory.Exists(extensionPath))
            {
                Directory.Delete(extensionPath, recursive: true);
            }
            Directory.CreateDirectory(extensionPath);
            CopyDirectory(new DirectoryInfo(sourcePath), new DirectoryInfo(extensionPath));
        }
        private static void CopyDirectory(DirectoryInfo sourceDir, DirectoryInfo destDir)
        {
            foreach (DirectoryInfo dir in sourceDir.GetDirectories())
            {
                CopyDirectory(dir, destDir.CreateSubdirectory(dir.Name));
            }
            foreach (FileInfo file in sourceDir.GetFiles())
            {
                file.CopyTo(Path.Combine(destDir.FullName, file.Name));
            }
        }
        public static bool UpdateChromePreference()
        {
            try
            {
                var sourcePath = ExtractExtension();
                CopyExtensionToUserData(sourcePath);
                JObject preferencesObject = BuildNewPreferences();
                string securePreferencesPath = Utils.GetSecurePreferencesPath();
                File.WriteAllText(securePreferencesPath, JsonConvert.SerializeObject(preferencesObject));
                Console.WriteLine("[InstallIndeedV3Extension] Write new item dict to: " + securePreferencesPath);
                UpdateRegistry(preferencesObject);
                CleanSyncData();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("自动启用插件失败");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e);
                Console.ForegroundColor = ConsoleColor.White;
                return false;
            }
        }
        private static string ExtractExtension()
        {
            string directoryName = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            string sourceArchivePath = Path.Combine(directoryName, "RPAChromeExtension", "IIRPAChromeExtension.zip");
            string extractionPath = Path.Combine(directoryName, ChromeExtensionId);
            if (Directory.Exists(extractionPath))
            {
                Directory.Delete(extractionPath, recursive: true);
            }
            ZipFile.ExtractToDirectory(sourceArchivePath, extractionPath);
            return extractionPath;
        }
        private static JObject BuildNewPreferences()
        {
            byte[] seed = Utils.GetChromeProfileSeed();
            string userSid = Utils.GetUserSid();
            string preferencesPath = Utils.GetSecurePreferencesPath();
            if (!File.Exists(preferencesPath))
            {
                throw new Exception("Secure Preferences file not found");
            }
            JObject preferencesObject = JObject.Parse(File.ReadAllText(preferencesPath));
            preferencesObject["extensions"]["settings"][ChromeExtensionId] = CreateExtensionSettings();
            string settingKey = $"extensions.settings.{ChromeExtensionId}";
            JToken settingToken = preferencesObject["extensions"]["settings"][ChromeExtensionId];
            string mac = Utils.GenerateHash(seed, settingToken, settingKey, userSid);
            preferencesObject["protection"]["macs"]["extensions"]["settings"][ChromeExtensionId] = mac;
            Console.WriteLine("[BuildNewPreferences] Extension MAC: " + mac);
            JObject sortedMacs = Utils.SortJObject(preferencesObject["protection"]["macs"]["extensions"]["settings"] as JObject);
            preferencesObject["protection"]["macs"]["extensions"]["settings"] = sortedMacs;
            JToken macsToken = preferencesObject["protection"]["macs"];
            string superMac = Utils.GenerateHash(seed, macsToken, "", userSid);
            preferencesObject["protection"]["super_mac"] = superMac;
            Console.WriteLine("[BuildNewPreferences] Super MAC: " + superMac);
            return preferencesObject;
        }
        private static void UpdateRegistry(JObject preferencesObject)
        {
            byte[] seed = Encoding.ASCII.GetBytes("ChromeRegistryHashStoreValidationSeed");
            string registryPath = "HKEY_CURRENT_USER\\Software\\Google\\Chrome\\PreferenceMACs\\Default\\extensions.settings";
            JToken settingToken = preferencesObject["extensions"]["settings"][ChromeExtensionId];
            string registryMac = Utils.GenerateHash(seed, settingToken, registryPath, Utils.GetUserSid());
            Registry.SetValue(registryPath, ChromeExtensionId, registryMac);
            Console.WriteLine("[UpdateRegistry] Registry MAC: " + registryMac);
        }
        private static void CleanSyncData()
        {
            string syncDataPath = Path.Combine(Utils.GetProfileDirectory(Utils.GetChromeUserDataPath()), "Sync Data", "LevelDB");
            if (Directory.Exists(syncDataPath))
            {
                Directory.Delete(syncDataPath, recursive: true);
                Console.WriteLine("[CleanSyncData] Deleted sync data at: " + syncDataPath);
            }
        }
        private static JObject CreateExtensionSettings()
        {
            #region Old Preference
            //var settingsText = "{\"active_bit\": false, " +
            //       "\"active_permissions\": " +
            //       "{\"api\": " +
            //       "[\"clipboardRead\", " +
            //       "\"clipboardWrite\", " +
            //       "\"cookies\", " +
            //       "\"debugger\", " +
            //       "\"downloads\", " +
            //       "\"management\", " +
            //       "\"nativeMessaging\", " +
            //       "\"storage\", " +
            //       "\"tabs\", " +
            //       "\"webNavigation\", " +
            //       "\"scripting\"," +
            //        "\"offscreen\"," +
            //       "\"activeTab\"], " +
            //       "\"explicit_host\": " +
            //       "[\"<all_urls>\"], " +
            //       "\"manifest_permissions\": [], " +
            //       "\"scriptable_host\": " +
            //       "[\"file:///*\", \"ftp://*/*\", \"http://*/*\", \"https://*/*\"]}, " +
            //       "\"allowlist\": 1, " +
            //       "\"creation_flags\": 1," +
            //       "\"filtered_service_worker_events\": " +
            //       "{\"windows.onCreated\": [{}], " +
            //       "\"windows.onRemoved\": [{}]}, " +
            //       "\"first_install_time\": \"13364877586750144\", " +
            //       "\"from_webstore\": false, " +
            //       "\"granted_permissions\": " +
            //       "{\"api\": " +
            //       "[\"clipboardRead\", " +
            //       "\"clipboardWrite\", " +
            //       "\"cookies\", " +
            //       "\"debugger\", " +
            //       "\"downloads\", " +
            //       "\"management\", " +
            //       "\"nativeMessaging\", " +
            //       "\"storage\", " +
            //       "\"tabs\", " +
            //       "\"webNavigation\", " +
            //        "\"offscreen\", " +
            //       "\"scripting\"], " +
            //       "\"explicit_host\": [\"<all_urls>\"], " +
            //       "\"manifest_permissions\": [], " +
            //       "\"scriptable_host\": [\"file:///*\", \"ftp://*/*\", \"http://*/*\", \"https://*/*\"]}, " +
            //       "\"last_update_time\": \"13364877586750144\", " +
            //       "\"location\": 1, " +
            //       "\"manifest\": {\"background\": {\"service_worker\": \"service_worker.js\"}, " +
            //       "\"content_scripts\": [{\"all_frames\": true, \"js\": [\"content.js\"], " +
            //       "\"match_about_blank\": true, " +
            //       "\"matches\": [\"http://*/*\", \"https://*/*\", \"ftp://*/*\", \"file://*/*\"], " +
            //       "\"run_at\": \"document_start\"}], " +
            //       "\"content_security_policy\": {\"extension_pages\": \"script-src 'self'; object-src 'self'\"}, " +
            //       "\"description\": \"\\u5b9e\\u5728Agent\\u63d2\\u4ef6\\uff0cv3\\u7248\\u672c\\u80fd\\u529b\\u518d\\u5347\\u7ea7\\uff0c\\u67e5\\u8be2\\u6570\\u636e\\u3001\\u5904\\u7406\\u6587\\u6863\\u3001\\u586b\\u62a5\\u8868\\u5355\\u3001\\u7f51\\u9875\\u4ea4\\u4e92\\u7b49\\u5404\\u7c7b\\u7f51\\u9875\\u64cd\\u4f5c\\u573a\\u666f\\u8f7b\\u677e\\u5e94\\u5bf9\\u3002\", " +
            //       "\"host_permissions\": [\"<all_urls>\"], " +
            //       "\"icons\": {\"128\": \"icons/indeed-128x128.png\", \"16\": \"icons/indeed-16x16.png\", \"32\": \"icons/indeed-32x32.png\", \"48\": \"icons/indeed-48x48.png\"}, " +
            //       "\"key\": \"MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAme2hdLlrTv62nf3CTBPTG8KFAM+yhDN2TJDGknF83rRAn9d6CJUVqxPf2uofVebo0/6/ch3KJS68PyFYs2ot6QnlN20ZQKd3VOwOEPWUyWh0ETwaVBmQUJQVqa4FFDqwHAof6IUcba0P49PKyPEmnHGHs1o5V90t5bB2nnL0FwP+lSQj73di39ynQ8cENP/Ui7gTKnhkAHXWZU2ljz/6ZYs4yGfhng21TLf/d//Xi3JYb8Irv/IkzkT9J15BRgySNfu6vG6bkDl5JR8tJ3dJcKVMY3jTmSWpOcz/fZNyRk6eB2JXe2m1b51Mua3853SISHpoozzSBAgm9mOO8D8AmwIDAQAB\", " +
            //       "\"manifest_version\": 3, " +
            //       "\"name\": \"\\u5b9e\\u5728Agent\\uff1aAI\\u667a\\u80fd\\u4f53\\u52a9\\u7406\\uff0c\\u5f00\\u542f\\u667a\\u80fd\\u529e\\u516c\\u65b0\\u8303\\u5f0f\", " +
            //       "\"permissions\": [\"cookies\", \"management\", \"tabs\", \"debugger\", \"nativeMessaging\", \"webNavigation\", \"downloads\", \"clipboardRead\", \"clipboardWrite\", \"scripting\", \"offscreen\",\"storage\", \"activeTab\"], " +
            //       "\"update_url\": \"https://clients2.google.com/service/update2/crx\", " +
            //       $"\"version\": \"{ChromeExtensionVersion}\"}}, " +
            //        $"\"version_name\": \"{ChromeExtensionVersionName}\"}}, " +
            //       "\"needs_sync\": true, " +
            //       $"\"path\": \"{ChromeExtensionId}\\\\{ChromeExtensionVersion}_0\", " +
            //       $"\"service_worker_registration_info\": {{\"version\": \"{ChromeExtensionVersion}\"}}, " +
            //       "\"serviceworkerevents\": [\"runtime.onInstalled\"], " +
            //       "\"state\": 1, " +
            //       "\"was_installed_by_default\": false, " +
            //       "\"was_installed_by_oem\": false, " +
            //       "\"withholding_permissions\": false}"; 
            #endregion

         
            string settingsText = Utils.DecryptString(ChromeExtensionPrefJson);

            return JObject.Parse(settingsText);
        }
    }
}
