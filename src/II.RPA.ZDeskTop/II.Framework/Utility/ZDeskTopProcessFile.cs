using System;
using System.IO;
using System.Text;

namespace II.Framework.Utility
{
    /// <summary>
    /// 小窗进程 pid 文件（%AppData%\Z-Pid\Z-Desktop.txt）的读写封装。
    /// 写入/清理均为尽力而为，失败静默——该文件仅用于主控探测小窗存活状态。
    /// </summary>
    public static class ZDeskTopProcessFile
    {
        public static string DefaultPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Z-Pid", "Z-Desktop.txt");

        public static void Write(string pid) => Write(DefaultPath, pid);

        public static void Write(string fileName, string pid)
        {
            try
            {
                var directory = Path.GetDirectoryName(fileName);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(fileName, pid, Encoding.Default);
            }
            catch
            {
                // 尽力而为，失败静默
            }
        }

        public static void Clear(string fileName)
        {
            try
            {
                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }
            }
            catch
            {
                // 尽力而为，失败静默
            }
        }

        public static void Clear() => Clear(DefaultPath);
    }
}
