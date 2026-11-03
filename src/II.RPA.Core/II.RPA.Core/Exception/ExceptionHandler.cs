using II.RPA.Core.Log;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Core
{
    public class ExceptionHandler
    {
        private readonly ILogger m_Logger;
        public ExceptionHandler(ILogger aLogger)
        {
            m_Logger = aLogger;
        }
        public static ExceptionHandler Default
        {
            get;
            set;
        }
        public void Handle(Exception anException)
        {
            try
            {
                //var name = Process.GetCurrentProcess().ProcessName;
                //var cpuCounter = new PerformanceCounter("Process", "% Processor Time", name);
                //var ramCounter = new PerformanceCounter("Process", "Working Set", name);
                //m_Logger.LogInfo($"{cpuCounter.NextValue()};{ramCounter.NextValue()}");
                if (anException is IIException exception)
                {
                    if (exception.Level != ExceptionLevel.WARN)
                    {
                        m_Logger?.LogError(exception);
                        ///暂不throw了
                    }
                    else
                    {
                        m_Logger?.LogWarning("", exception);
                    }
                }
                else
                {
                    m_Logger?.LogError(anException);
                    if (anException.InnerException != null)
                    {
                        m_Logger?.LogError(anException.InnerException);
                    }
                }
            }
            catch (Exception ex)
            {
                m_Logger?.LogError(ex);
            }
        }
        public void Handle(Exception anException,string aSourceType)
        {
            m_Logger.LogInfo($"Exception SourceType:{aSourceType}");
            Handle(anException);
        }
    }
}
