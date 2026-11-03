using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Core.Common
{
    public class IndeedAppDomainManager
    {
        private readonly ConcurrentDictionary<string, AppDomain> m_AppDomains;
        IndeedAppDomainManager()
        {
            m_AppDomains = new ConcurrentDictionary<string, AppDomain>();
        }
        void CreateDomain(string aDomainName)
        {
           m_AppDomains.GetOrAdd(aDomainName, AppDomain.CreateDomain(aDomainName));
        }
        void LoadAssembly(string anAssemblyFullName, string aDomainName)
        {
            if (m_AppDomains.TryGetValue(aDomainName, out AppDomain domain))
            {
                domain.Load(AssemblyName.GetAssemblyName(anAssemblyFullName));
            }
            else
            {
                
            }               
        }
        object ResolveInstance(string aDomainName,string aTypeName,string anAssemblyName)
        {
            if (m_AppDomains.TryGetValue(aDomainName, out AppDomain domain))
            {
                return domain.CreateInstanceAndUnwrap(aTypeName, anAssemblyName);
            }
            else
            {
                throw new KeyNotFoundException($"{aDomainName}");
            }
        }
        bool UnloadDomain(string aDomainName)
        {
            if (m_AppDomains.TryGetValue(aDomainName, out AppDomain domain))
            {
                AppDomain.Unload(domain);
                return true;
            }
            else
            {
                throw new KeyNotFoundException($"{aDomainName}");
            }
        }
        void UnloadAllDomain()
        {
            if (m_AppDomains != null && !m_AppDomains.IsEmpty)
            {
                foreach (var domain in m_AppDomains)
                {
                    AppDomain.Unload(domain.Value);
                }
            }
        }
    }
}
