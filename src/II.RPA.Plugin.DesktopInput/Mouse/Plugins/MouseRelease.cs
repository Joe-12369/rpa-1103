using FlaUI.Core.Input;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.Abstractions;
using II.RPA.Plugin.DesktopInput.Mouse.Params;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;


namespace II.RPA.Plugin.DesktopInput.Mouse.Plugins
{

    public class MouseRelease : MouseComponentBase<ParamMouseReleaseModel>
    {
        public MouseRelease(IPluginInvoker pluginInvoker) : base(pluginInvoker) { }

        private ILogger logger = Logger.OfContext();
        protected override eComponentType ComponentType => eComponentType.MouseRelease;

        protected override JMessage Execute(ParamMouseReleaseModel paramModel, PluginExecutionContext context)
        {
            JMessage msg = new JMessage();
            ReleaseMouseKey();
            return msg;
        }

        protected override void ValidateParameter(ParamMouseReleaseModel paramModel)
        {
            try
            {
                return;
            }
            catch (Exception)
            {
                throw new RPAException(eErrorMessageType.Error_ParamsError);
            }
        }

        private void ReleaseMouseKey()
        {
            if ((NativeMethods.GetAsyncKeyState(0x01) & 0x8000) > 0)//鼠标左键按下状态
            {
                FlaUI.Core.Input.Mouse.Up(MouseButton.Left);
                logger.LogInformation("Mouse Key Left Release");
            }
            else if ((NativeMethods.GetAsyncKeyState(0x02) & 0x8000) > 0)
            {
                FlaUI.Core.Input.Mouse.Up(MouseButton.Right);
                logger.LogInformation("Mouse Key Right Release");
            }
            else if ((NativeMethods.GetAsyncKeyState(0x04) & 0x8000) > 0)
            {
                FlaUI.Core.Input.Mouse.Up(MouseButton.Middle);
                logger.LogInformation("Mouse Key Mid Release");
            }
            else
            {
                logger.LogInformation("No Mouse Key Released");
            }
            
        }
    }


}
