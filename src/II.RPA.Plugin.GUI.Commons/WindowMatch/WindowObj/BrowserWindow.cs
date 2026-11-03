using AutoIt;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Commons.WindowMatch.Entities;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;

namespace II.RPA.Plugin.GUI.Commons.WindowMatch
{
    public class BrowserWindow : WindowObject
    {

        public BrowserWindow(IntPtr handle) : base(handle) { }

        #region 业务属性
        public WindowMatcherItem HitMatchHandler { get; set; }

        public string TabDetailJson { get; set; }
        public int SpecifiedTabId { get; set; } = -1;
        public int SpecifiedWindowId { get; set; }
        public bool SpecifiedWindow { get; set; }
        #endregion

        public bool IsEmbeddedIE()
        {
            if ((this.WindowType & WindowType.IE) > 0)
            {
                return false;
            }
            return this.HitMatchHandler.EmbeddedIEMatchHandler.IsMatch(this, this.HitMatchHandler.MatchConfig);
        }
        public bool IsRenderWindow(WindowObject windowInfo)
        {
            if ((this.WindowType & WindowType.IE) == 0)
                return this.HitMatchHandler.MatchConfig.RenderClassNames.Any((claName) => windowInfo.ClassName.EqualIgnoreCase(claName));
            else
                return this.HitMatchHandler.MatchConfig.IERenderClassNames.Any((claName) => windowInfo.ClassName.EqualIgnoreCase(claName));
        }

        public virtual void SetForeground()
        {
            if (this.Handle == IntPtr.Zero)
            {
                return;
            }
            this.Activate(2000);
            //UiCommon.ForceShow(this.Handle, false);
            //RPAUtils.Retry(delegate
            //{
            //    if (AutoItX.WinActive(this.Handle) == 0)
            //    {
            //        AutoItX.WinActivate(this.Handle);
            //        return false;
            //    }
            //    return true;
            //}, 2000);
        }
    }
}
