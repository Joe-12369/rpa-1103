using II.RPA.WinAutomation.CommonUtility;
using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Checksum;
using ICSharpCode.SharpZipLib.Zip;

namespace II.RPA.MobileDeviceManager.MobileInstall
{
    public class ZipHandler
    {
        private class SingleHelper
        {
            private static ZipHandler emperor = new ZipHandler();

            public static ZipHandler GetEmperor()
            {
                return emperor;
            }
        }

        public delegate void ProcessChange(double percent);

        private Dictionary<string, ProcessItem> ProcessItems = new Dictionary<string, ProcessItem>();

        private bool _isKeepPath = true;

        public bool IsKeepPath
        {
            get
            {
                return _isKeepPath;
            }
            set
            {
                _isKeepPath = value;
            }
        }

        public static ZipHandler GetInstance()
        {
            return SingleHelper.GetEmperor();
        }

        public void AddFile(string filePath, string zipPath, string filePathInZip)
        {
            using (ZipFile zip = new ZipFile(zipPath))
            {
                zip.BeginUpdate();
                zip.Add(filePath, filePathInZip);
                zip.CommitUpdate();
            }
        }

        public void AddDirectory(string dicPath, string zipPath, string dicPathInZip, ProcessChange changedDG)
        {
            dicPathInZip = (dicPathInZip.EndsWith(Path.DirectorySeparatorChar.ToString()) ? dicPathInZip : (dicPathInZip + Path.DirectorySeparatorChar));
            string[] files = Directory.GetFiles(dicPath, "*", SearchOption.AllDirectories);
            double totalCount = files.Count();
            string key = Guid.NewGuid().ToString();
            ProcessItems.Add(key, new ProcessItem(totalCount));
            if (!Directory.Exists(dicPath))
            {
                throw new ArgumentException("文件夹路径不存在".I18n("controlType_ZipHandler_C2E2BF7F"));
            }
            string[] array = files;
            foreach (string file in array)
            {
                string filePathInZip = (IsKeepPath ? (dicPathInZip + file.Remove(0, dicPath.Count())) : (dicPathInZip + Path.GetFileName(file)));
                AddFile(file, zipPath, filePathInZip);
                changedDG(AddOneAndReport(key));
            }
            ProcessItems.Remove(key);
        }

        public void PackDirectory(string strDirectory, string zipedFile, ProcessChange changedDG)
        {
            if (!Directory.Exists(strDirectory))
            {
                throw new ArgumentException("需要压缩的文件夹不存在".I18n("controlType_ZipHandler_D8B897DD"));
            }
            using (FileStream ZipFile = File.Create(zipedFile))
            {
                using (ZipOutputStream s = new ZipOutputStream(ZipFile))
                {
                    double totalCount = Directory.GetFileSystemEntries(strDirectory, "*", SearchOption.AllDirectories).Count();
                    string key = Guid.NewGuid().ToString();
                    ProcessItems.Add(key, new ProcessItem(totalCount));
                    PackSetp(strDirectory, s, "", key, zipedFile, changedDG);
                    ProcessItems.Remove(key);
                }
            }
        }

        private void PackSetp(string strDirectory, ZipOutputStream s, string parentPath, string processItemKey, string zipPath, ProcessChange changedDG)
        {
            if (strDirectory[strDirectory.Length - 1] != Path.DirectorySeparatorChar)
            {
                strDirectory += Path.DirectorySeparatorChar;
            }
            Crc32 crc = new Crc32();
            string[] fileSystemEntries = Directory.GetFileSystemEntries(strDirectory);
            foreach (string file in fileSystemEntries)
            {
                if (Directory.Exists(file))
                {
                    string pPath = parentPath;
                    pPath += file.Substring(file.LastIndexOf(Path.DirectorySeparatorChar) + 1);
                    pPath += Path.DirectorySeparatorChar;
                    PackSetp(file, s, pPath, processItemKey, zipPath, changedDG);
                    changedDG?.Invoke(AddOneAndReport(processItemKey));
                    continue;
                }
                using (FileStream fs = File.OpenRead(file))
                {
                    byte[] buffer = new byte[fs.Length];
                    fs.ReadExactly(buffer);

                    string fileName = parentPath + file.Substring(file.LastIndexOf("\\") + 1);
                    if (IsKeepPath || !fileName.Contains("\\"))
                    {
                        goto next;
                    }
                    fileName = Path.GetFileName(fileName);
                    if (ProcessItems[processItemKey].HandledFiles.Count((string p) => p == fileName) <= 0)
                    {
                        goto next;
                    }
                    changedDG(AddOneAndReport(processItemKey));
                    goto end;
                next:
                    ZipEntry entry = new ZipEntry(fileName);
                    entry.DateTime = DateTime.Now;
                    entry.Size = fs.Length;
                    fs.Close();
                    crc.Reset();
                    crc.Update(buffer);
                    entry.Crc = crc.Value;
                    s.PutNextEntry(entry);
                    s.Write(buffer, 0, buffer.Length);
                    changedDG?.Invoke(AddOneAndReport(processItemKey));
                    ProcessItems[processItemKey].HandledFiles.Add(fileName);
                end:;
                }
            }
        }

