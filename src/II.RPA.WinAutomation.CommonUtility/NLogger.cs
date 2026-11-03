using II.RPA.Logging;
using II.RPA.Logging.Definitions;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace II.RPA.WinAutomation.CommonUtility
{
    [Obsolete("该类型仅用作兼容，新代码应该使用Microsoft.Extensions.Logging.ILogger接口")]
    public class NLogger
    {
        private static ILogger logger = Logger.OfContext();

        static NLogger()
        {
            Default = new NLogger();
        }

        public bool IsTraceEnabled()
        {
            return true;
        }

        public bool IsDebugEnabled()
        {
            return true;
        }

        public bool IsInfoEnabled()
        {
            return true;
        }

        public bool IsErrorEnabled()
        {
            return true;
        }

        public static NLogger Default { get; private set; }

        #region public interfaces

        public void ServiceError(int functionId, int errorCode, string errorMessage)
        {
            logger.LogError("{type} 调用服务 {service} 发生错误。错误码：{code},错误信息{errorMessage}", "SERVICE", functionId, errorCode, errorMessage);
        }

        public void CalcTimeLogger(Action action, string stringFromat)
        {
            var watch = Stopwatch.StartNew();
            action?.Invoke();
            watch.Stop();
            using (logger.BeginScope(watch.Elapsed))
            {
                logger.LogDebug(stringFromat, watch.Elapsed);
            }
        }

        /// <summary>
        /// Debug模式下记录日志
        /// </summary>
        /// <param name="action"></param>
        /// <param name="stringFromat"></param>
        public void DebugModeCalcTimeLogger(Action action, string stringFromat)
        {
            CalcTimeLogger(action, stringFromat);
        }

        /// <summary>
        /// Debug模式下记录日志
        /// </summary>
        /// <param name="message"></param>
        public void DebugMode(object message)
        {
            Debug(message);
        }

        /// <summary>
        /// Debug模式下记录日志
        /// </summary>
        /// <param name="message"></param>
        /// <param name="exception"></param>
        public void DebugMode(string message, Exception exception)
        {
            Debug(message, exception);
        }

        /// <summary>
        /// Debug模式下记录日志
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void DebugModeFormat(string format, params object[] args)
        {
            DebugFormat(format, args);
        }

        /// <summary>
        /// Debug模式下记录性能日志
        /// </summary>
        /// <param name="message"></param>
        public void DebugModeTrace(object message)
        {
            Trace(message);
        }

        /// <summary>
        /// Debug模式下记录性能日志
        /// </summary>
        /// <param name="message"></param>
        /// <param name="exception"></param>
        public void DebugModeTrace(string message, Exception exception)
        {
            Trace(message, exception);
        }

        /// <summary>
        /// Debug模式下记录性能日志
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void DebugModeTraceFormat(string format, params object[] args)
        {
            TraceFormat(format, args);
        }

        public void Trace(object message)
        {
            if (message is string || message is StringBuilder)
            {
                logger.LogDebug($"{message}");
            }
            else if (message is Exception exMessage)
            {
                CollectMemoryInfoWhenNecessary(
                    () => logger.LogDebug(EventIds.DebugDefault, exMessage, "发生了错误"),
                    () => exMessage is OutOfMemoryException);
            }
            else
            {
                logger.LogDebug("打印对象信息：{obj}", message);
            }
        }

        public void Trace(string message, Exception exception)
        {
            logger.LogDebug(EventIds.DebugDefault, exception, message);
        }

        public void TraceFormat(string format, params object[] args)
        {
            logger.LogDebug(format, args);
        }

        internal void TraceFormat(IFormatProvider provider, string format, params object[] args)
        {
            logger.LogDebug(format, args);
        }

        public void Debug(object message)
        {
            if (message is string || message is StringBuilder)
            {
                logger.LogDebug($"{message}");
            }
            else if (message is Exception exMessage)
            {
                CollectMemoryInfoWhenNecessary(
                    () => logger.LogDebug(EventIds.DebugDefault, exMessage, "发生了错误"),
                    () => exMessage is OutOfMemoryException);
            }
            else
            {
                logger.LogDebug("打印对象信息：{obj}", message);
            }
        }

        public void Debug(string message, Exception exception)
        {
            logger.LogDebug(EventIds.DebugDefault, exception, message);
        }

        public void DebugFormat(string format, params object[] args)
        {
            logger.LogDebug(format, args);
        }

        internal void DebugFormat(IFormatProvider provider, string format, params object[] args)
        {
            logger.LogDebug(format, args);
        }

        public void Info(object message)
        {
            if (message is string || message is StringBuilder)
            {
                logger.LogInformation($"{message}");
            }
            else if (message is Exception exMessage)
            {
                CollectMemoryInfoWhenNecessary(
                    () => logger.LogInformation(EventIds.DebugDefault, exMessage, "发生了错误"),
                    () => exMessage is OutOfMemoryException);
            }
            else
            {
                logger.LogInformation("打印对象信息：{obj}", message);
            }
        }

        public void Info(string message, Exception exception)
        {
            logger.LogInformation(EventIds.InfoDefault, exception, message);
        }

        public void InfoFormat(string format, params object[] args)
        {
            logger.LogInformation(format, args);
        }

        internal void InfoFormat(IFormatProvider provider, string format, params object[] args)
        {
            logger.LogInformation(format, args);
        }

        public void Warn(object message)
        {
            if (message is string || message is StringBuilder)
            {
                logger.LogWarning($"{message}");
            }
            else if (message is Exception exMessage)
            {
                CollectMemoryInfoWhenNecessary(
                    () => logger.LogWarning(EventIds.DebugDefault, exMessage, "发生了错误"),
                    () => exMessage is OutOfMemoryException);
            }
            else
            {
                logger.LogWarning("打印对象信息：{obj}", message);
            }
        }

        public void Warn(string message, Exception exception)
        {
            logger.LogWarning(EventIds.WarningDefault, exception, message);
        }

        public void WarnFormat(string format, params object[] args)
        {
            logger.LogWarning(format, args);
        }

        internal void WarnFormat(IFormatProvider provider, string format, params object[] args)
        {
            logger.LogWarning(format, args);
        }

        private void CollectMemoryInfoWhenNecessary(Action logAction, Func<bool> condition)
        {
            if (condition())
            {
                var process = Process.GetCurrentProcess();
                using (var scope = logger.BeginScope(new
                {
                    tag = "OutOfMemory",
                    process.PrivateMemorySize64,
                    process.WorkingSet64,
                    process.PeakVirtualMemorySize64
                }))
                {
                    logAction();
                }
            }
            else
            {
                logAction();
            }
        }

        public void Error(object message)
        {
            if (message is string || message is StringBuilder)
            {
                logger.LogError($"{message}");
            }
            else if (message is Exception exMessage)
            {
                CollectMemoryInfoWhenNecessary(
                    () => logger.LogError(EventIds.DebugDefault, exMessage, "发生了错误"),
                    () => exMessage is OutOfMemoryException);
            }
            else
            {
                logger.LogError("打印对象信息：{obj}", message);
            }
        }

        public void ErrorStackTrace(object message)
        {
            //Error添加可选参数重载时，UcfContext Msghandle等地方或报错找不到方法。故新增方法。
            var stackInfo = GetCurrenStackTrace();

            if (message is string strMessage)
            {
                logger.LogError($"{strMessage} 堆栈为：{{stackInfo}}", stackInfo);
            }
            else if (message is Exception exMessage)
            {
                CollectMemoryInfoWhenNecessary(
                    () => logger.LogError(EventIds.DebugDefault, exMessage, "发生了错误 堆栈为：{{stackInfo}}", stackInfo),
                    () => exMessage is OutOfMemoryException);
            }
            else
            {
                logger.LogError("打印对象信息：{obj}。堆栈为：{stackInfo}", message, stackInfo);
            }
        }

        private static string GetCurrenStackTrace()
        {
            StringBuilder sb = new StringBuilder();
            //当前堆栈信息
            var st = new StackTrace();
            var sfs = st.GetFrames();

            if (sfs == null || sfs.Length < 3)
            {
                return null;
            }

            for (int i = 2; i < sfs.Length; ++i)
            {
                //非用户代码,系统方法之后均为系统调用
                if (StackFrame.OFFSET_UNKNOWN == sfs[i].GetILOffset())
                {
                    break;
                }

                var methodInfo = sfs[i].GetMethod();

                if (methodInfo == null)
                {
                    continue;
                }

                var methodName = methodInfo.Name;//方法名称
                var fileName = methodInfo.DeclaringType?.FullName;

                if (!string.IsNullOrEmpty(fileName))
                {
                    sb.Append("class:");
                    sb.Append(fileName);
                    sb.Append(",");
                }

                sb.Append("funtion:");
                sb.AppendLine(methodName);
                //sfs[i].GetFileLineNumber();//没有PDB文件的情况下将始终返回0
            }

            return sb.ToString();
        }
         
        public void Error(string message, Exception exception)
        {
            CollectMemoryInfoWhenNecessary(
                () => logger.LogError(EventIds.ErrorDefault, exception, message),
                () => exception is OutOfMemoryException);
        }

        public void ErrorFormat(string format, params object[] args)
        {
            logger.LogError(format, args);
        }

        internal void ErrorFormat(IFormatProvider provider, string format, params object[] args)
        {
            logger.LogError(format, args);
        }

        public void Fatal(object message)
        {
            if (message is string || message is StringBuilder)
            {
                logger.LogCritical($"{message}");
            }
            else if (message is Exception exMessage)
            {
                CollectMemoryInfoWhenNecessary(
                    () => logger.LogCritical(EventIds.DebugDefault, exMessage, "发生了错误"),
                    () => exMessage is OutOfMemoryException);
            }
            else
            {
                logger.LogCritical("打印对象信息：{obj}", message);
            }
        }

        public void Fatal(string message, Exception exception)
        {
            logger.LogCritical(EventIds.CriticalDefault, exception, message);
        }

        public void FatalFormat(string format, params object[] args)
        {
            logger.LogCritical(format, args);
        }

        internal void FatalFormat(IFormatProvider provider, string format, params object[] args)
        {
            logger.LogCritical(format, args);
        }

        #endregion
    }
}
