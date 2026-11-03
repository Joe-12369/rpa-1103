using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Core.Common
{
    /// <summary>
    /// 应用程序域包装类
    /// </summary>
    public class IndeedAppDomain
    {
        public IndeedAppDomain(string aDomainName)
        {
            Domain = AppDomain.CreateDomain(aDomainName);
            DomainName = aDomainName;
            AssemblyDictionary = new ConcurrentDictionary<string, Assembly>();
            SingletonDictionary = new ConcurrentDictionary<string, object>();
        }
        public IndeedAppDomain(string aDomainName, string aAssemblyFileName, string aTypeName)
        {
            Domain = AppDomain.CreateDomain(aDomainName);
            DomainName = aDomainName;
            var assembly = Assembly.Load(AssemblyName.GetAssemblyName(aAssemblyFileName));
            Type _type = assembly.GetType(aTypeName);
            if (_type.BaseType != typeof(MarshalByRefObject))
            {
                throw new InvalidOperationException($"{_type.FullName} do not inherit from {nameof(MarshalByRefObject)}");
            }
            var Instance = Activator.CreateInstance(_type);
        }
        public string DomainName
        {
            get;
            private set;
        }
        public AppDomain Domain
        {
            get;
            private set;
        }
        public Assembly DefaultAssembly
        {
            get
            {
                return Assemblies?.First();
            }
        }
        public object Singleton
        {
            get
            {
                return Assemblies?.First();
            }
        }
        public Assembly[] Assemblies
        {
            get
            {
                return AssemblyDictionary?.Values?.ToArray();
            }
        }
        private ConcurrentDictionary<string,Assembly> AssemblyDictionary
        {
            get;
            set;
        }
        private ConcurrentDictionary<string, object> SingletonDictionary
        {
            get;
            set;
        }
        public Assembly AddAssembly(string aAssemblyFile)
        {
            return AssemblyDictionary.AddOrUpdate(aAssemblyFile, 
                Assembly.Load(AssemblyName.GetAssemblyName(aAssemblyFile)), 
                (key,value) => 
                {
                    return value;
                });
        }
        public void Unload()
        {
            AppDomain.Unload(Domain);
        }
        public object DeflautExecuteMothod(string typeName, string methodName,object[] aParams)
        {
            Type defaultType = DefaultAssembly.GetType(typeName);
            if(defaultType.BaseType != typeof(MarshalByRefObject))
            {
                throw new InvalidOperationException($"{defaultType.FullName} do not inherit from {nameof(MarshalByRefObject)}");
            }
            var defaultObject = Activator.CreateInstance(defaultType);
            return defaultType.GetMethod(methodName).Invoke(defaultObject, aParams);
        }
        public object DeflautSingletonExecuteMothod(string typeName, string methodName, object[] aParams)
        {
            Type defaultType = DefaultAssembly.GetType(typeName);
            if (defaultType.BaseType != typeof(MarshalByRefObject))
            {
                throw new InvalidOperationException($"{defaultType.FullName} do not inherit from {nameof(MarshalByRefObject)}");
            }
            var defaultObject = SingletonDictionary.GetOrAdd(defaultType.FullName, Activator.CreateInstance(defaultType));
            return defaultType.GetMethod(methodName).Invoke(defaultObject, aParams);
        }
    }
}
