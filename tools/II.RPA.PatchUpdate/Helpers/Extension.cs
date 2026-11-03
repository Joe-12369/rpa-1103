using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace II.RPA.PatchUpdate.Helpers
{
    public static class Extension
    {
        /// <summary>
        /// .net 4.6及以上版本内置了该方法
        /// </summary>
        /// <param name="dateTimeOffset"></param>
        /// <returns></returns>
        public static long ToUnixTimeSeconds(this DateTimeOffset dateTimeOffset)
        {
            long num = dateTimeOffset.UtcDateTime.Ticks / 10000000;
            return num - 62135596800L;
        }
        public static long ConvertDateTimeToLong()
        {
            var time = DateTime.Now;
            System.DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1, 0, 0, 0, 0));
            long t = (time.Ticks - startTime.Ticks) / 10000; 
            return t;
        }

        /// <summary>
        /// 测试文件是否被锁定
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns></returns>
        public static bool IsLocked(this string filePath)
        {
            try
            {
                using (Stream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None)) { }
            }
            catch
            {
                return true;
            }
            return false;
            //// 另一种写法
            //try
            //{
            //    using (FileStream stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None))
            //    {
            //        stream.Close();
            //    }
            //}
            //catch (IOException)
            //{
            //    //the file is unavailable because it is:
            //    //still being written to
            //    //or being processed by another thread
            //    //or does not exist (has already been processed)
            //    return true;
            //}

            ////file is not locked
            //return false;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern SafeFileHandle CreateFile(string lpFileName,
            FileSystemRights dwDesiredAccess, FileShare dwShareMode, IntPtr
            securityAttrs, FileMode dwCreationDisposition, FileOptions
            dwFlagsAndAttributes, IntPtr hTemplateFile);

        const int ERROR_SHARING_VIOLATION = 32;
        /// <summary>
        /// 调用内核方法测试文件是否被锁定
        /// </summary>
        /// <param name="fileName">文件路径</param>
        /// <returns></returns>
        public static bool IsFileInUse(string fileName)
        {
            bool inUse = false;

            SafeFileHandle fileHandle =
            CreateFile(fileName, FileSystemRights.Modify,
                  FileShare.Write, IntPtr.Zero,
                  FileMode.OpenOrCreate, FileOptions.None, IntPtr.Zero);

            if (fileHandle.IsInvalid)
            {
                if (Marshal.GetLastWin32Error() ==
                    ERROR_SHARING_VIOLATION)
                {
                    inUse = true;
                }
            }
            fileHandle.Close();
            return inUse;
        }
    }
}
