using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.EventMonitor
{
    internal class Loggers
    {
        //public static readonly log4net.ILog loginfo = log4net.LogManager.GetLogger("TelnetServer");//这里的 loginfo 和 log4net.config 里的名字要一样
        public static readonly log4net.ILog loginfo = null;

        static Loggers()
        {
            //string path = AppDomain.CurrentDomain.BaseDirectory + @"Config\log4net.config";
            //log4net.Config.XmlConfigurator.Configure(new FileInfo(path));
            loginfo = log4net.LogManager.GetLogger("TriggerServer");//这里的 loginfo 和 log4net.config 里的名字要一样
        }

        public static log4net.ILog GetLog(string logName)
        {
            log4net.ILog log = log4net.LogManager.GetLogger(logName);
            return log;
        }
        public static void WriteLog(string info)
        {
            try
            {

                if (loginfo.IsInfoEnabled)
                {
                    loginfo.Info(info);
                }

            }
            catch
            {

            }
        }
        internal static void Info(object message)
        {
            try
            {

                if (loginfo.IsInfoEnabled)
                {
                    loginfo.Info(message);
                }

            }
            catch
            {

            }
        }
        internal static void Debug(object message)
        {
            try
            {

                if (loginfo.IsDebugEnabled)
                {
                    loginfo.Debug(message);
                }
            }
            catch
            {

            }
        }
        internal static void Error(object message)
        {
            try
            {

                if (loginfo.IsErrorEnabled)
                {
                    loginfo.Error(message);
                }

            }
            catch
            {

            }
        }
        internal static void Fatal(object message)
        {
            try
            {

                if (loginfo.IsFatalEnabled)
                {
                    loginfo.Fatal(message);
                }
            }
            catch
            {

            }
        }
        internal static void Warn(object message)
        {
            try
            {
                if (loginfo.IsWarnEnabled)
                {
                    loginfo.Warn(message);
                }
            }
            catch
            {

            }
        }
    }
}
