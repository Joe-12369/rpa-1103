using II.RPA.Logging;
using II.RPA.Plugin.GUI.Deeps.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace II.RPA.Plugin.GUI.Deeps.Actions
{
    internal class ActionRouter
    {
        private readonly Dictionary<string, IActionHandler> _handlers;
        private static readonly ILogger _logger = Logger.OfContext();
        private readonly TrackService _tracker;
        private readonly IElementService _elementService;
        public ActionRouter(TrackService tracker, IElementService elementService)
        {
            _tracker = tracker;
            _elementService = elementService;
            _handlers = new Dictionary<string, IActionHandler>(StringComparer.OrdinalIgnoreCase)
            {
                { ActionNames.StartInspect, Intercept(ActionNames.StartInspect, new UiaMessageHandler(HandleStartInspect)) },
                { ActionNames.StopInspect,  Intercept(ActionNames.StopInspect, new UiaMessageHandler(_ => HandleStopInspect())) },
                { ActionNames.GetDescendantsCount, Intercept(ActionNames.GetDescendantsCount, new UiaMessageHandler(payload => _elementService.GetDescendantsCount(payload))) },
                { ActionNames.GetDescendants, Intercept(ActionNames.GetDescendants, new UiaMessageHandler(payload => _elementService.GetDescendants(payload))) },
                { ActionNames.BuildSelector, Intercept(ActionNames.BuildSelector, new UiaMessageHandler(payload => _elementService.BuildSelector(payload))) },
                { ActionNames.GetDetail, Intercept(ActionNames.GetDetail, new UiaMessageHandler(payload => _elementService.GetElementDetail(payload))) },
                { ActionNames.StartHighlight, Intercept(ActionNames.StartHighlight, new UiaMessageHandler(payload => _elementService.StartHighlight(payload))) },
                { ActionNames.StopHighlight, Intercept(ActionNames.StopHighlight, new UiaMessageHandler(payload => _elementService.StopHighlight(payload))) },
                //{ ActionNames.Peer, Intercept(ActionNames.Peer, new PeerHandler(_elementService.PeerElement)) }
            };
        }
        private Dictionary<string, IActionHandler> Handlers => _handlers;
        public string RouteAction(string action, object arg)
        {
            if (Handlers.TryGetValue(action, out var handler))
            {
                try
                {
                    var ret = handler.Execute(arg);
                    return ret;
                }
                catch (Exception ex)
                {
                    Microsoft.Extensions.Logging.LoggingExtensions.LogError(_logger, ex, $"Error while handling action: {action}");
                    return PayloadDto.Failed($"Error while handling action: {action};{ex.Message}").ToString();
                }
            }
            else
            {
                return PayloadDto.Failed($"Unknown action received: {action}").ToString();
            }
        }
        private string HandleStartInspect(string payload)
        {
            try
            {
                if (JObject.Parse(payload).TryGetValue("mode", out var modeToken) &&
                    Enum.TryParse(modeToken.ToString(), out InspectMode mode))
                {
                    _tracker.StartTrack(mode);
                    return PayloadDto.Success($"track start").ToString();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
            }

            _logger.LogError($"Invalid InspectMode received: {payload}");
            return PayloadDto.Failed().ToString();
        }
        private string HandleStopInspect()
        {
            try
            {
                _tracker.StopTrack();
                return PayloadDto.Success("track stop").ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
            }
            return PayloadDto.Failed().ToString();
        }
        private IActionHandler Intercept(string name, IActionHandler inner)
        {
            return new ActionInterceptor(name, inner);
        }
    }
}

