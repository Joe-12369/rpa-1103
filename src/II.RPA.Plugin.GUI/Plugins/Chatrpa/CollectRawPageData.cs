using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Params;
using II.RPA.Plugin.GUI.Utils;
using II.RPA.WebBrowser.Common.Basic;
using II.RPA.WinAutomation.CommonUtility;
using IWshRuntimeLibrary;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Plugins.Chatrpa
{
    public class ParamCollectRawPageDataModel : ParamBase
    {
        public string guiType { get; set; } = string.Empty;
        public string windowType { get; set; } = string.Empty;
        public string fileName { get; set; } = string.Empty;
        //非入参
        [JsonIgnore]
        internal IntPtr WndHandle { get; set; }
    }
    public class CollectRawPageData : GuiComponentBase<ParamCollectRawPageDataModel>
    {
        protected override eComponentType ComponentType => eComponentType.None;
        protected override void ValidateParameter(ParamCollectRawPageDataModel paramModel) {
            if (paramModel.windowType == "window")
            {
                IntPtr hWnd = IntPtr.Zero;
                RPAUtils.Retry(() =>
                {
                    hWnd = ForegroundWinUtils.GetForegroundWindow();
                    return hWnd != IntPtr.Zero;
                }, 3000, 200);
                paramModel.WndHandle = hWnd;
            }
            else
            {
                var browserWindow = BrowserWindowProvider.GetFirstBrowserByZOrder();
                paramModel.guiType = browserWindow.WindowType.ToString();
                paramModel.WndHandle = browserWindow.Handle;
                paramModel.WindowInfo = browserWindow;
            }


            return;
        }
        protected override JMessage Execute(ParamCollectRawPageDataModel paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
        {
            string fileName1;
            string fileName2;
            string dir;
            string id;
            if (!string.IsNullOrEmpty(paramModel.fileName))
            {
                id = Path.GetFileNameWithoutExtension(paramModel.fileName);
                dir = Directory.GetParent(paramModel.fileName).FullName;
                fileName1 = paramModel.fileName;
                fileName2 = Path.Combine(dir, $"{id}.json");
            }
            else
            {
                id = paramModel.fileName ?? DateTime.Now.Ticks.ToString();
                dir = Path.Combine(GlobleConfigurations.PackageDirectory, "rawData", id);
                fileName1 = Path.Combine(dir, $"{id}.png");
                fileName2 = Path.Combine(dir, $"{id}.json");

            }
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            IntPtr hwnd = IntPtr.Zero;
            UiCommon.ForceShow(paramModel.WndHandle, false);
            if (paramModel.windowType != "window")
            {
                IGuiOperator_6_9_0 guiOperator = (IGuiOperator_6_9_0)GuiOperatorProvider.Instance.GetGuiOperator(paramModel.guiType, paramModel.WindowInfo);
                var data = ((IGuiWebOperator)guiOperator).GetViewElementsData(new Commons.GetElementsAllParams
                {
                    TabId = -1, //暂时使用激活页，口子预留
                });
                System.IO.File.WriteAllText(fileName2, data?.ToJsonNoneFormating());
                hwnd = NativeMethods.FindWindowEx(paramModel.WndHandle, IntPtr.Zero, "Chrome_RenderWidgetHostHWND", null);
            }
            if (hwnd == IntPtr.Zero)
            {
                hwnd = paramModel.WndHandle;
            }

            IGuiNode_6_6_0 CGuiNode = (IGuiNode_6_6_0)(GuiManager.CreateWindowsGuiNode(UiCommon.UIAAutomation.FromHandle(hwnd)));
            GuiElement element = new GuiElement(CGuiNode);
            element.Screenshot(fileName1);
            JMessage jMessage = new JMessage();
            jMessage.Info = id;
            return jMessage;
        }


    }
}
