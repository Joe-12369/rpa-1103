using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DriverInput.Utils
{
    public class LogHelper:SingleInstance<LogHelper>
    {
        string logfile = Path.Combine(Path.GetTempPath(), "DriverInput.txt");//C:\Users\edz\AppData\Local\Temp\DriverInput.tx
        public bool HasEmpty = false;
        public void EmptyLog()
        {
            try
            {
                if (File.Exists(logfile))
                {
                    File.Delete(logfile);
                }

                HasEmpty = true;
            }
            catch { }
        }

        public void LogError(Exception ex)
        {
            if (!HasEmpty)
            {
                EmptyLog();
            }
            StringBuilder stringBuilder = new StringBuilder();
            do
            {
                stringBuilder.AppendLine(ex.Message);
                stringBuilder.AppendLine(ex.StackTrace);
                ex = ex.InnerException;
            }
            while (ex != null);
            //File.WriteAllText(logfile, stringBuilder.ToString());

            List<string> textLines = new List<string>();
            textLines.Add(stringBuilder.ToString());
            File.AppendAllLines(logfile, textLines);
        }

        public void LogError(string strMsg)
        {
            if (!HasEmpty)
            {
                EmptyLog();
            }
            StringBuilder stringBuilder = new StringBuilder();
            //File.WriteAllText(logfile, strMsg);
            List<string> textLines = new List<string>();
            textLines.Add(strMsg);
            File.AppendAllLines(logfile, textLines);
        }

        public void PrintLogPath()
        {
            LogInfo(logfile);
        }

        public void LogInfo(string strMsg)
        {
            try
            {
                //string logfile = Path.Combine("D:", "DriverInput.txt");
                //StringBuilder stringBuilder = new StringBuilder();
                //stringBuilder.AppendLine(strMsg); 
                //File.WriteAllText(logfile, stringBuilder.ToString());
                string logfile = @"C:\DriverInput.txt";
                //string logfile = Path.Combine(Path.GetTempPath(), "DriverInput.txt");
                //byte[] mybyte = Encoding.UTF8.GetBytes(strContent);
                //string mystr1 = Encoding.UTF8.GetString(mybyte);
                List<string> textLines = new List<string>();
                textLines.Add(strMsg);
                File.AppendAllLines(logfile, textLines);
            }
            catch
            { }
        }
    }
}
