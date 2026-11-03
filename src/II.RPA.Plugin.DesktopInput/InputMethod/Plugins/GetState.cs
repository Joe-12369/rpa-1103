using II.RPA.WinAutomation.CommonUtility;
using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.Plugin.DesktopInput.InputMethod.Params;
using II.RPA.WinAPI;
using Newtonsoft.Json.Linq;
using System;

namespace II.RPA.Plugin.DesktopInput.InputMethod
{
    public class GetState : PluginAbilityBase<ParamGetState>
    {
        protected override RPA.WinAutomation.CommonUtility.eComponentType ComponentType => WinAutomation.CommonUtility.eComponentType.GetInputMethodState;

        protected override void ValidateParameter(ParamGetState parameter)
        {
        }

        protected override JMessage Execute(
            ParamGetState paramModel,
            II.RPA.Plugin.Abstractions.PluginExecutionContext context)
        {
            JMessage msg = new JMessage();
            try
            {              
                var hwnd = User32.GetForegroundWindow();
                var hIme = Imm32.ImmGetDefaultIMEWnd(hwnd);
                var open_zn = User32.SendMessage(hIme, 0x283, new IntPtr(5), IntPtr.Zero);
                if (open_zn >0)
                {
                    var mode = User32.SendMessage(hIme, 0x283, new IntPtr(1), IntPtr.Zero);
                    var state = mode & 3;
                    int _char = 0;
                    if ((state & 0x400) > 0) _char = 3;//中文标点
                    else if ((state & 8) > 0) _char = 2;//英文全角
                    else _char = 1;//英文半角
                    var _state = state > 0 ? "中文" : "英文";

                    msg.Info = new JObject() { { "state", _state }, { "char", _char } }.ToString();
                }
                else
                {
                    msg.Info = new JObject() { { "state", "英文" } }.ToString();
                }
            }
            catch (Exception)
            {
                msg.Code = -1;
                msg.UnifyCode = "-1";
                msg.Message = "获取输入法状态失败".I18n("GetState_6587B4EA");

            }
            return msg;
        }
    }

}
