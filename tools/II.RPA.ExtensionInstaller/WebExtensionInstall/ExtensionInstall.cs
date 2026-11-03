using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace WebExtensionInstall
{
    public class ExtensionInstall
    {
        public static void InstallEdgeExtension()
        {
            Message msg = new Message();
            try
            {
                //检测Edge是否安装
                if (Common.FindEdgePath())
                {
                    if (Common.VerifyWebBrowserVersion("msedge", out var fileMissing))
                    {
                        if (fileMissing)
                        {
                            msg.status = (int)ReturnStatus.Fail;
                            msg.code = (int)WebExtensionStatue.BrowserNotFound;
                            Common.EdgeLoggin(msg);
                            return;
                        }
                        //检测Edge是否打开
                        if (!Common.IsExistBrowserProcess("msedge"))
                        {
                            if (!Common.TryGetEdgeExtensionSelection(out EdgeExtensionSelection selection))
                            {
                                msg.status = (int)ReturnStatus.Fail;
                                msg.code = (int)WebExtensionStatue.BrowserNotFound;
                                Common.EdgeLoggin(msg);
                                return;
                            }
                            string sourceCrxPath = Path.Combine(
                                AppDomain.CurrentDomain.BaseDirectory,
                                "RPAEdgeExtension",
                                selection.CrxFileName);
                            if (!File.Exists(sourceCrxPath))
                            {
                                msg.status = (int)ReturnStatus.Fail;
                                msg.code = (int)WebExtensionStatue.FileCopyFail;
                                Common.EdgeLoggin(msg);
                                return;
                            }
                            Common.DeleteEdgeExtension();
                            //写入Host文件
                            if (!Common.CopyHostFileToPath("Edge"))
                            {
                                msg.status = (int)ReturnStatus.Fail;
                                msg.code = (int)WebExtensionStatue.FileCopyFail;
                                Common.EdgeLoggin(msg);
                                return;
                            }
                            if (!Common.RemoveLegacyEdgeNativeHostFiles())
                            {
                                msg.status = (int)ReturnStatus.Fail;
                                msg.code = (int)WebExtensionStatue.FileCopyFail;
                                Common.EdgeLoggin(msg);
                                return;
                            }
                            if (!NativeHostManifestEditor.EnsureAllowedOrigins(
                                Path.Combine(
                                    Common.GetEdgeExtensionInstallDirectory(),
                                    EdgeExtensionInstallation.NativeHostManifestFileName),
                                new[]
                                {
                                    EdgeExtensionPolicy.V2ExtensionId,
                                    EdgeExtensionPolicy.V3ExtensionId
                                }))
                            {
                                msg.status = (int)ReturnStatus.Fail;
                                msg.code = (int)WebExtensionStatue.FileCopyFail;
                                Common.EdgeLoggin(msg);
                                return;
                            }
                            //写入注册表
                            if (!Common.EdgeWriteRegedit(selection))
                            {
                                msg.status = (int)ReturnStatus.Fail;
                                msg.code = (int)WebExtensionStatue.RegeditWriteFail;
                                Common.EdgeLoggin(msg);
                                return;
                            }
                            msg.status = (int)ReturnStatus.Success;
                            msg.code = (int)WebExtensionStatue.ChromeExtensionInstallSuccess;
                            Common.EdgeLoggin(msg);
                            OpenBrowser("edge");
                            //Task.Run(() => OpenBrowser("edge"));
                            return;
                        }
                        else
                        {
                            msg.status = (int)ReturnStatus.Fail;
                            msg.code = (int)WebExtensionStatue.ChromeNotClose;
                            Common.EdgeLoggin(msg);
                            return;
                        }
                    }
                    else
                    {
                        msg.status = (int)ReturnStatus.Fail;
                        msg.code = (int)WebExtensionStatue.WebbrowserLowversion;
                        Common.EdgeLoggin(msg);
                        return;
                    }
                }
                else
                {
                    msg.status = (int)ReturnStatus.Fail;
                    msg.code = (int)WebExtensionStatue.ChromeUnInstall;
                    Common.EdgeLoggin(msg);
                    return;
                }
            }
            catch (Exception e)
            {
                msg.status = (int)ReturnStatus.Fail;
                msg.code = (int)WebExtensionStatue.Unknown;
                Common.EdgeLoggin(msg);
                Common.LogError(e);
                return;
            }
        }

        private static void InstallSogouExtensionV11(string path)
        {

            Message msg = new Message();
            //extension
            var extensionPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SogouExplorer", "Extension", "ofdinijfhmgnfddmbmminecbaafgoocd");

            //安装
            var extension = Common.GetSogouExtensionPath();
            if (string.IsNullOrEmpty(extension))
            {
                msg.status = (int)ReturnStatus.Fail;
                msg.code = (int)WebExtensionStatue.Unknown;
                msg.message = "插件目录未找到";
                Common.SogouLoggin(msg);
                return;
            }
            Process.Start(path, extension);

            msg.status = (int)ReturnStatus.Success;
            msg.code = (int)WebExtensionStatue.ChromeExtensionInstallSuccess;
            Common.SogouLoggin(msg);
            return;
        }
        public static void InstallSogouExtension()
        {
            Message msg = new Message();
            try
            {
                if (Common.FindSogouPath(out string path))
                {
                    if (!Common.IsHighVersionSogouExplorer(path))
                    {
                        InstallSogouExtensionV11(path);
                    }
                    else
                    {
                        //Common.ClearPreference("Sogou\\SogouExplorer\\User Data", "nfiffhdmjkohbbolbamdboplicimjnpm");
                        //拷贝文件
                        if (!Common.CopyHostFileToPath("Sogou"))
                        {
                            msg.status = (int)ReturnStatus.Fail;
                            msg.code = (int)WebExtensionStatue.FileCopyFail;
                            Common.SogouLoggin(msg);
                            return;
                        }
                        // 写入注册表
                        if (!Common.SougouWriteRegedit())
                        {
                            msg.status = (int)ReturnStatus.Fail;
                            msg.code = (int)WebExtensionStatue.RegeditWriteFail;
                            Common.SogouLoggin(msg);
                            return;
                        }
                        //var crxpath = Common.FindSogoucrxPath();
                        //Process.Start(path, "--load-extension=" + crxpath);
                        //Process.Start(path, crxpath);
                        msg.status = (int)ReturnStatus.Success;
                        msg.code = (int)WebExtensionStatue.SougouExtension;
                        Common.SogouLoggin(msg);

                    }
                }
                else
                {
                    msg.status = (int)ReturnStatus.Fail;
                    msg.code = (int)WebExtensionStatue.ChromeUnInstall;
                    Common.SogouLoggin(msg);
                    return;
                }


            }
            catch (Exception e)
            {
                msg.status = (int)ReturnStatus.Fail;
                msg.code = (int)WebExtensionStatue.Unknown;
                Common.SogouLoggin(msg);
                Common.LogError(e);
                return;
            }
        }

        public static void IsInstallSogouExtension()
        {
            Message msg = new Message();
            //extension
            var extensionPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SogouExplorer", "Extension", "ofdinijfhmgnfddmbmminecbaafgoocd");
            if (Common.IsExistSogouRegeditKey())
            {
                msg.status = (int)ReturnStatus.Success;
                msg.code = (int)WebExtensionStatue.ChromeExtensionInstallSuccess;
            }
            else if (!Directory.Exists(extensionPath) || Directory.GetDirectories(extensionPath, "1.0.0").Length < 1)
            {
                msg.status = (int)ReturnStatus.Fail;
                msg.code = (int)WebExtensionStatue.ChromeExtensionUnInstall;
            }
            else
            {
                msg.status = (int)ReturnStatus.Success;
                msg.code = (int)WebExtensionStatue.ChromeExtensionInstallSuccess;
            }
            Common.SogouLoggin(msg);
        }

        public static void IsInstallEdgeExtension()
        {
            //检测是否安装Edge浏览器 检测注册表是否写入
            Message msg = new Message();
            if (!Common.FindEdgePath())
            {
                msg.status = (int)ReturnStatus.Fail;
                msg.code = (int)WebExtensionStatue.ChromeUnInstall;
                Common.EdgeLoggin(msg);
                return;
            }
            if (Common.IsExistRegeditKeyEdge())
            {
                msg.status = (int)ReturnStatus.Success;
                msg.code = (int)WebExtensionStatue.ChromeExtensionInstallSuccess;
                if (Common.TryGetEdgeExtensionSelection(out EdgeExtensionSelection selection))
                {
                    msg.info = new JObject
                    {
                        { "version", selection.Generation.ToString() }
                    }.ToString();
                }
            }
            else
            {
                msg.status = (int)ReturnStatus.Fail;
                msg.code = (int)WebExtensionStatue.ChromeExtensionUnInstall;
            }
            Common.EdgeLoggin(msg);
        }
        public static void IsInstallChromeExtension()
        {
            var isV3 = Common.ChromeV2OrV3();
            Common.InitParams(isV3);
            //检测是否安装Chrome浏览器 检测注册表是否写入
            Message msg = new Message();
            if (!Common.FindChromePath())
            {
                msg.status = (int)ReturnStatus.Fail;
                msg.code = (int)WebExtensionStatue.ChromeUnInstall;
                Common.ChromeLoggin(msg);
                return;
            }
            if (Common.IsExistRegeditKey())
            {
                msg.status = (int)ReturnStatus.Success;
                msg.code = (int)WebExtensionStatue.ChromeExtensionInstallSuccess;
                var installResult = Common.GetLatestChromeExVersionInfo();
                if (installResult != null)
                {
                    msg.info = installResult.ToString();
                }
            }
            else
            {
                msg.status = (int)ReturnStatus.Fail;
                msg.code = (int)WebExtensionStatue.ChromeExtensionUnInstall;
            }
            Common.ChromeLoggin(msg);
        }

        public static void InstallChromeExtension()
        {
            Message msg = new Message();
            try
            {
                //检测Chrome是否安装
                if (Common.FindChromePath())
                {

                    if (Common.VerifyWebBrowserVersion("chrome", out var fileMissing))
                    {
                        if (fileMissing)
                        {
                            msg.status = (int)ReturnStatus.Fail;
                            msg.code = (int)WebExtensionStatue.BrowserNotFound;
                            Common.ChromeLoggin(msg);
                            return;
                        }
                        //检测Chrome是否打开
                        if (!Common.IsExistBrowserProcess("chrome"))
                        {
                            // Debugger.Launch();
                            var isV3 = Common.ChromeV2OrV3();
                            if (isV3)
                            {
                                //标记 3.0 为已卸载状态
                                Common.DeleteUninstallRecordIfNeeded("Google\\Chrome\\User Data");

                            }
                            Common.InitParams(isV3);
                            Common.DeleteExtension(!isV3);
                            //写入Host文件
                            if (!Common.CopyHostFileToPath("Chrome"))
                            {
                                msg.status = (int)ReturnStatus.Fail;
                                msg.code = (int)WebExtensionStatue.FileCopyFail;
                                Common.ChromeLoggin(msg);
                                return;
                            }
                            //写入注册表
                            if (!Common.WriteRegedit())
                            {
                                msg.status = (int)ReturnStatus.Fail;
                                msg.code = (int)WebExtensionStatue.RegeditWriteFail;
                                Common.ChromeLoggin(msg);
                                return;
                            }
                            Common.EnsureV2ExtensionId();
                            Common.SaveLatestChromeExVersionInfo();
                            msg.status = (int)ReturnStatus.Success;
                            msg.code = (int)WebExtensionStatue.ChromeExtensionInstallSuccess;
                            Common.ChromeLoggin(msg);

                            //if (isV3)
                            //{
                            //    Common.UpdateChromePreference();
                            //    //删除Service Worker缓存
                            //    var swPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google\\Chrome\\User Data\\Default\\Service Worker");
                            //    if (Directory.Exists(swPath))
                            //    {
                            //        Directory.Delete(swPath, true);
                            //    }
                            //}

                            OpenBrowser("chrome");
                            return;
                        }
                        else
                        {
                            msg.status = (int)ReturnStatus.Fail;
                            msg.code = (int)WebExtensionStatue.ChromeNotClose;
                            Common.ChromeLoggin(msg);
                            return;
                        }
                    }
                    else
                    {
                        msg.status = (int)ReturnStatus.Fail;
                        msg.code = (int)WebExtensionStatue.WebbrowserLowversion;
                        Common.ChromeLoggin(msg);
                        return;
                    }
                }
                else
                {
                    msg.status = (int)ReturnStatus.Fail;
                    msg.code = (int)WebExtensionStatue.ChromeUnInstall;
                    Common.ChromeLoggin(msg);
                    return;
                }
            }
            catch (Exception e)
            {
                msg.status = (int)ReturnStatus.Fail;
                msg.code = (int)WebExtensionStatue.Unknown;
                Common.ChromeLoggin(msg);
                Common.LogError(e);
                return;
            }
        }

        public static void IsInstallFireFoxExtension()
        {//检测是否安装Chrome浏览器 检测注册表是否写入
            Message msg = new Message();
            if (Common.FindFireFoxPath() && Common.IsExistRegeditKeyFireFox())
            {
                msg.status = (int)ReturnStatus.Success;
                //msg.code = (int)WebExtensionStatue.FireFoxExtensionInstallSuccess;
                msg.code = (int)WebExtensionStatue.ChromeExtensionInstallSuccess;
            }
            else
            {
                msg.status = (int)ReturnStatus.Fail;
                //msg.code = (int)WebExtensionStatue.FireFoxExtensionUnInstall;
                msg.code = (int)WebExtensionStatue.ChromeExtensionUnInstall;
            }
            Common.FireFoxLoggin(msg);
        }
        public static void InstallFireFoxExtension()
        {
            Message msg = new Message();
            try
            {
                //检测FireFox是否安装
                if (Common.FindFireFoxPath())
                {
                    if (Common.VerifyWebBrowserVersion("firefox", out var fileMissing))
                    {
                        if (fileMissing)
                        {
                            msg.status = (int)ReturnStatus.Fail;
                            msg.code = (int)WebExtensionStatue.BrowserNotFound;
                            Common.FireFoxLoggin(msg);
                            return;
                        }
                        //检测FireFox是否打开
                        if (!Common.IsExistBrowserProcess("firefox"))
                        {
                            //写入Host文件
                            if (!Common.CopyHostFileToPath("FireFox"))
                            {
                                msg.status = (int)ReturnStatus.Fail;
                                msg.code = (int)WebExtensionStatue.FileCopyFail;
                                Common.FireFoxLoggin(msg);
                                return;
                            }
                            //写入注册表
                            if (!Common.FireFoxRegedit())
                            {
                                msg.status = (int)ReturnStatus.Fail;
                                msg.code = (int)WebExtensionStatue.RegeditWriteFail;
                                Common.FireFoxLoggin(msg);
                                return;
                            }
                            msg.status = (int)ReturnStatus.Success;
                            // msg.code = (int)WebExtensionStatue.FireFoxExtensionInstallSuccess;
                            msg.code = (int)WebExtensionStatue.ChromeExtensionInstallSuccess;
                            Common.FireFoxLoggin(msg);
                            return;
                        }
                        else
                        {
                            msg.status = (int)ReturnStatus.Fail;
                            //msg.code = (int)WebExtensionStatue.FireFoxNotClose;
                            msg.code = (int)WebExtensionStatue.ChromeNotClose;
                            Common.FireFoxLoggin(msg);
                            return;
                        }
                    }
                    else
                    {
                        msg.status = (int)ReturnStatus.Fail;
                        msg.code = (int)WebExtensionStatue.WebbrowserLowversion;
                        Common.FireFoxLoggin(msg);
                        return;
                    }
                }
                else
                {
                    msg.status = (int)ReturnStatus.Fail;
                    //msg.code = (int)WebExtensionStatue.FireFoxUnInstall;
                    msg.code = (int)WebExtensionStatue.ChromeUnInstall;
                    Common.FireFoxLoggin(msg);
                    return;
                }
            }
            catch (Exception e)
            {
                msg.status = (int)ReturnStatus.Fail;
                msg.code = (int)WebExtensionStatue.Unknown;
                Common.FireFoxLoggin(msg);
                Common.LogError(e);
                return;
            }
        }

        public static void Install360XExtension()
        {
            Message msg = new Message();
            try
            {
                //检测360是否安装
                if (Common.Find360XPath(out string _))
                {
                    //if (Common.VerifyWebBrowserVersion("360se"))
                    //{
                    //检测360是否打开
                    if (!Common.IsExistBrowserProcess("360ChromeX") && !Common.IsExistBrowserProcess("360chrome"))
                    {
                        //Common.Delete360seExtension();
                        //写入Host文件
                        if (!Common.CopyHostFileToPath("360X"))
                        {
                            msg.status = (int)ReturnStatus.Fail;
                            msg.code = (int)WebExtensionStatue.FileCopyFail;
                            Common.Loggin360X(msg);
                            return;
                        }
                        //写入注册表
                        if (!Common.Write360XRegedit())
                        {
                            msg.status = (int)ReturnStatus.Fail;
                            msg.code = (int)WebExtensionStatue.RegeditWriteFail;
                            Common.Loggin360X(msg);
                            return;
                        }
                        msg.status = (int)ReturnStatus.Success;
                        msg.code = (int)WebExtensionStatue.ChromeExtensionInstallSuccess;
                        Common.Loggin360X(msg);
                        return;
                    }
                    else
                    {
                        msg.status = (int)ReturnStatus.Fail;
                        msg.code = (int)WebExtensionStatue.ChromeNotClose;
                        Common.Loggin360X(msg);
                        return;
                    }
                    //}
                    //else
                    //{
                    //    msg.status = (int)ReturnStatus.Fail;
                    //    msg.code = (int)WebExtensionStatue.WebbrowserLowversion;
                    //    Common.Loggin360se(msg);
                    //    return;
                    //}
                }
                else
                {
                    msg.status = (int)ReturnStatus.Fail;
                    msg.code = (int)WebExtensionStatue.ChromeUnInstall;
                    Common.Loggin360X(msg);
                    return;
                }
            }
            catch (Exception e)
            {
                msg.status = (int)ReturnStatus.Fail;
                msg.code = (int)WebExtensionStatue.Unknown;
                Common.Loggin360X(msg);
                Common.LogError(e);
                return;
            }
        }
        public static void Install360seExtension()
        {
            Message msg = new Message();
            try
            {
                //检测360是否安装
                if (Common.Find360sePath(out string _))
                {
                    //if (Common.VerifyWebBrowserVersion("360se"))
                    //{
                    //检测360是否打开
                    if (!Common.IsExistBrowserProcess("360se"))
                    {
                        //Common.Delete360seExtension();
                        //写入Host文件
                        if (!Common.CopyHostFileToPath("360se"))
                        {
                            msg.status = (int)ReturnStatus.Fail;
                            msg.code = (int)WebExtensionStatue.FileCopyFail;
                            Common.Loggin360se(msg);
                            return;
                        }
                        //写入注册表
                        if (!Common.Write360seRegedit())
                        {
                            msg.status = (int)ReturnStatus.Fail;
                            msg.code = (int)WebExtensionStatue.RegeditWriteFail;
                            Common.Loggin360se(msg);
                            return;
                        }
                        msg.status = (int)ReturnStatus.Success;
                        msg.code = (int)WebExtensionStatue.ChromeExtensionInstallSuccess;
                        Common.Loggin360se(msg);
                        return;
                    }
                    else
                    {
                        msg.status = (int)ReturnStatus.Fail;
                        msg.code = (int)WebExtensionStatue.ChromeNotClose;
                        Common.Loggin360se(msg);
                        return;
                    }
                    //}
                    //else
                    //{
                    //    msg.status = (int)ReturnStatus.Fail;
                    //    msg.code = (int)WebExtensionStatue.WebbrowserLowversion;
                    //    Common.Loggin360se(msg);
                    //    return;
                    //}
                }
                else
                {
                    msg.status = (int)ReturnStatus.Fail;
                    msg.code = (int)WebExtensionStatue.ChromeUnInstall;
                    Common.Loggin360se(msg);
                    return;
                }
            }
            catch (Exception e)
            {
                msg.status = (int)ReturnStatus.Fail;
                msg.code = (int)WebExtensionStatue.Unknown;
                Common.Loggin360se(msg);
                Common.LogError(e);
                return;
            }
        }

        public static void IsInstall360XExtension()
        {
            Common.InitParams();
            //检测是否安装360浏览器 检测注册表是否写入
            Message msg = new Message();
            if (Common.Find360XPath(out string _) && Common.IsExist360XRegeditKey())
            {
                msg.status = (int)ReturnStatus.Success;
                msg.code = (int)WebExtensionStatue.ChromeExtensionInstallSuccess;
            }
            else
            {
                msg.status = (int)ReturnStatus.Fail;
                msg.code = (int)WebExtensionStatue.ChromeExtensionUnInstall;
            }
            Common.Loggin360X(msg);
        }
        public static void IsInstall360seExtension()
        {
            Common.InitParams();
            //检测是否安装360浏览器 检测注册表是否写入
            Message msg = new Message();
            if (Common.Find360sePath(out string _) && Common.IsExist360RegeditKey())
            {
                msg.status = (int)ReturnStatus.Success;
                msg.code = (int)WebExtensionStatue.ChromeExtensionInstallSuccess;
            }
            else
            {
                msg.status = (int)ReturnStatus.Fail;
                msg.code = (int)WebExtensionStatue.ChromeExtensionUnInstall;
            }
            Common.Loggin360se(msg);
        }

        #region 关闭浏览器
        public static void CloseChromeBrowser()
        {
            Message msg = new Message();
            try
            {
                Common.CloseProcessWithName("chrome");
                msg.status = (int)ReturnStatus.Success;
                msg.code = (int)WebExtensionStatue.CloseBrowserSuccess;
                Common.ChromeLoggin(msg);
                return;
            }
            catch (Exception)
            {
                msg.status = (int)ReturnStatus.Fail;
                msg.code = (int)WebExtensionStatue.CloseBrowserFail;
                Common.ChromeLoggin(msg);
                return;
            }
        }
        public static void CloseFirefoxBrowser()
        {
            Message msg = new Message();
            try
            {
                Common.CloseProcessWithName("firefox");
                msg.status = (int)ReturnStatus.Success;
                msg.code = (int)WebExtensionStatue.CloseBrowserSuccess;
                Common.FireFoxLoggin(msg);
                return;
            }
            catch (Exception)
            {
                msg.status = (int)ReturnStatus.Fail;
                msg.code = (int)WebExtensionStatue.CloseBrowserFail;
                Common.FireFoxLoggin(msg);
                return;
            }
        }
        public static void CloseEdgeBrowser()
        {
            Message msg = new Message();
            try
            {
                Common.CloseProcessWithName("msedge");
                msg.status = (int)ReturnStatus.Success;
                msg.code = (int)WebExtensionStatue.CloseBrowserSuccess;
                Common.EdgeLoggin(msg);
                return;
            }
            catch (Exception)
            {
                msg.status = (int)ReturnStatus.Fail;
                msg.code = (int)WebExtensionStatue.CloseBrowserFail;
                Common.EdgeLoggin(msg);
                return;
            }
        }
        public static void Close360XBrowser()
        {
            Message msg = new Message();
            try
            {
                Common.CloseProcessWithName("360ChromeX");
                Common.CloseProcessWithName("360chrome");
                msg.status = (int)ReturnStatus.Success;
                msg.code = (int)WebExtensionStatue.CloseBrowserSuccess;
                Common.Loggin360se(msg);
                return;
            }
            catch (Exception)
            {
                msg.status = (int)ReturnStatus.Fail;
                msg.code = (int)WebExtensionStatue.CloseBrowserFail;
                Common.Loggin360se(msg);
                return;
            }
        }
        public static void Close360seBrowser()
        {
            Message msg = new Message();
            try
            {
                Common.CloseProcessWithName("360se");
                msg.status = (int)ReturnStatus.Success;
                msg.code = (int)WebExtensionStatue.CloseBrowserSuccess;
                Common.Loggin360se(msg);
                return;
            }
            catch (Exception)
            {
                msg.status = (int)ReturnStatus.Fail;
                msg.code = (int)WebExtensionStatue.CloseBrowserFail;
                Common.Loggin360se(msg);
                return;
            }
        }
        public static void CloseSogouBrowser()
        {
            Message msg = new Message();
            try
            {
                Common.CloseProcessWithName("SogouExplorer");
                msg.status = (int)ReturnStatus.Success;
                msg.code = (int)WebExtensionStatue.CloseBrowserSuccess;
                Common.SogouLoggin(msg);
                return;
            }
            catch (Exception)
            {
                msg.status = (int)ReturnStatus.Fail;
                msg.code = (int)WebExtensionStatue.CloseBrowserFail;
                Common.SogouLoggin(msg);
                return;
            }
        }
        #endregion
        #region 打开浏览器
        public static void OpenBrowser(string browserType, string args = null)
        {
            try
            {
                if (browserType == "chrome")
                {
                    var chromePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Google\Chrome\Application\chrome.exe");
                    if (!File.Exists(chromePath))
                    {
                        //计算机\HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\chrome.exe
                        RegistryKey key = Registry.LocalMachine;
                        RegistryKey software = key.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\App Paths\\chrome.exe", false);
                        chromePath = software.GetValue("").ToString();
                    }
                    Process.Start(chromePath);
                }
                if (browserType == "edge")
                {
                    var edgePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Google\Edge\Application\Edge.exe");
                    if (!File.Exists(edgePath))
                    {
                        //计算机\HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\Edge.exe
                        RegistryKey key = Registry.LocalMachine;
                        RegistryKey software = key.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\App Paths\\msedge.exe", false);
                        edgePath = software.GetValue("").ToString();
                    }
                    Process.Start(edgePath);
                }
                if (browserType.ToLower() == "sogou")
                {
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex);
            }
        }

        internal static void IsInstallSuperBrowserExtension()
        {
            Common.InitParams();
            Message msg = new Message();
            if (Common.FindSuperBrowser(out _) && Common.IsExistRegeditKeySuperBrowser())
            {
                msg.status = (int)ReturnStatus.Success;
                msg.code = (int)WebExtensionStatue.ChromeExtensionInstallSuccess;
            }
            else
            {
                msg.status = (int)ReturnStatus.Fail;
                msg.code = (int)WebExtensionStatue.ChromeExtensionUnInstall;
            }
            Common.SuperBrowserLoggin(msg);
        }

        internal static void InstallSuperBrowserExtension()
        {
            Message msg = new Message();
            try
            {
                //检测Chrome是否安装
                if (Common.FindSuperBrowser(out string path))
                {
                    //检测Chrome是否打开
                    if (!Common.IsExistBrowserProcess("SuperBrowser"))
                    {
                        Common.InitParams();
                        Common.DeleteExtensionSuperBrowser();
                        //写入Host文件
                        if (!Common.CopyHostFileToPathSuperBrowser("Chrome"))
                        {
                            msg.status = (int)ReturnStatus.Fail;
                            msg.code = (int)WebExtensionStatue.FileCopyFail;
                            Common.SuperBrowserLoggin(msg);
                            return;
                        }
                        //写入注册表
                        if (!Common.WriteRegeditSuperBrowser())
                        {
                            msg.status = (int)ReturnStatus.Fail;
                            msg.code = (int)WebExtensionStatue.RegeditWriteFail;
                            Common.SuperBrowserLoggin(msg);
                            return;
                        }
                        msg.status = (int)ReturnStatus.Success;
                        msg.code = (int)WebExtensionStatue.ChromeExtensionInstallSuccess;
                        Common.SuperBrowserLoggin(msg);
                        Process.Start(path);
                        //Task.Run(() => OpenBrowser("chrome"));
                        return;
                    }
                    else
                    {
                        msg.status = (int)ReturnStatus.Fail;
                        msg.code = (int)WebExtensionStatue.ChromeNotClose;
                        Common.SuperBrowserLoggin(msg);
                        return;
                    }
                }
                else
                {
                    msg.status = (int)ReturnStatus.Fail;
                    msg.code = (int)WebExtensionStatue.ChromeUnInstall;
                    Common.SuperBrowserLoggin(msg);
                    return;
                }
            }
            catch (Exception e)
            {
                msg.status = (int)ReturnStatus.Fail;
                msg.code = (int)WebExtensionStatue.Unknown;
                Common.SuperBrowserLoggin(msg);
                Common.LogError(e);
                return;
            }
        }

        internal static void CloseSuperBrowser()
        {
            Message msg = new Message();
            try
            {
                Common.CloseProcessWithName("SuperBrowser");
                Common.CloseProcessWithName("SuperBrowser");

                msg.status = (int)ReturnStatus.Success;
                msg.code = (int)WebExtensionStatue.CloseBrowserSuccess;
                Common.SuperBrowserLoggin(msg);
                return;
            }
            catch (Exception)
            {
                msg.status = (int)ReturnStatus.Fail;
                msg.code = (int)WebExtensionStatue.CloseBrowserFail;
                Common.SuperBrowserLoggin(msg);
                return;
            }
        }
        #endregion

        #region Chromium内核浏览器相关
        internal static void DetectChromiumBrowser(string[] args)
        {

            Message msg = new Message();
            try
            {
                var defConfigMap = Common.GetDefConfigMap();
                JObject userConfigObj = Common.GetUserConfigObj();
                HashSet<string> allowExeNames = defConfigMap.Values
                                                            .SelectMany(prop => prop["processFeatures"]?.Values<string>() ?? Enumerable.Empty<string>())
                                                            .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var lnkPaths = Common.GetLnkPaths();
                var lnkTargetPaths = Common.GetLnkTargetPaths(lnkPaths);
                var targetPaths = lnkTargetPaths.Where(path => allowExeNames.Contains(Path.GetFileNameWithoutExtension(path)))

                                                .Distinct()
                                                .ToList();


                targetPaths.AddRange(Common.GetRunningBrowserPaths(allowExeNames));
                targetPaths = targetPaths.Distinct().ToList();
                JArray browsersInfo = new JArray();
                foreach (var targetPath in targetPaths)
                {
                    try
                    {
                        FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(targetPath);
                        var productName = versionInfo.ProductName;
                        if (!defConfigMap.ContainsKey(productName))
                            continue;
                        if (userConfigObj["BrowserConfigMap"][productName] != null
                         && (userConfigObj["BrowserConfigMap"][productName]["appExePath"]?.ToString()) == targetPath)
                        {
                            continue;
                        }
                        Common.SaveBrowserIcon(targetPath, productName);
                        browsersInfo.Add(new JObject()
                            {
                                { "productName", productName},
                                { "lableName", defConfigMap[productName]["lableName"]},
                                { "appExePath",targetPath }
                            });
                    }
                    catch
                    {
                    }
                }
                msg.status = (int)ReturnStatus.Success;
                msg.code = (int)WebExtensionStatue.DetectChromiumCompleted;
                msg.info = browsersInfo.ToString();
                Common.ChromiumLoggin(msg);
                return;
            }
            catch (Exception)
            {
                msg.status = (int)ReturnStatus.Fail;
                msg.code = (int)WebExtensionStatue.DetectChromiumCompleted;
                Common.ChromiumLoggin(msg);
                return;
            }
        }
        internal static void IsChromiumBrowser(string[] args)
        {
            Message msg = new Message();
            msg.status = (int)ReturnStatus.Fail;
            msg.code = (int)WebExtensionStatue.NotChromiumBrowser;
            try
            {
                var defConfigMap = Common.GetDefConfigMap();
                JObject userConfigObj = Common.GetUserConfigObj();
                try
                {
                    var parameters = Common.ParseParameters(args);
                    if (!parameters.TryGetValue("exepath", out var exepath)
                    || string.IsNullOrWhiteSpace(exepath)
                    || !File.Exists(exepath)
                    || !string.Equals(Path.GetExtension(exepath), ".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        Common.ChromiumLoggin(msg);
                        return;
                    }
                    FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(exepath);
                    var productName = versionInfo.ProductName;
                    if (defConfigMap.ContainsKey(productName))//&& userConfigObj["BrowserConfigMap"][productName] == null
                    {
                        Common.SaveBrowserIcon(exepath, productName);
                        msg.status = (int)ReturnStatus.Success;
                        msg.code = (int)WebExtensionStatue.IsChromiumBrowser;
                        msg.info = new JObject() {
                                { "productName", productName},
                                { "lableName", defConfigMap[productName]["lableName"]},
                                { "appExePath",exepath }
                            }.ToString();
                    }
                    else
                    {
                        Common.SaveBrowserIcon(exepath, productName);
                        msg.status = (int)ReturnStatus.Success;
                        msg.code = (int)WebExtensionStatue.IsChromiumBrowser;
                        msg.info = new JObject() {
                                { "productName", productName},
                                { "lableName", productName},
                                { "appExePath",exepath }
                            }.ToString();
                    }
                }
                catch
                {
                }
                Common.ChromiumLoggin(msg);
                return;
            }
            catch (Exception)
            {
                msg.status = (int)ReturnStatus.Fail;
                msg.code = (int)WebExtensionStatue.NotChromiumBrowser;
                Common.ChromiumLoggin(msg);
                return;
            }
        }
        internal static void InstallChromiumExtension(string[] args)
        {
            //Debug.Assert(false);
            Message msg = new Message();
            try
            {
                var parameters = Common.ParseParameters(args);
                if (!parameters.TryGetValue("exepath", out var exepath))
                {
                    return;
                }


                JObject userConfigObj = Common.GetUserConfigObj();
                var defConfigMap = Common.GetDefConfigMap();

                FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(exepath);
                var productName = versionInfo.ProductName;
                JObject config;
                if (defConfigMap.ContainsKey(productName))
                {
                    config = defConfigMap[productName];
                }
                else
                {
                    //msg.status = (int)ReturnStatus.Fail;
                    //msg.code = (int)WebExtensionStatue.UnsupportedBrowser;
                    //Common.ChromiumLoggin(msg);
                    //return;

                    config = defConfigMap.Values.FirstOrDefault();
                    config["productName"] = productName;
                    config["lableName"] = productName;
                    config["processFeatures"] = JArray.FromObject(new string[] { Path.GetFileNameWithoutExtension(exepath) });
                }
                if (Common.IsExistBrowserProcess(config["processFeatures"].First.ToString(), exepath))
                {
                    msg.status = (int)ReturnStatus.Fail;
                    msg.code = (int)WebExtensionStatue.ChromeNotClose;
                    Common.ChromiumLoggin(msg);
                    return;
                }

                config["appExePath"] = exepath;
                userConfigObj["BrowserConfigMap"][productName] = config;
                Common.SaveChromiumBrowserConfig(userConfigObj);
                Common.SaveBrowserIcon(exepath, productName);

                Common.InitParams();
                //写入Host文件
                if (!Common.CopyHostFileToPath("Chrome"))
                {
                    msg.status = (int)ReturnStatus.Fail;
                    msg.code = (int)WebExtensionStatue.FileCopyFail;
                    Common.ChromiumLoggin(msg);
                    return;
                }
                //写入注册表
                if (!Common.WriteNativeMessageRegedit())
                {
                    msg.status = (int)ReturnStatus.Fail;
                    msg.code = (int)WebExtensionStatue.RegeditWriteFail;
                    Common.ChromiumLoggin(msg);
                    return;
                }

                msg.status = (int)ReturnStatus.Success;
                msg.code = (int)WebExtensionStatue.ChromiumExtensionInstallSuccess;
                Common.ChromiumLoggin(msg);
                return;
            }
            catch (Exception)
            {
                msg.status = (int)ReturnStatus.Fail;
                msg.code = (int)WebExtensionStatue.Unknown;
                Common.ChromiumLoggin(msg);
                return;
            }

        }
        internal static void CloseChromiumBrowser(string[] args)
        {
            Message msg = new Message();
            try
            {
                var parameters = Common.ParseParameters(args);
                if (!parameters.TryGetValue("exepath", out var exepath))
                {
                    return;
                }
                var fileName = Path.GetFileNameWithoutExtension(exepath);

                //Common.IsExistBrowserProcess(fileName);
                Common.CloseProcessWithName(fileName, exepath);
                msg.status = (int)ReturnStatus.Success;
                msg.code = (int)WebExtensionStatue.CloseBrowserSuccess;
                Common.ChromiumLoggin(msg);
                return;
            }
            catch (Exception)
            {
                msg.status = (int)ReturnStatus.Fail;
                msg.code = (int)WebExtensionStatue.CloseBrowserFail;
                Common.ChromiumLoggin(msg);
                return;
            }
        }
        #endregion
    }
}
