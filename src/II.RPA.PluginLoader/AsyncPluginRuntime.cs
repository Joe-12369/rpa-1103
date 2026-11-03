using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using II.RPA.Plugin.Abstractions;
using II.RPA.Plugin.Core;
using II.RPA.WinAutomation.CommonUtility;

namespace II.RPA.PluginLoader
{
    public sealed class AsyncPluginRuntime
    {
        private readonly IReadOnlyDictionary<string, IPluginPackage> packages;
        private readonly IReadOnlyDictionary<string, IPluginPackage> routes;
        private readonly IPluginInvoker invoker;
        private readonly HashSet<string> initializedPackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly string[] BasePackageInitializationOrder =
        {
            "CommonDLLBase",
            "Browsers",
            "GUIIE",
            "GUIZExplorer",
            "Element"
        };

        public static AsyncPluginRuntime FromDirectory(string runtimeDirectory)
        {
            return new AsyncPluginRuntime(new PluginPackageDiscovery().LoadPackages(runtimeDirectory));
        }

        public AsyncPluginRuntime(IEnumerable<IPluginPackage> packages)
        {
            if (packages == null)
            {
                throw new ArgumentNullException(nameof(packages));
            }

            var packageMap = new Dictionary<string, IPluginPackage>(StringComparer.OrdinalIgnoreCase);
            foreach (var package in packages)
            {
                if (package == null)
                {
                    throw new ArgumentException("A plugin package cannot be null.", nameof(packages));
                }

                ValidatePackageName(package.PackageName);

                packageMap.Add(package.PackageName, package);
            }

            var routeMap = new Dictionary<string, IPluginPackage>(StringComparer.OrdinalIgnoreCase);
            foreach (var package in packageMap.Values)
            {
                var names = package.RoutePackageNames;
                if (names == null) throw new ArgumentException("Plugin route package names cannot be null.", nameof(packages));
                var uniqueNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var name in names)
                {
                    ValidatePackageName(name);
                    if (!uniqueNames.Add(name) || routeMap.ContainsKey(name)
                        || (packageMap.TryGetValue(name, out var owner) && !ReferenceEquals(owner, package)))
                        throw new ArgumentException("The plugin route package name is already registered.", nameof(packages));
                    routeMap.Add(name, package);
                }
                if (uniqueNames.Count == 0) throw new ArgumentException("A plugin package must declare at least one route package name.", nameof(packages));
            }

            this.packages = packageMap;
            routes = routeMap;
            invoker = new PluginInvoker(this);
        }

        public bool IsInitialized { get; private set; }

        public async Task InitAsync()
        {
            if (IsInitialized)
            {
                return;
            }

            IsInitialized = false;
            foreach (var packageName in BasePackageInitializationOrder)
            {
                IPluginPackage package;
                if (!packages.TryGetValue(packageName, out package)) continue;
                await package.InitializeAsync(invoker).ConfigureAwait(false);
                initializedPackages.Add(package.PackageName);
            }

            IsInitialized = true;
        }

        public Task<string> RunAsync(string route, string action, string jsonParam)
        {
            if (!IsSupportedAction(action))
            {
                return Task.FromResult(Error(eErrorMessageType.Error_PluginRunError));
            }

            var context = new PluginExecutionContext(
                PluginRoute.Parse(route),
                jsonParam,
                Guid.NewGuid().ToString("N"));

            return DispatchAsync(context);
        }

        private async Task<string> DispatchAsync(PluginExecutionContext context)
        {
            if (!IsInitialized)
            {
                return Error(eErrorMessageType.Error_PluginRun_ServerStatusUninit);
            }

            IPluginPackage package;
            if (!routes.TryGetValue(context.Route.PackageName, out package))
            {
                return Error(
                    eErrorMessageType.Error_PluginAbilityNotFoundError,
                    context.Route.PackageName);
            }

            if (!initializedPackages.Contains(package.PackageName))
            {
                try
                {
                    await package.InitializeAsync(invoker).ConfigureAwait(false);
                    initializedPackages.Add(package.PackageName);
                }
                catch
                {
                    return Error(eErrorMessageType.Error_PluginGetAbilityError);
                }
            }

            return await package.RunAsync(context).ConfigureAwait(false);
        }

        private static bool IsSupportedAction(string action)
        {
            return string.IsNullOrEmpty(action)
                || string.Equals(action, "Excute", StringComparison.OrdinalIgnoreCase);
        }

        private static void ValidatePackageName(string packageName)
        {
            if (string.IsNullOrWhiteSpace(packageName)
                || !string.Equals(packageName, packageName.Trim(), StringComparison.Ordinal)
                || packageName.Contains("."))
            {
                throw new ArgumentException("The plugin package name is invalid.", nameof(packageName));
            }
        }

        private static string Error(eErrorMessageType errorType, params object[] messageArguments)
        {
            var exception = new RPAException(errorType);
            var message = exception.ErrorMessage;
            if (messageArguments != null && messageArguments.Length > 0)
            {
                message = string.Format(message, messageArguments);
            }

            return new JMessage
            {
                Code = exception.ErrorCode,
                UnifyCode = exception.UnifyErrorCode,
                Message = message
            }.ToJson();
        }

        private sealed class PluginInvoker : IPluginInvoker
        {
            private readonly AsyncPluginRuntime runtime;

            public PluginInvoker(AsyncPluginRuntime runtime)
            {
                this.runtime = runtime;
            }

            public Task<string> InvokeAsync(
                PluginRoute targetRoute,
                string jsonParam)
            {
                var targetContext = new PluginExecutionContext(
                    targetRoute,
                    jsonParam,
                    Guid.NewGuid().ToString("N"));

                return runtime.DispatchAsync(targetContext);
            }
        }
    }
}
