using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.PatchUpdate.Helpers
{
    public class LogHelper
    {
        private static readonly LogHelper logHelper = new LogHelper();
        private LogHelper()
        {

        }

        public static LogHelper GetLogHelper()
        {
            return logHelper;
        }

        private void WriteLogs(string dirName, string type, string content)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory;
            if (!string.IsNullOrEmpty(path))
            {
                path += dirName;
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                path += "\\" + DateTime.Now.ToString("yyyyMMdd") + ".log";
                if (!File.Exists(path))
                {
                    FileStream fs = File.Create(path);
                    fs.Close();
                }
                if (File.Exists(path))
                {
                    StreamWriter sw = new StreamWriter(path, true, Encoding.Default);
                    sw.WriteLineAsync(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss ") + type + " -->" + content);
                    sw.Close();
                }
            }
        }

        private void Log(string type, string content)
        {
            WriteLogs("logs", type, content);
        }

        public void Debug(string content)
        {
            Log("Debug", content);
        }

        public void Info(string content)
        {
            Log("Info", content);
        }

        public void Warn(string content)
        {
            Log("Warn", content);
        }

        public void Error(string content)
        {
            Log("Error", content);
        }

        public void Fatal(string content)
        {
            Log("Fatal", content);
        }
    }
}
