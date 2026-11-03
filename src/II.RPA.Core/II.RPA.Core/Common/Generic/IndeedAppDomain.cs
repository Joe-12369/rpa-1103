using II.RPA.Core.Log;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Core.Common.Generic
{
    /// <summary>
    /// 应用程序域包装类-泛型
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    public class IndeedAppDomain<T> where T : MarshalByRefObject
    {
        private readonly ILogger m_Logger;
        public IndeedAppDomain(ILogger aLogger,params object[] anArgsForCreateInstance)
        {
            m_Logger = aLogger;
            var type = typeof(T);
            string runby = Global.ClientIDString;
            if (string.IsNullOrEmpty(runby)||Global.Client == ClientID.INVALID)
            {
                runby = "AppLogger";
            }
            DomainName = $"{runby}-{type.Name}-{DateTime.Now.Hour}-{DateTime.Now.Minute}-{DateTime.Now.Second}-{DateTime.Now.Millisecond}";
            m_Logger.LogInfo($"New AppDomain compatibility scope:{DomainName}");
            Domain = AppDomain.CurrentDomain;
            Domain.UnhandledException += Domain_UnhandledException;
            Type typeInDomain = type;
            //双重验证
            if (typeInDomain.BaseType != typeof(MarshalByRefObject))
            {
                throw new InvalidOperationException($"{typeInDomain.FullName} does not inherit from {nameof(MarshalByRefObject)}");
            }
            if (anArgsForCreateInstance != null && anArgsForCreateInstance.Any())
            {
                Instance = (T)Activator.CreateInstance(typeInDomain, anArgsForCreateInstance);
            }
            else
            {
                Instance = (T)Activator.CreateInstance(typeInDomain);
            }
        }

        private void Domain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            m_Logger.LogError($"DomainException from {DomainName}", (Exception)e.ExceptionObject);
        }

        public string DomainName
        {
            get;
            private set;
        }
        private AppDomain Domain
        {
            get;
            set;
        }
        public T Instance
        {
            get;
            private set;
        }
        
        public void Unload()
        {
            Domain.UnhandledException -= Domain_UnhandledException;
        }
        private void Domain_DomainUnload(object sender, EventArgs e)
        {
            //m_Logger.LogInfo($"{Domain.FriendlyName} is Unloading");
        }

    }
}
