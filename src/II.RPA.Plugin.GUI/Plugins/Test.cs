using FlaUI.Core.AutomationElements;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Entities;
using II.RPA.Plugin.GUI.Commons;
using II.RPA.Plugin.GUI.Commons.WindowMatch;
using II.RPA.Plugin.GUI.Params;
using II.RPA.Plugin.GUI.Utils;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace II.RPA.Plugin.GUI.Plugins
{
    public class Test : GuiComponentBase<ParamBase>
    {
        private const int retryTimes = 3;
        protected override eComponentType ComponentType => eComponentType.SetText;
        protected override eErrorMessageType OperationTimeoutMsgType => eErrorMessageType.Error_SetText_OperationTomeout_Error;


        private static ILogger logger = Logger.OfContext();

        protected override void ValidateParameter(ParamBase paramModel) {
            paramModel = new ParamBase();
            return;
        }

        protected override JMessage Execute(ParamBase paramModel, II.RPA.Plugin.Abstractions.PluginExecutionContext executionContext)
        {
            var gui = (IGuiOperator_6_9_0)GuiOperatorProvider.Instance.GetGuiOperator(eGuiType.ZExplorer);
            System.Drawing.Point point = new System.Drawing.Point(1240, 440);
            var pointWnd = UiCommon.UIAAutomation.FromPoint(point);
            var process = Process.GetProcessesByName("zexplore").First().MainWindowHandle;
            var rootWnd = UiCommon.UIAAutomation.FromHandle(process);

            //WindowObject wnd = new WindowObject(pointWnd.Properties.NativeWindowHandle);
            //var rootWnd = UiCommon.UIAAutomation.FromHandle(wnd.RootWindow);

            gui.InspectNode(point, rootWnd, pointWnd, "", out var inspectNode);
            //((IGuiNode_6_6_0)inspectNode).SetText()

            return new JMessage(); ;
        }
    }
}
