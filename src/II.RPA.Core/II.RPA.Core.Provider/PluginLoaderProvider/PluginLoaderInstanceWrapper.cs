using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Core.Provider.PluginLoaderProvider
{
    /// <summary>
    /// PluginLoaderWrapper
    /// </summary>
    public class PluginLoaderInstanceWrapper : MarshalByRefObject
    {
        private static readonly PluginLoader.PluginLoader s_PluginLoader = new PluginLoader.PluginLoader();
        public Func<string, string> PluginLoaderCallBack
        {
            get;
            set;
        }
        public PluginLoaderInstanceWrapper()
        {
        }
        public PluginLoader.PluginLoader PluginLoader
        {
            get => s_PluginLoader;
        }
        public bool HasFirstInit
        {
            get;
            set;
        }
        public bool NeedRestart
        {
            get;
            set;
        } = false;
        public bool IsRunning
        {
            get;
            set;
        } = false;
        public override object InitializeLifetimeService()
        {
            return null;
        }
    }
}
