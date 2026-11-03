using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using II.RPA.Logging;
using II.RPA.Plugin.GUI.Deeps.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;

namespace II.RPA.Plugin.GUI.Deeps
{
    public class ElementCacheService
    {
        private static readonly ILogger _logger = Logger.OfContext();
        private static readonly ConcurrentDictionary<string, ElementModel> _elements = new ConcurrentDictionary<string, ElementModel>();
        private readonly AutomationBase _automationProvider;
        public ElementCacheService(AutomationBase automationProvider)
        {
            _automationProvider = automationProvider;
        }

        public string Add(ElementModel element)
        {
            var hWnd = element.Handle;
            string elementId;
            if (!hWnd.HasValue || hWnd == IntPtr.Zero)
            {
                if (element.Automation != null)
                {
                    elementId = $"uia:{element.Automation.GetHashCode()}";
                }
                else
                {
                    elementId = $"uia:{element.GetHashCode()}";
                }
                _elements[elementId] = element;
            }
            else
            {
                elementId = $"wnd:{(int)hWnd}";
            }
            element.Id = elementId;
            return elementId;
        }
        public ElementModel Add(AutomationElement autoElement)
        {
            var element = autoElement.ToModel();
            GetOrAdd(element);
            return element;
        }
        public bool TryGet(string elementId, out ElementModel element)
        {
            element = null;

            if (string.IsNullOrEmpty(elementId))
            {
                _logger.LogWarning("TryGet failed: elementId is null or empty.");
                return false;
            }

            if (elementId.Length < 4)
            {
                _logger.LogWarning($"TryGet failed: elementId '{elementId}' is too short.");
                return false;
            }

            string type = elementId.Substring(0, 3);

            switch (type)
            {
                case "wnd":
                    {
                        if (!int.TryParse(elementId.Substring(4), out var hWnd))
                        {
                            _logger.LogWarning($"TryGet failed: invalid window handle format in elementId '{elementId}'.");
                            return false;
                        }

                        if (hWnd == 0)
                        {
                            _logger.LogWarning($"TryGet failed: window handle is zero, elementId '{elementId}'.");
                            return false;
                        }

                        IntPtr handle;
                        try
                        {
                            handle = new IntPtr(hWnd);
                        }
                        catch (Exception ex)
                        {
                            Microsoft.Extensions.Logging.LoggingExtensions.LogError(_logger, ex, $"TryGet failed: cannot convert hWnd {hWnd} to IntPtr.");
                            return false;
                        }

                        try
                        {
                            var automationElement = _automationProvider.FromHandle(handle);
                            if (automationElement == null)
                            {
                                _logger.LogWarning("TryGet failed: handle {Handle} is not a valid window.", hWnd);
                                return false;
                            }

                            element = automationElement.ToModel();
                            element.Id = elementId;
                            return true;
                        }
                        catch (Exception ex)
                        {
                            Microsoft.Extensions.Logging.LoggingExtensions.LogError(_logger, ex, "TryGet failed: exception while resolving handle {Handle}.", hWnd);
                            return false;
                        }
                    }

                case "uia":
                    {
                        if (!_elements.TryGetValue(elementId, out var uiaElement))
                        {
                            _logger.LogWarning("TryGet failed: elementId '{ElementId}' not found in cache.", elementId);
                            return false;
                        }
                        if (!uiaElement.Automation.IsAvailable)
                        {
                            _logger.LogWarning($"TryGet successfully,But element is unavailable: elementId '{elementId}'.");
                            _elements.TryRemove(elementId, out _);
                            return false;
                        }
                        if (!uiaElement.Automation.Properties.BoundingRectangle.IsSupported)
                        {
                            _logger.LogWarning($"TryGet successfully,But element's BoundingRectangle is unsupported: elementId '{elementId}'.");
                            _elements.TryRemove(elementId, out _);
                            return false;
                        }
                        element = uiaElement;
                        return true;
                    }

                default:
                    _logger.LogWarning("TryGet failed: unknown elementId prefix '{Prefix}' in '{ElementId}'.", type, elementId);
                    return false;
            }
        }


        public string GetOrAdd(ElementModel element)
        {
            if (!string.IsNullOrEmpty(element.Id))
            {
                _elements[element.Id] = element;
                return element.Id;
            }
            else
            {
                return Add(element);
            }
        }
        public void Remove(string[] elementIds)
        {
            foreach (string elementId in elementIds)
            {
                _elements.TryRemove(elementId, out _);
            }
        }
        public void Remove(string elementId)
        {
            _elements.TryRemove(elementId, out _);
        }
    }
}

