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
    public static class PluginLoaderConcrete
    {
        private static readonly BlockingCollection<ConcreteCommand> s_CommandCollection;
        private static readonly ConcurrentDictionary<string, PluginLoaderInstanceWrapper> s_PluginLoaderDictionary;
        private static readonly object s_SyncObject = new object();
        private static readonly DateTime s_StartTime = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
        private static readonly CancellationTokenSource s_CancellationTokenSource;
        private static readonly Thread s_ConsumeThread;
        static PluginLoaderConcrete()
        {
            s_CancellationTokenSource = new CancellationTokenSource();
            s_PluginLoaderDictionary = new ConcurrentDictionary<string, PluginLoaderInstanceWrapper>();
            s_CommandCollection = new BlockingCollection<ConcreteCommand>(100);
            s_ConsumeThread = new Thread(ConsumeCommandsSTA);
            ///该线程必须为true、否则可能会造成Application.Exit()无法结束整个进程
            s_ConsumeThread.IsBackground = true;
            s_ConsumeThread.SetApartmentState(ApartmentState.STA);
            s_ConsumeThread.Start();
            //ConsumeCommands(s_CancellationTokenSource.Token);
        }
        private static long CurrentStamp
        {
            get
            {
                return (long)(DateTime.Now - s_StartTime).TotalMilliseconds;
            }
        }
        [STAThread]
        private static async Task CommandExecuteInQueue(ConcreteCommand aCommand)
        {
            await Task.Run(() => 
            {
                return _ = s_CommandCollection.TryAdd(aCommand, 2000);
            });
        }
        
        private static void ConsumeCommandsSTA()
        {
            while (true)
            {
                //阻塞
                var commmand = s_CommandCollection.Take();
                commmand.ExecuteWithResult();
            }
        }
        public static async Task PluginLoaderSetParam(string aPackagePath,string aParam)
        {
            var currentPluginLoader = GetPluginLoaderToExecute(aPackagePath);
            var command = new PluginLoaderSetParamCommand(currentPluginLoader, aParam);
            await CommandExecuteInQueue(command);
        }
        public static async Task<object> PluginLoaderInit(string aPackagePath,string anInstallationPath)
        {
            var currentPluginLoader = GetPluginLoaderToExecute(aPackagePath);
            var command = new PluginLoaderInitCommand(currentPluginLoader, aPackagePath, anInstallationPath);
            await CommandExecuteInQueue(command);
            return await Task.FromResult(command.CommandResult);
        }
        public static async Task<object> PluginLoaderRun(string aPackagePath,string aPluginName,string anAction,string aJsonParam)
        {
            var currentPluginLoader = GetPluginLoaderToExecute(aPackagePath);
            if (!currentPluginLoader.HasFirstInit)
            {
                var commandinit = new PluginLoaderInitCommand(currentPluginLoader, aPackagePath, aPackagePath);
                await CommandExecuteInQueue(commandinit);
                await Task.FromResult(commandinit.CommandResult);
            }
            var command = new PluginLoaderRunCommand(currentPluginLoader, aPluginName, anAction, aJsonParam);
            await CommandExecuteInQueue(command);
            return await Task.FromResult(command.CommandResult);
        }
        public static void PluginLoaderDispose(string aPackagePath)
        {
            _ = s_PluginLoaderDictionary.TryRemove(aPackagePath, out _);
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
            return s_PluginLoaderDictionary.GetOrAdd(aPackagePath, 
            key =>
            {
                if (s_PluginLoaderDictionary.TryGetValue(aPackagePath, out PluginLoaderInstanceWrapper value))
                {
                    return value;
                }
                else
                {
                    return new PluginLoaderInstanceWrapper();
                }
            });
        }
    }
}
