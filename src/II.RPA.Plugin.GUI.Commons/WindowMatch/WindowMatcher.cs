using II.RPA.Plugin.GUI.Commons.WindowMatch.Entities;
using II.RPA.Plugin.GUI.Commons.WindowMatch.MatchRule;
using System.Collections.Generic;

namespace II.RPA.Plugin.GUI.Commons.WindowMatch
{
    public class WindowMatcher
    {
        private List<WindowMatcherItem> windowMatcherItemList;

        public List<MatchConfig> MatchConfigs { get; set; }
        public WindowType WindowType { get; set; }
        public WindowMatcher(WindowType windowType, List<MatchConfig> matchConfigs)
        {
            this.WindowType = windowType;
            this.MatchConfigs = matchConfigs;
            windowMatcherItemList = new List<WindowMatcherItem>();
            SetNormalMatchHandlers();
        }

        private void SetNormalMatchHandlers()
        {
            foreach (var matchConfig in this.MatchConfigs)
            {
                SetNormalMatchHandlers(matchConfig);
            }
        }
        private void SetNormalMatchHandlers(MatchConfig matchConfig)
        {
            //非普通配置规则，参照II.RPA.Plugin.ChromeChrome.PluginPackage 中的设置模版
            if (!matchConfig.ConfigType.Equals(MatchConfigType.Normal))
            {
                return;
            }

            //普通配置执行顺序 按照：ClassNames > TitleFeatures > ProcessFeatures > IEClassNames ，特殊顺序 参照II.RPA.Plugin.ChromeChrome.PluginPackage 中的设置模版
            List<MatchHandler> matchHandlers = new List<MatchHandler>();
            if (matchConfig.BrowserWndClassNames.Length > 0)
            {
                matchHandlers.Add(new MainWndClassNameMatchHandler());
            }
            if (matchConfig.TitleFeatures.Length > 0)
            {
                matchHandlers.Add(new TitleMatchHandler());
            }
            if (matchConfig.ProcessFeatures.Length > 0)
            {
                matchHandlers.Add(new ProcessMatchHandler());
            }
            if (!string.IsNullOrEmpty(matchConfig.AppExePath))
            {
                //针对电商浏览器安装方式，定制化插件未配置AppExePath 
                if (string.IsNullOrEmpty(matchConfig.ProductName))
                    matchHandlers.Add(new AppExePathMatchHandler());
                else
                    matchHandlers.Add(new ProductNameMatchHandler());
            }
            if (matchHandlers.Count == 0)
                return;
            SetWindowMatcherItem(matchConfig, new EmbeddedIEMatchHandler(), matchHandlers.ToArray());
        }
        public void SetWindowMatcherItem(MatchConfig matchConfig, MatchHandler embeddedIEMatchHandler = null, params MatchHandler[] matchHandlers)
        {
            for (int i = matchHandlers.Length - 1; i > 0; i--)
            {
                matchHandlers[i - 1].SetNextHandler(matchHandlers[i]);
            }
            var matcherItem = new WindowMatcherItem
            {
                MatchConfig = matchConfig,
                WindowMatchHandler = matchHandlers[0],
                EmbeddedIEMatchHandler = embeddedIEMatchHandler
            };
            windowMatcherItemList.Add(matcherItem);
        }

        public bool IsMatch(BrowserWindow wndInfo)
        {
            foreach (var matchHandlerItem in windowMatcherItemList)
            {
                if (matchHandlerItem.WindowMatchHandler.IsMatch(wndInfo, matchHandlerItem.MatchConfig))
                {
                    wndInfo.WindowType = this.WindowType;
                    wndInfo.HitMatchHandler = matchHandlerItem;
                    return true;
                }
            }
            return false;
        }
    }
}
