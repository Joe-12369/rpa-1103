using FlaUI.Core.Input;
using FlaUI.Core.Tools;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Commons.WindowMatch;
using II.RPA.Plugin.IE.V2.Automation.Provider;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Drawing;
using System.Threading;

namespace II.RPA.Plugin.IE.V2
{
    public class IEGuiNode : ElementGuiNode
    {
        public override bool IsUseControlSend => true;
        public IEGuiNode(PageElement pageElement) : base(pageElement)
        {

        }

        public override void MouseMove(int cursorPos, int offsetX = 0, int offsetY = 0, bool isUsePerson = false)
        {
            //延用之前移动逻辑
            //后续可以考虑移除
            base.MouseMove(cursorPos, offsetX, offsetY, isUsePerson);
            //Thread.Sleep(50);
            Point pt = new Point(this.BoundingRectangle.X, this.BoundingRectangle.Y);
            IntPtr hWnd = NativeMethods.WindowFromPoint(pt);
            //NativeMethods.ScreenToClient(hWnd, ref pt);
            int cx = pt.X;
            int cy = pt.Y;
            //NativeMethods.SetFocus(hWnd);
            NativeMethods.PostMessage(hWnd, NativeMethods.WM_MOUSEOVER, (IntPtr)0, (IntPtr)(cx + (cy << 16)));
        }
        public override string GetSelector(bool innerTextFirst)
        {
            BrowserWindow browserWindow = BrowserWindowProvider.MatchBrowserWindowByHandle(PageElement.TabPage.BrowserWindowInfo.Handle);
            if (browserWindow == null || browserWindow.WindowType == (WindowType.Windows | WindowType.IE))
            {
                ExtendedTags |= ExtendedTags.EmbeddedIE;
                this.ProcessID = 0;
            }
            else
            {
                this.ProcessID = (int)browserWindow.ProcessId;
            }

            return base.GetSelector(innerTextFirst);
        }

        public override string Executejs(string scriptCode, string @params, string executionEnvironment = "", bool isAsyncCode = false, float waitResultTimeout = 0, bool catchException = false)
        {
            if (isAsyncCode)
            {
                throw new IEException(eErrorMessageType.Error_IEAsyncExecutejsError);
            }
            return base.Executejs(scriptCode, @params, executionEnvironment, isAsyncCode, waitResultTimeout, catchException);
        }
    }
}
