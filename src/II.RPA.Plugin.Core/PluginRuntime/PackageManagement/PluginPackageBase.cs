using II.RPA.Plugin.Abstractions;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace II.RPA.Plugin.Core.PluginBase
{
    public abstract class PluginPackageBase : II.RPA.Plugin.Abstractions.IPluginPackage
    {
        private PackageDispatcher _dispatcher;
        private IServiceProvider _services;
        private bool _initialized;

        public abstract string PackageName { get; }

        public virtual IReadOnlyCollection<string> RoutePackageNames => new[] { PackageName };

        public async Task InitializeAsync(IPluginInvoker pluginInvoker)
        {
            if (_initialized) return;
            if (pluginInvoker == null) throw new ArgumentNullException(nameof(pluginInvoker));
            var builder = new PluginPackageBuilder(PackageName, RoutePackageNames);
            Configure(builder);
            builder.Services.AddSingleton(pluginInvoker);
            var services = builder.Services.BuildServiceProvider();
            _services = services;
            foreach (var registration in builder.RouteRegistrations.Values)
            {
                if (registration.ActivationMode == AbilityActivationMode.Eager) services.GetRequiredService(registration.AbilityType);
            }
            await InitializePackageAsync(pluginInvoker).ConfigureAwait(false);
            _dispatcher = new PackageDispatcher(services, builder.RouteRegistrations);
            _initialized = true;
        }

        public async Task<string> RunAsync(PluginExecutionContext context)
        {
            if (!_initialized)
            {
                OnError(eErrorMessageType.Error_PluginRun_ServerStatusUninit);
                return Error(eErrorMessageType.Error_PluginRun_ServerStatusUninit);
            }
            return await _dispatcher.DispatchAsync(context).ConfigureAwait(false);
        }

        protected abstract void Configure(PluginPackageBuilder builder);

        protected virtual Task InitializePackageAsync(IPluginInvoker pluginInvoker)
        {
            return Task.CompletedTask;
        }

        protected TService GetRequiredPackageService<TService>() where TService : class
        {
            if (_services == null) throw new InvalidOperationException("Package services are not available before initialization.");
            return _services.GetRequiredService<TService>();
        }

        protected virtual void OnError(eErrorMessageType errorType) { }

        private static string Error(eErrorMessageType errorType)
        {
            return new JMessage { Code = (int)errorType, UnifyCode = ((int)errorType).ToString(), Message = errorType.ToString() }.ToJson();
        }
    }
}
