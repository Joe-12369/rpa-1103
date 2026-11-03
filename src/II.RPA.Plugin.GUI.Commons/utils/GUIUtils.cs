using FlaUI.Core.Input;
using II.RPA.Plugin.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Configuration;
using System.Threading;

namespace II.RPA.Plugin.GUI.Commons
{
    public class GUIUtils
    {
        private readonly static int ClickIntervalMs = 10;

        private static readonly Microsoft.Extensions.Logging.ILogger logger = II.RPA.Logging.Logger.OfContext();
        static GUIUtils()
        {
            try
            {
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var appSettings = config.AppSettings;
                var setting = appSettings.Settings["MouseClickIntervalms"];
                if (setting != null)
                {
                    var intervalStr = setting.Value;
                    if (int.TryParse(intervalStr, out int interval))
                    {
                        ClickIntervalMs = interval;
                    }
                }
                logger.LogInformation($"MouseClickIntervalms:{ClickIntervalMs}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
            }
        }

        private static void MouseBouttonClick(RPAMouseButtons mouseButtons)
        {
            switch (mouseButtons)
            {
                case RPAMouseButtons.Left:
                    if (ClickIntervalMs < 0)
                    {
                        LeftButtonClickOnce();
                    }
                    else
                    {
                        Mouse.Click(ClickIntervalMs);
                    }
                    break;
                case RPAMouseButtons.Right:
                    Mouse.Click(MouseButton.Right);
                    break;
                case RPAMouseButtons.Middle:
                    Mouse.Click(MouseButton.Middle);
                    break;
                default:
                    break;
            }
        }
        private static void MouseBouttonDown(RPAMouseButtons mouseButtons)
        {
            switch (mouseButtons)
            {
                case RPAMouseButtons.Left:
                    Mouse.Down();
                    break;
                case RPAMouseButtons.Right:
                    Mouse.Down(MouseButton.Right);
                    break;
                case RPAMouseButtons.Middle:
                    Mouse.Down(MouseButton.Middle);
                    break;
                default:
                    break;
            }
        }
        private static void MouseBouttonUp(RPAMouseButtons mouseButtons)
        {
            switch (mouseButtons)
            {
                case RPAMouseButtons.Left:
                    Mouse.Up();
                    break;
                case RPAMouseButtons.Right:
                    Mouse.Up(MouseButton.Right);
                    break;
                case RPAMouseButtons.Middle:
                    Mouse.Up(MouseButton.Middle);
                    break;
                default:
                    break;
            }
        }
        public static bool MouseAction(RPAMousePresses mousePresses, RPAMouseButtons mouseButtons)
        {
            switch (mousePresses)
            {
                case RPAMousePresses.Up:
                    MouseBouttonUp(mouseButtons);
                    break;
                case RPAMousePresses.Down:
                    MouseBouttonDown(mouseButtons);
                    break;
                case RPAMousePresses.Click:
                    MouseBouttonClick(mouseButtons);
                    break;
                default:
                    break;
            }
            return true;
        }
        public static bool LeftButtonClickOnce()
        {
            Mouse.LeftButtonClickOnce();
            return true;
        }
        /// <summary>
        /// 按键
        /// </summary>
        /// <param name="aidKeyType">按键</param>
        /// <param name="actionType">按下|释放</param>
        /// <returns></returns>
        public static bool KeyBoardAction(AidKeyType aidKeyType, int actionType)
        {
            FlaUI.Core.WindowsAPI.VirtualKeyShort virtualKeyShort;
            switch (aidKeyType)
            {
                case AidKeyType.None:
                    return false;
                case AidKeyType.Alt:
                    virtualKeyShort = FlaUI.Core.WindowsAPI.VirtualKeyShort.ALT;
                    break;
                case AidKeyType.Ctrl:
                    virtualKeyShort = FlaUI.Core.WindowsAPI.VirtualKeyShort.CONTROL;
                    break;
                case AidKeyType.Shift:
                    virtualKeyShort = FlaUI.Core.WindowsAPI.VirtualKeyShort.SHIFT;
                    break;
                case AidKeyType.Win:
                    virtualKeyShort = FlaUI.Core.WindowsAPI.VirtualKeyShort.LWIN;
                    break;
                default:
                    return false;
            }
            if (actionType == 0)
            {
                Keyboard.Press(virtualKeyShort);
            }
            else
            {
                Keyboard.Release(virtualKeyShort);
            }
            return true;
        }
        public static void HandleClickEventWithAidKey(AidKeyType aidKeyType, Action clickAction)
        {
            GUIUtils.KeyBoardAction(aidKeyType, 0);
            Thread.Sleep(200);//按下后等个200ms，防止未生效
            clickAction.Invoke();
            GUIUtils.KeyBoardAction(aidKeyType, 1);
        }
    }
}
