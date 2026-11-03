using FlaUI.Core.AutomationElements;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text;

namespace II.RPA.Plugin.GUI.ProcessCreator.ElementTrack
{
    public class ElementCacheProvider
    {
        private readonly ConcurrentDictionary<string, ElementCacheModel> elementCaches 
            = new ConcurrentDictionary<string, ElementCacheModel>();
        private static readonly ILogger logger = Logger.OfContext();

        public ElementCacheProvider()
        {
            
        }
        public void ClearCache()
        {
            elementCaches.Clear();
        }
        public void CacheElement(string anElementId, 
            ElementCacheModel anElementCacheModel)
        {
            elementCaches.AddOrUpdate(anElementId, anElementCacheModel, (id, model) => model);
        }
        public void CacheElement(AutomationElement anElement)
        {
            if (anElement != null)
            {

                if (anElement.Properties.HelpText.IsSupported
                    && anElement.Properties.AutomationId.IsSupported)
                {
                    if (!string.IsNullOrEmpty(anElement.Properties.HelpText.ValueOrDefault)
                        && !string.IsNullOrEmpty(anElement.Properties.AutomationId.ValueOrDefault))
                    {
                        var automationid = new StringBuilder(anElement.Properties.AutomationId.ValueOrDefault);
                        var helptext = new StringBuilder(anElement.Properties.HelpText.ValueOrDefault);
                        var elementmodel = new ElementCacheModel
                        {
                            HelpText = helptext.ToString() 
                        };
                        logger.LogDebug($"CacheElement:{automationid};{helptext}");
                        elementCaches.AddOrUpdate(automationid.ToString(), elementmodel, (id, model) => 
                        {
                            logger.LogDebug($"Update CacheElement:{id};{elementCaches[id].HelpText};{model.HelpText}");
                            return model;
                        });
                    }
                }
            }
        }
        public string GetCacheElementHelpText(AutomationElement anElement)
        {
            if (anElement != null) 
            {
                if (anElement.Properties.AutomationId.IsSupported
                    && !string.IsNullOrEmpty(anElement.Properties.AutomationId.ValueOrDefault))
                {
                    if (elementCaches.TryGetValue(anElement.Properties.AutomationId.ValueOrDefault,
                        out ElementCacheModel elementCacheModel))
                    {
                        return elementCacheModel.HelpText;
                    }
                    else
                    {
                        logger.LogWarning($"Can not find {anElement.Properties.AutomationId.ValueOrDefault}");
                    }
                }
                else
                {
                    logger.LogWarning($"{anElement.Properties.Name.ValueOrDefault}:AutomationId.IsSupported = false");
                }
            }

            return null;
        }
        public ElementCacheModel? GetElement(string anElementId)
        {
            if (elementCaches.TryGetValue(anElementId, out var element))
            {
                return element;
            }
            else
            {
                return null;
            }          
        }
    }

    public struct ElementCacheModel
    {
        public string HelpText
        {
            get;
            set;
        }
    }

}
