using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.Core.Threading;
using II.RPA.Plugin.Abstractions;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.WebBrowser.Params;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.WebBrowser.Plugins
{
    public abstract class BrowserComponentBase<TParam> : AsyncPluginAbilityBase<TParam> where TParam : ParamBase, new()
    {
        private static ILogger logger = Logger.OfContext();

        protected override void ValidateParameter(TParam parameter)
        {
        }

        protected virtual JMessage Execute(TParam parameter, II.RPA.Plugin.Abstractions.PluginExecutionContext context)
        {
            throw new NotSupportedException($"{GetType().FullName} 未实现 Execute。");
        }

        protected override Task<JMessage> ExecuteAsync(TParam parameter, II.RPA.Plugin.Abstractions.PluginExecutionContext context)
        {
            return Task.FromResult(Execute(parameter, context));
        }

    }

    public abstract class StaBrowserComponentBase<TParam> : AsyncPluginAbilityBase<TParam>
        where TParam : ParamBase, new()
    {
        private readonly IStaDispatcher staDispatcher;

        protected StaBrowserComponentBase(IStaDispatcher staDispatcher)
        {
            this.staDispatcher = staDispatcher ?? throw new ArgumentNullException(nameof(staDispatcher));
        }

        protected sealed override Task<JMessage> ExecuteAsync(
            TParam parameter,
            PluginExecutionContext context)
        {
            return staDispatcher.InvokeAsync(() => Execute(parameter, context));
        }

        protected abstract JMessage Execute(
            TParam parameter,
            PluginExecutionContext context);
    }
}

