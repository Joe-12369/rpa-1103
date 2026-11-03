using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.PluginBase;
using II.RPA.WinAPI;
using II.RPA.WinAPI.Struct;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using System;
using static II.RPA.Plugin.DesktopInput.InputMethod.Commons.ImmHelper;

namespace II.RPA.Plugin.DesktopInput.InputMethod
{
    public class SetState : PluginAbilityBase<Params.ParamSetState>
    {
        private static ILogger logger = Logger.OfContext();

        protected override eComponentType ComponentType => eComponentType.SetInputMethodState;

        protected override void ValidateParameter(Params.ParamSetState paramModel)
        {
        }

        protected override void NormalizeParameter(Params.ParamSetState paramModel)
        {
            if (string.IsNullOrEmpty(paramModel.State))
            {
                paramModel.State = "en_us";
            }
        }
        /// <summary>与 IME 通信的消息号（同 ImmHelper 中 643U）。</summary>
        private const int WmImePrivate = 0x283;
        /// <summary>SendMessage 对 IME 可能无限期阻塞；使用超时避免偶发卡死。</summary>
        private const int ImeSendTimeoutMs = 3000;

        protected override JMessage Execute(
            Params.ParamSetState paramModel,
            II.RPA.Plugin.Abstractions.PluginExecutionContext context)
        {
            JMessage msg = new JMessage();
            try
            {
                var hwnd = User32.GetForegroundWindow();
                if (hwnd == IntPtr.Zero || !User32.IsWindow(hwnd))
                {
                    msg.Code = -1;
                    msg.UnifyCode = "-1";
                    msg.Message = "设置输入法状态失败".I18n("SetState_7D64AB27");
                    return msg;
                }

                var hIme = GetWindowImeWnd(hwnd);
                if (hIme == IntPtr.Zero || !User32.IsWindow(hIme))
                {
                    msg.Code = -1;
                    msg.UnifyCode = "-1";
                    msg.Message = "设置输入法状态失败".I18n("SetState_7D64AB27");
                    return msg;
                }

                //IntPtr wParam = paramModel.State.Equals("zh_cn") ? new IntPtr(2) : new IntPtr(6);
                // 设置 IME 开 / 关状态的属性对应的是 6，这里为何来个2? 和SetWindowIme中也保持一致了
                IntPtr wParam = new IntPtr(6);
                IntPtr lParam = paramModel.State.Equals("zh_cn") ? new IntPtr(1) : IntPtr.Zero;
                int sent = User32.SendMessageTimeout(hIme, WmImePrivate, wParam, lParam,
                    (int)SendMessageTimeoutFlags.SMTO_NORMAL, ImeSendTimeoutMs, out _);
                // 切中文时，额外设置转换模式为 IME_CMODE_NATIVE
                if (paramModel.State.Equals("zh_cn") && sent != 0)
                {
                    User32.SendMessageTimeout(hIme, WmImePrivate, new IntPtr(2), new IntPtr(1),
                        (int)SendMessageTimeoutFlags.SMTO_NORMAL, ImeSendTimeoutMs, out _);
                }
                if (sent == 0)
                {
                    msg.Code = -1;
                    msg.UnifyCode = "-1";
                    msg.Message = "设置输入法状态失败".I18n("SetState_7D64AB27");
                    logger.LogWarning("SetState: SendMessageTimeout 未在 {Timeout}ms 内完成 (IME 句柄可能无响应)。", ImeSendTimeoutMs);
                }
            }
            catch (Exception)
            {
                msg.Code = -1;
                msg.UnifyCode = "-1";
                msg.Message = "设置输入法状态失败".I18n("SetState_7D64AB27");

            }
            return msg;
        }

    }
}

