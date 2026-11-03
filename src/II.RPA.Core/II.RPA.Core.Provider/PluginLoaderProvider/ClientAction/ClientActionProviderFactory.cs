using II.RPA.Core.Log;
using II.RPA.Core.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Core.Provider.PluginLoaderProvider.ClientAction
{
    public class ClientActionProviderFactory
    {
        private readonly IEnumerable<Type> m_ClientActionProviderTypes;
        private readonly ILogger m_Logger = null;
        public ClientActionProviderFactory(ILogger aLogger)
        {
            m_Logger = aLogger;
            m_ClientActionProviderTypes = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.GetInterfaces().Contains(typeof(IClientActionProvider)));
        }
        /// <summary>
        /// resolve action provider
        /// </summary>
        /// <param name="aClientActionName"></param>
        /// <param name="aRuntimeHostController">个别action需要调用pluginloader进程参数包装(bad design)</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public IClientActionProvider ResolveProvider(string aClientActionName,IRuntimeHostController aRuntimeHostController)
        {
            var wantedProvider = m_ClientActionProviderTypes.FirstOrDefault(t => t.GetCustomAttribute<ClientActionNameAttribute>()?.ClientAction == aClientActionName);
            if (wantedProvider == null)
            {
                throw new ArgumentException($"Can not find the sepecific provider : {aClientActionName}");
            }
            m_Logger.LogDebug($"Get provider:{wantedProvider}");
            return Activator.CreateInstance(wantedProvider, m_Logger, aRuntimeHostController) as IClientActionProvider;
        }
    }
}
