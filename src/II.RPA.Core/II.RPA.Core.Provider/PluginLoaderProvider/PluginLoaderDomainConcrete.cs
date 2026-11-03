using II.RPA.Core.Common;
using II.RPA.Core.Common.Generic;
using II.RPA.Core.Provider.Command;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity;
using Unity.Lifetime;

namespace II.RPA.Core.Provider.PluginLoaderProvider
{
    public static class PluginLoaderDomainConcrete
    {
        private static readonly BlockingCollection<ConcreteCommand> s_CommandCollection;
        private static readonly ConcurrentDictionary<string, IndeedAppDomain<PluginLoaderInstanceWrapper>> s_PluginLoaderDictionary;
        private static readonly object s_SyncObject = new object();
        private static readonly DateTime s_StartTime = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
        //private static readonly IUnityContainer s_PluginLoaderContainer;
        private static readonly CancellationTokenSource s_CancellationTokenSource;
        private static Task s_ConsumTask;
        static PluginLoaderDomainConcrete()
        {
            s_CancellationTokenSource = new CancellationTokenSource();
            s_PluginLoaderDictionary = new ConcurrentDictionary<string, IndeedAppDomain<PluginLoaderInstanceWrapper>>();
            s_CommandCollection = new BlockingCollection<ConcreteCommand>(100);
            s_ConsumTask = new Task(ConsumeCommands);
            s_ConsumTask.ConfigureAwait(false);
            s_ConsumTask.Start();
        }
        private static long CurrentStamp
        {
            get
            {
                return (long)(DateTime.Now - s_StartTime).TotalMilliseconds;
            }
        }
        private static async Task CommandExecuteInQueue(ConcreteCommand aCommand)
        {
            await Task.Run(() =>
            {
                return _ = s_CommandCollection.TryAdd(aCommand, 2000);
            });
        }
        private static void ConsumeCommands()
        {
            while (!s_CommandCollection.IsCompleted)
            {
                //阻塞
                var commmand = s_CommandCollection.Take();
                commmand.ExecuteWithResult();
            }
        }
        public static async Task PluginLoaderSetParam(string aPackagePath, string aParam)
        {
            var currentPluginLoader = GetPluginLoaderToExecute(aPackagePath);
            var command = new PluginLoaderSetParamCommand(currentPluginLoader, aParam);
            await CommandExecuteInQueue(command);
        }
        public static async Task<object> PluginLoaderInit(string aPackagePath, string anInstallationPath)
        {
            var currentPluginLoader = GetPluginLoaderToExecute(aPackagePath);
            var command = new PluginLoaderInitCommand(currentPluginLoader, aPackagePath, anInstallationPath);
            await CommandExecuteInQueue(command);
            return await Task.FromResult(command.CommandResult);
        }
        public static async Task<object> PluginLoaderRun(string aPackagePath, string aPluginName, string anAction, string aJsonParam)
        {
            var currentPluginLoader = GetPluginLoaderToExecute(aPackagePath);
            var command = new PluginLoaderRunCommand(currentPluginLoader, aPluginName, anAction, aJsonParam);
            await CommandExecuteInQueue(command);
            return await Task.FromResult(command.CommandResult);
        }
        public static void PluginLoaderDispose(string aPackagePath)
        {
            if (s_PluginLoaderDictionary.TryRemove(aPackagePath, out IndeedAppDomain<PluginLoaderInstanceWrapper> value))
            {
                value.Unload();
            }
            //foreach (var registration in s_PluginLoaderContainer.Registrations.Where(p
            //    =>
            //    p.RegisteredType == typeof(PluginLoader.PluginLoader)
            //    && p.Name == aPackagePath
            //    && p.LifetimeManager.GetType() == typeof(ContainerControlledLifetimeManager)))
            //{
            //    registration.LifetimeManager.RemoveValue();
            //}
        }
        //private static PluginLoader.PluginLoader GetCurrentPluginLoader(string aPackagePath)
        //{
        //    lock (s_SyncObject)
        //    {
        //        if (!s_PluginLoaderContainer.IsRegistered(typeof(PluginLoader.PluginLoader), aPackagePath))
        //        {
        //            s_PluginLoaderContainer.RegisterType<PluginLoader.PluginLoader>(aPackagePath);
        //        }
        //        return s_PluginLoaderContainer.Resolve<PluginLoader.PluginLoader>(aPackagePath);
        //    }
        //}
        private static PluginLoaderInstanceWrapper GetPluginLoaderToExecute(string aPackagePath)
        {
            var domain = s_PluginLoaderDictionary.GetOrAdd(aPackagePath, new IndeedAppDomain<PluginLoaderInstanceWrapper>());
            return domain.Instance;
        }
    }
}
