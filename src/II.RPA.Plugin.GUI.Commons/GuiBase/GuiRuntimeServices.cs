using System;
using System.Collections.Generic;
using System.Linq;
using II.RPA.Plugin.Abstractions;

namespace II.RPA.Plugin.GUI.Commons
{
    public sealed class GuiRuntimeServices
    {
        public GuiRuntimeServices(
            IEnumerable<IGuiOperator> operators,
            IPluginInvoker pluginInvoker,
            IGuiCVOperator650 cvOperator,
            IGuiWindowsOperator windowsOperator,
            ICVGuiNodeFactory cvNodeFactory,
            IWindowsGuiNodeFactory windowsNodeFactory)
        {
            if (operators == null) throw new ArgumentNullException(nameof(operators));
            Operators = operators.OrderBy(value => value.Sort).ToArray();
            PluginInvoker = pluginInvoker ?? throw new ArgumentNullException(nameof(pluginInvoker));
            CVOperator = cvOperator ?? throw new ArgumentNullException(nameof(cvOperator));
            WindowsOperator = windowsOperator ?? throw new ArgumentNullException(nameof(windowsOperator));
            CVNodeFactory = cvNodeFactory ?? throw new ArgumentNullException(nameof(cvNodeFactory));
            WindowsNodeFactory = windowsNodeFactory ?? throw new ArgumentNullException(nameof(windowsNodeFactory));
        }

        public IReadOnlyList<IGuiOperator> Operators { get; }
        public IPluginInvoker PluginInvoker { get; }
        public IGuiCVOperator650 CVOperator { get; }
        public IGuiWindowsOperator WindowsOperator { get; }
        public ICVGuiNodeFactory CVNodeFactory { get; }
        public IWindowsGuiNodeFactory WindowsNodeFactory { get; }

        public string InvokePlugin(PluginRoute targetRoute, string jsonParam)
        {
            if (targetRoute == null) throw new ArgumentNullException(nameof(targetRoute));
            if (jsonParam == null) throw new ArgumentNullException(nameof(jsonParam));
            return PluginInvoker
                .InvokeAsync(targetRoute, jsonParam)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        }
    }
}