        public async Task UnpackAllAsync(string zipFilePath, string unzipPath, ProcessChange changedDG)
        {
            double totalCount = GetZipFileCount(zipFilePath);
            string key = Guid.NewGuid().ToString();
            ProcessItems.Add(key, new ProcessItem(totalCount));
            await UnpackFiles(zipFilePath, unzipPath, changedDG, "*", key);
            ProcessItems.Remove(key);
        }

        public async void UnpackFile(string zipFilePath, string unzipPath, string filePathInZip)
        {
            await UnpackFiles(zipFilePath, unzipPath, null, filePathInZip);
        }

        public async void UnpackDirectory(string zipFilePath, string unzipPath, string DicPathInZip)
        {
            await UnpackFiles(zipFilePath, unzipPath, null, DicPathInZip);
        }

        private async Task UnpackFiles(string zipFilePath, string unzipPath, ProcessChange changedDG = null, string directName = "*", string processKey = "")
        {
            if (unzipPath[unzipPath.Length - 1] != Path.DirectorySeparatorChar)
            {
                unzipPath += Path.DirectorySeparatorChar;
            }
            if (!Directory.Exists(unzipPath))
            {
                Directory.CreateDirectory(unzipPath);
            }
            using (ZipInputStream zipStream = new ZipInputStream(File.OpenRead(zipFilePath)))
            {
                ZipEntry zipEntry;
                while ((zipEntry = zipStream.GetNextEntry()) != null)
                {
                    zipEntry.CompressedSize = 9L;
                    string directoryName = Path.GetDirectoryName(zipEntry.Name);
                    string fileName = Path.GetFileName(zipEntry.Name);
                    if (!string.IsNullOrEmpty(directoryName) && !Directory.Exists(unzipPath + directoryName))
                    {
                        if (directName != "*" && !Path.HasExtension(directName) && !directoryName.StartsWith(directName))
                        {
                            continue;
                        }
                        if (IsKeepPath)
                        {
                            Directory.CreateDirectory(unzipPath + directoryName);
                        }
                    }
                    if (string.IsNullOrEmpty(fileName))
                    {
                        continue;
                    }
                    if (zipEntry.CompressedSize == 0L)
                    {
                        break;
                    }
                    if (zipEntry.IsDirectory)
                    {
                        Path.GetDirectoryName(unzipPath + zipEntry.Name);
                    }
                    string zipFileName = ((!IsKeepPath) ? Path.GetFileName(zipEntry.Name) : zipEntry.Name);
                    if (!(directName != "*") || Path.HasExtension(directName) || zipEntry.Name.StartsWith(directName))
                    {
                        if (Path.HasExtension(directName) && directName == zipEntry.Name)
                        {
                            await HandleFile(unzipPath + zipFileName, zipStream, processKey, changedDG);
                            return;
                        }
                        if (!string.IsNullOrEmpty(directName) && !Path.HasExtension(directName))
                        {
                            await HandleFile(unzipPath + zipFileName, zipStream, processKey, changedDG);
                        }
                    }
                }
            }
        }

        private async Task HandleFile(string filePath, ZipInputStream zipStream, string processKey, ProcessChange changedDG)
        {
            using (FileStream stream = File.Create(filePath))
            {
                byte[] buffer = new byte[2048];
                while (true)
                {
                    int size = await zipStream.ReadAsync(buffer, 0, buffer.Length);
                    if (size <= 0)
                    {
                        break;
                    }
                    stream.Write(buffer, 0, size);
                }
            }
            changedDG?.Invoke(AddOneAndReport(processKey));
        }

        private double AddOneAndReport(string processItemKey)
        {
            return Math.Round((ProcessItems[processItemKey].HadHandleCount += 1.0) / ProcessItems[processItemKey].NeedHandleCount * 100.0, 2);
        }

        public int GetZipFileCount(string zipPath)
        {
            int result = 0;
            try
            {
                using (ZipInputStream inputStream = new ZipInputStream(File.OpenRead(zipPath)))
                {
                    ZipEntry currentEntry = null;
                    while ((currentEntry = inputStream.GetNextEntry()) != null)
                    {
                        foreach (Match item in Regex.Matches(currentEntry.Name, "[^/]+$"))
                        {
                            _ = item;
                            result++;
                        }
                    }
                }
            }
            catch
            {
            }
            return result;
        }

        public bool IsExistFileInZip(string zipPath, string filename)
        {
            try
            {
                using (ZipInputStream inputStream = new ZipInputStream(File.OpenRead(zipPath)))
                {
                    ZipEntry currentEntry = null;
                    while ((currentEntry = inputStream.GetNextEntry()) != null)
                    {
                        if (currentEntry.Name == filename)
                        {
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
            return false;
        }
    }
}
