using II.RPA.Core.Log;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json.Linq;
using System;
using System.Configuration;
using System.Diagnostics;
using System.Threading;

namespace II.RPA.Core.Provider.PluginLoaderProvider.Command
{
    public abstract class ConcreteCommand
    {
        protected static readonly int DefaultTimeoutSeconds = 600;
        protected readonly ManualResetEvent m_ManualResetEvent = new ManualResetEvent(false);
        private object m_CommandResult;
        protected readonly PluginLoaderInstanceWrapper m_PluginLoaderInstanceWrapper;
        protected readonly Action<object> m_ActionAfterExecute = null;
        protected readonly ILogger m_Logger;
        static ConcreteCommand()
        {
            //if (int.TryParse(ConfigurationManager.AppSettings["ActionDefaultTimeoutSecond"], out int anActionDefaultTimeoutSecond))
            //{
            //    DefaultTimeoutSeconds = anActionDefaultTimeoutSecond;
            //}
            DefaultTimeoutSeconds = 60 * 60;
        }
        public ConcreteCommand(PluginLoaderInstanceWrapper aPluginLoaderInstanceWrapper, 
            ILogger aLogger,
            int aTimeoutSpan, 
            Action<object> anActionAfterExecute)
        {
            m_Logger = aLogger;
            m_PluginLoaderInstanceWrapper = aPluginLoaderInstanceWrapper;
            if (aTimeoutSpan <= 0)
            {
                CommandExcuteTimeOutSeconds = DefaultTimeoutSeconds;
            }
            else
            {
                CommandExcuteTimeOutSeconds = aTimeoutSpan;
            }
            m_Logger.LogDebug($"{GetType().Name}:set execute timeout as {CommandExcuteTimeOutSeconds}s");
            m_ActionAfterExecute = anActionAfterExecute;
        }
        public string PluginPath
        {
            get;
            set;
        }
        public string PackagePath
        {
            get;
            set;
        }
        /// <summary>
        /// command 执行结果
        /// </summary>
        public virtual object CommandResult
        {
            get
            {
                ///这个超时时间就是plugin action的超时时间.very important
                ///每个command可以有通过构造方法单独设置超时时间
                m_Logger.LogDebug($"{this.GetType().Name}: start waiting result");
                if (m_ManualResetEvent.WaitOne(TimeSpan.FromSeconds(CommandExcuteTimeOutSeconds), true))
                {
                    m_Logger.LogDebug($"{this.GetType().Name}: get result");
                    m_ManualResetEvent.Reset();
                    return m_CommandResult;
                }
                else
                {
                    return new
                    {
                        ///待错误码需求合入后再调整
                        code = $"400000401",
                        message = ErrorMessageConfig.GetMessage(eErrorMessageType.Error_PluginRun_TimeOut),
                        info = $"",
                        extend = ""
                    }.ToJson();
                }
            }
            protected set
            {
                m_CommandResult = value;
                m_Logger.LogDebug($"{this.GetType().Name}: set result");
                m_ManualResetEvent.Set();
                ///m_ManualResetEvent.Reset();
                ///此处不能直接reset()、如果先调的是set方法、再调get那么就永远wait了。
                ///此bug只有在ExecuteWithResult()执行特别快的时候才会出现 基本是0~1毫秒才会出现
            }
        }
        private int CommandExcuteTimeOutSeconds
        {
            get;
            set;
        }
        public abstract void Execute();
        public abstract void ExecuteWithResult();
        /// <summary>
        /// 重写此方法用于直接获取返回值、不需要从CommandResult获取
        /// </summary>
        /// <returns></returns>
        public abstract object ExecuteWithReturnResult();
        public void ExecuteWithActionResult()
        {
            ExecuteWithResult();
            m_ActionAfterExecute?.Invoke(CommandResult);
        }
        public void CancelWaitResult()
        {
            m_ManualResetEvent?.Set();
        }
    }
}
