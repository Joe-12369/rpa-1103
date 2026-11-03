using Microsoft.Win32;
using System;
using System.IO;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;

namespace II.RPA.WebBrowser.Common.Utils
{
    public class FileHelper
    {
        private static ILogger logger = Logger.OfContext();
        private static string PrintFileVersionInfo(string path)
        {
            System.IO.FileInfo fileInfo = null;
            try
            {
                fileInfo = new System.IO.FileInfo(path);
            }
            catch (Exception e)
            {
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
        public static string FormattedFilePath(string saveFile, string fileName, string defaultDirectory)
        {
            var path = string.Empty;
            if (string.IsNullOrEmpty(fileName))
            {
                string name = Path.GetFileName(defaultDirectory);
                path = Path.Combine(saveFile, name);
            }
            else
            {
                path = Path.Combine(saveFile, fileName);
                var extName = Path.GetExtension(fileName);
                if (string.IsNullOrEmpty(extName))
                {
                    var sourceExtName = Path.GetExtension(defaultDirectory);
                    path = $"{path}{sourceExtName}";
                }
            }
            return path;
        }
        public static long FileSize(string path)
        {
            try
            {
                var data = FileInfoData(path);
                logger.LogDebug($"file size:{data.Length}");
                return data.Length;
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
                return 0;
            }
        }
        private static FileInfo FileInfoData(string path)
        {
            if (IsHideFileExtensions())
            {
                var directory = Path.GetDirectoryName(path);
                var filename = Path.GetFileName(path);
                DirectoryInfo dirPrograms = new DirectoryInfo(directory);
                var listDirectory = dirPrograms.EnumerateFiles();
                foreach (var item in listDirectory)
                {
                    try
                    {
                        var dir = Path.GetDirectoryName(item.FullName);
                        var name = Path.GetFileName(item.FullName);
                        if (directory.Equals(dir, StringComparison.OrdinalIgnoreCase)
                            && name.Contains(filename))
                            return item;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex.Message);
                    }
                }
                return null;
            }
            else
            {
                return new FileInfo(path);
            }
        }
        public static bool Exists(string path)
        {
            return FileInfoData(path)?.Exists == true;
        }
        internal static bool IsHideFileExtensions()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"))
                {
                    if (key != null)
                    {
                        object hideFileExtValue = key.GetValue("HideFileExt");

                        if (hideFileExtValue != null)
                        {
                            int hideFileExt = Convert.ToInt32(hideFileExtValue);
                            return hideFileExt == 1;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError("IsHideFileExtensions: " + ex.Message);
            }
            return false; // 默认情况下返回false，表示后缀名未被隐藏
        }
    }
}
