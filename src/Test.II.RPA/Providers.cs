using FlaUI.Core;
using II.RPA.Plugin.Core.UI;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Deeps;
using II.RPA.Plugin.GUI.Deeps.Highlight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test.II.RPA
{
    public static class Providers
    {
        private static readonly UIProvider _provider;
        private static readonly TrackService _trackService;
        private static readonly ElementService _elementService;
        private static readonly HighlightService _highlightService;
        private static  readonly AutomationBase _automationBase;
        private static readonly WndService _wndService;
        static Providers()
        {
            _automationBase = UiCommon.UIAAutomation;
            _provider = new UIProvider();
            _highlightService = new HighlightService(_provider.UIDispatcher);
            _wndService = new WndService();
            _trackService = new TrackService(new UIProvider().UIDispatcher, _highlightService, _automationBase, _wndService);
            _elementService = new ElementService(_highlightService, _automationBase, _wndService);
        }
        public static TrackService TrackService => _trackService;
        public static ElementService ElementService => _elementService;
        public static HighlightService HighlightService => _highlightService;
        public static  AutomationBase Automation => _automationBase;
        public static WndService WndService => _wndService;
    }
}
