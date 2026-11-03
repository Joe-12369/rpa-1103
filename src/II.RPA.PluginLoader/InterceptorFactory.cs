using II.RPA.Logging;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;

namespace II.RPA.PluginLoader
{
    internal static class InterceptorFactory
    {
        private static ILogger logger = Logger.OfContext();

        public static string Interceptor(string actionName, string jsonParam)
        {
            var parts = actionName.Split('.');
            if (parts.Length < 2) return actionName;

            var pluginName = parts[0];
            var action = parts[1];

            if (!pluginName.Equals("Element", StringComparison.OrdinalIgnoreCase)
                || !action.Equals("IsSimilarElementExist", StringComparison.OrdinalIgnoreCase))
                return actionName;

            if (string.IsNullOrEmpty(jsonParam))
                return actionName;

            var paramModel = jsonParam.ToObject<JObject>();
            if (paramModel == null)
                return actionName;

            if (!paramModel.TryGetValue("selector", out var selector))
                return actionName;

            CommonParseSelector.ParseSelectorV1(selector.ToString(), out var selectorType, out var version);

            return PluginNameInterceptor(pluginName, action, selectorType);
        }
        private static string PluginNameInterceptor(string pluginName, string action, string selectorType)
        {
            string actionName = pluginName + "." + action;
            string[] supportRenovate = { "chrome", "ie", "edge", "firefox", "web360se", "windows", "sogou", "web360x", "superbrowser", "zexplorer", "java", "web", "chromium", "wdterp" };
            logger.LogInformation($"Interceptor actionName {pluginName}.{action}");
            if (!supportRenovate.Contains(selectorType.ToLower()))
            {
                actionName = "Element.IsElementExist";
            }
            logger.LogInformation($"return actionName {actionName}");
            return actionName;
        }
    }
}
