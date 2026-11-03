using FlaUI.Core;
using FlaUI.UIA3.Converters;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using System;
using UIA = Interop.UIAutomationClient;

namespace FlaUI.UIA3.Extensions
{
    public static class CacheRequestExtensions
    {
        private static ILogger logger = Logger.OfContext();
        public static UIA.IUIAutomationCacheRequest ToNative(this CacheRequest cacheRequest, UIA3Automation automation)
        {
            var nativeCacheRequest = automation.NativeAutomation.CreateCacheRequest();
            nativeCacheRequest.AutomationElementMode = (UIA.AutomationElementMode)cacheRequest.AutomationElementMode;
            nativeCacheRequest.TreeFilter = ConditionConverter.ToNative(automation, cacheRequest.TreeFilter);
            nativeCacheRequest.TreeScope = (UIA.TreeScope)cacheRequest.TreeScope;

            foreach (var pattern in cacheRequest.Patterns)
            {
                logger.LogDebug($"cacheRequest.Patterns Add {pattern.Id}");
                if (pattern.Id != -1)
                {
                    try
                    {
                        nativeCacheRequest.AddPattern(pattern.Id);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"cacheRequest.Patterns Add {pattern.Id} ex:{ex.ToString()}");
                        continue;
                    }
                }
            }

            foreach (var property in cacheRequest.Properties)
            {
                logger.LogDebug($"cacheRequest.Properties Add {property.Id}");
                if (property.Id != -1)
                {
                    try
                    {
                        nativeCacheRequest.AddProperty(property.Id);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"cacheRequest.Properties Add {property.Id} ex:{ex.ToString()}");
                        continue;
                    }
                }
            }
            return nativeCacheRequest;
        }
    }
}
