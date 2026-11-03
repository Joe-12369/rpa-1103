using System;

namespace II.RPA.WinAutomation.CommonUtility
{
    [Obsolete("所有对这个类型的引用应该修改为对Microsoft.Extension.Logging.ILogger的引用")]
    public class Log
    {
        public static void Verbose(string message)
        {
            NLogger.Default.Trace(message);
            System.Diagnostics.Trace.WriteLine(message, "Verbose");
        }

        public static void Activity(string message)
        {
            NLogger.Default.Trace(message);
            System.Diagnostics.Trace.WriteLine(message, "Activity");
        }

        public static void Debug(string message)
        {
            NLogger.Default.Debug(message);
            System.Diagnostics.Trace.WriteLine(message, "Debug");
        }

        public static void Selector(string message)
        {
            NLogger.Default.Debug(message);
            System.Diagnostics.Trace.WriteLine(message, "Selector");
        }

        public static void Information(string message)
        {
            NLogger.Default.Info(message);
            System.Diagnostics.Trace.WriteLine(message, "Information");
        }

        public static void Output(string message)
        {
            NLogger.Default.Info(message);
            System.Diagnostics.Trace.WriteLine(message, "Output");
        }

        public static void Warning(string message)
        {
            NLogger.Default.Warn(message);
            System.Diagnostics.Trace.WriteLine(message, "Warning");
        }

        public static void Error(object obj, string message)
        {
            var _message = obj.ToString();

            if (!string.IsNullOrEmpty(message))
            {
                _message = message + "\n" + _message;
            }

            NLogger.Default.Error(message);
            System.Diagnostics.Trace.WriteLine(_message, "Error");
        }

        public static void Error(string message)
        {
            NLogger.Default.Error(message);
            System.Diagnostics.Trace.WriteLine(message, "Error");
        }
    }
}
