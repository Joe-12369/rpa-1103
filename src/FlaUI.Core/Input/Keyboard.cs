using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using FlaUI.Core.Logging;
using FlaUI.Core.WindowsAPI;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;

namespace FlaUI.Core.Input
{
    /// <summary>
    /// Keyboard class to simulate key input.
    /// </summary>
    public static class Keyboard
    {
        /// <summary>
        /// 常用的扩展虚拟键键码
        /// </summary>
        private static VirtualKeyShort[] commonExtendVirtualKeys = new[]
        {
            VirtualKeyShort.UP,VirtualKeyShort.DOWN,VirtualKeyShort.LEFT,
            VirtualKeyShort.RIGHT, VirtualKeyShort.HOME, VirtualKeyShort.END ,
            VirtualKeyShort.DELETE, VirtualKeyShort.INSERT,VirtualKeyShort.NEXT,
            VirtualKeyShort.PRIOR,VirtualKeyShort.CLEAR
        };

        private static Microsoft.Extensions.Logging.ILogger logger
            = II.RPA.Logging.Logger.OfContext();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        public static void SendInput(string text)
        {
            // The encoding.
            UnicodeEncoding unicode = new UnicodeEncoding();
            // Create a string that contains Unicode characters.
            string unicodeString = text;
            foreach (var items in unicodeString)
            {
                //如果字符是中文符号
                if (Regex.IsMatch(items.ToString(), "[^\x00-\xff]"))
                {
                    SendKeys.SendWait(items.ToString());
                }
                else
                {
                    var encodedBytes = unicode.GetBytes(items.ToString());

                    foreach (var item in encodedBytes)
                    {
                        INPUT[] inputs = new INPUT[4];
                        var keyboardInput = new KEYINPUTUNION
                        {
                            time = 1,
                            dwExtraInfo = User32.GetMessageExtraInfo()
                        };
                        keyboardInput.dwFlags = KeyEventFlags.KEYEVENTF_UNICODE;
                        keyboardInput.wScan = item;
                        var input = INPUT.KeyboardInput(keyboardInput);
                        inputs[0] = input;
                        //https://stackoverflow.com/questions/12761169/send-keys-through-sendinput-in-user32-dll
                        keyboardInput.dwFlags = KeyEventFlags.KEYEVENTF_UNICODE | KeyEventFlags.KEYEVENTF_KEYUP;
                        input = INPUT.KeyboardInput(keyboardInput);
                        inputs[1] = input;

                        if (User32.SendInput(Convert.ToUInt32(inputs.Count()), inputs, INPUT.Size) == 0)
                        {
                            var errorCode = Marshal.GetLastWin32Error();
                            logger.LogDebug($"{nameof(SendInput)}打印错误代码：{errorCode}");
                        }
                    }
                }

                Thread.Sleep(20);//输入后等待 防止输入不完整情况
            }
        }

        /// <summary>
        /// Types the given text, one char after another.
        /// </summary>
        public static void Type(string text)
        {
            foreach (var c in text)
            {
                Type(c);
            }
        }

        /// <summary>
        /// Types the given character.
        /// </summary>
        public static void Type(char character)
        {
            var code = User32.VkKeyScan(character);
            // Check if the char is unicode or no virtual key could be found
            if (character > 0xFE || code == -1)
            {
                // It seems to be unicode
                SendInput(character, true, false, false, true);
                SendInput(character, false, false, false, true);
            }
            else
            {
                // Get the high-order and low-order byte from the code
                var high = (byte)(code >> 8);
                var low = (byte)(code & 0xff);

                // Check for caps lock and unset it
                var isCapsLockToggled = false;
                if ((User32.GetKeyState((int)VirtualKeyShort.CAPITAL) & 0x0001) != 0)
                {
                    isCapsLockToggled = true;
                    Type(VirtualKeyShort.CAPITAL);
                }

                // Check if there are any modifiers
                var modifiers = new List<VirtualKeyShort>();
                if (HasScanModifier(high, VkKeyScanModifiers.SHIFT))
                {
                    modifiers.Add(VirtualKeyShort.SHIFT);
                }
                if (HasScanModifier(high, VkKeyScanModifiers.CONTROL))
                {
                    modifiers.Add(VirtualKeyShort.CONTROL);
                }
                if (HasScanModifier(high, VkKeyScanModifiers.ALT))
                {
                    modifiers.Add(VirtualKeyShort.ALT);
                }
                // Press the modifiers
                foreach (var mod in modifiers)
                {
                    Press(mod);
                }
                // Type the effective key
                SendInput(low, true, false, false, false);
                SendInput(low, false, false, false, false);
                // Release the modifiers
                foreach (var mod in Enumerable.Reverse(modifiers))
                {
                    Release(mod);
                }

                // Re-toggle the caps lock if it was set before
                if (isCapsLockToggled)
                {
                    Type(VirtualKeyShort.CAPITAL);
                }
            }
        }

        /// <summary>
        /// Types the given keys, one by one.
        /// </summary>
        public static void Type(params VirtualKeyShort[] virtualKeys)
        {
            if (virtualKeys == null)
            {
                return;
            }
            foreach (var key in virtualKeys)
            {
                Press(key);
                Release(key);
            }
        }

        /// <summary>
        /// Types the given keys simultaneously (starting with the first).
        /// </summary>
        public static void TypeSimultaneously(params VirtualKeyShort[] virtualKeys)
        {
            if (virtualKeys == null)
            {
                return;
            }
            foreach (var key in virtualKeys)
            {
                Press(key);
            }
            foreach (var key in virtualKeys.Reverse())
            {
                Release(key);
            }
        }


        /// <summary>
        /// Types the given scan-code.
        /// </summary>
        public static void TypeScanCode(ushort scanCode, bool isExtendedKey)
        {
            PressScanCode(scanCode, isExtendedKey);
            ReleaseScanCode(scanCode, isExtendedKey);
        }

        /// <summary>
        /// Types the given virtual key-code.
        /// </summary>
        public static void TypeVirtualKeyCode(ushort virtualKeyCode)
        {
            PressVirtualKeyCode(virtualKeyCode);
            ReleaseVirtualKeyCode(virtualKeyCode);
        }

        /// <summary>
        /// Presses the given key.
        /// </summary>
        public static void Press(VirtualKeyShort virtualKey)
        {
            PressVirtualKeyCode((ushort)virtualKey);
        }

        /// <summary>
        /// Presses the given scan-code.
        /// </summary>
        public static void PressScanCode(ushort scanCode, bool isExtendedKey)
        {
            SendInput(scanCode, true, true, isExtendedKey, false);
        }

        /// <summary>
        /// Presses the given virtual key-code.
        /// </summary>
        public static void PressVirtualKeyCode(ushort virtualKeyCode)
        {
            var isExtend = commonExtendVirtualKeys.Select(x => (ushort)x).Any(x => x == virtualKeyCode);
            SendInput(virtualKeyCode, true, false, isExtend, false);
        }

        /// <summary>
        /// Releases the given key.
        /// </summary>
        public static void Release(VirtualKeyShort virtualKey)
        {
            ReleaseVirtualKeyCode((ushort)virtualKey);
        }

        /// <summary>
        /// Releases the given scan-code.
        /// </summary>
        public static void ReleaseScanCode(ushort scanCode, bool isExtendedKey)
        {
            SendInput(scanCode, false, true, isExtendedKey, false);
        }

        /// <summary>
        /// Releases the given virtual key-code.
        /// </summary>
        public static void ReleaseVirtualKeyCode(ushort virtualKeyCode)
        {
            var isExtend = commonExtendVirtualKeys.Select(x => (ushort)x).Any(x => x == virtualKeyCode);
            SendInput(virtualKeyCode, false, false, isExtend, false);
        }

        /// <summary>
        /// Presses the given keys and releases them when the returned object is disposed.
        /// </summary>
        public static IDisposable Pressing(params VirtualKeyShort[] virtualKeys)
        {
            foreach (var key in virtualKeys)
            {
                Press(key);
            }

            return new ActionDisposable(() =>
            {
                foreach (var key in virtualKeys.Reverse())
                {
                    Release(key);
                }
            });
        }

        /// <summary>
        /// Checks if a given byte has a specific VkKeyScan-modifier set.
        /// </summary>
        private static bool HasScanModifier(byte b, VkKeyScanModifiers modifierToTest)
        {
            return (VkKeyScanModifiers)(b & (byte)modifierToTest) == modifierToTest;
        }

        /// <summary>
        /// Effectively sends the keyboard input command.
        /// </summary>
        /// <param name="keyCode">The key code to send. Can be the scan code or the virtual key code.</param>
        /// <param name="isKeyDown">Flag if the key should be pressed or released.</param>
        /// <param name="isScanCode">Flag if the code is the scan code or the virtual key code.</param>
        /// <param name="isExtended">Flag if the key is an extended key.</param>
        /// <param name="isUnicode">Flag if the key is unicode.</param>
        private static void SendInput(ushort keyCode, bool isKeyDown, bool isScanCode, bool isExtended, bool isUnicode)
        {
            // Prepare the basic object
            var keyboardInput = new KEYBDINPUT
            {
                time = 0,
                dwExtraInfo = User32.GetMessageExtraInfo()
            };

            // Add the "key-up" flag if needed. By default it is "key-down"
            if (!isKeyDown)
            {
                keyboardInput.dwFlags |= KeyEventFlags.KEYEVENTF_KEYUP;
            }

            if (isScanCode)
            {
                keyboardInput.wScan = keyCode;
                keyboardInput.dwFlags |= KeyEventFlags.KEYEVENTF_SCANCODE;
                // Add the extended flag if the flag is set or the keycode is prefixed with the byte 0xE0
                // See https://msdn.microsoft.com/en-us/library/windows/desktop/ms646267(v=vs.85).aspx
                if (isExtended || (keyCode & 0xFF00) == 0xE0)
                {
                    keyboardInput.dwFlags |= KeyEventFlags.KEYEVENTF_EXTENDEDKEY;
                }
            }
            else if (isUnicode)
            {
                keyboardInput.dwFlags |= KeyEventFlags.KEYEVENTF_UNICODE;
                keyboardInput.wScan = keyCode;
            }
            else
            {
                //虚拟按键也要考虑扩展键的情况，否则控制键上下左右、删除键、插入键、HOM END、PU 、PD会和小键盘碰撞。
                if (isExtended || (keyCode & 0xFF00) == 0xE0)
                {
                    keyboardInput.dwFlags |= KeyEventFlags.KEYEVENTF_EXTENDEDKEY;
                }

                keyboardInput.wVk = keyCode;
            }

            // Build the input object
            var input = INPUT.KeyboardInput(keyboardInput);
            // Send the command
            if (User32.SendInput(1, new[] { input }, INPUT.Size) == 0)
            {
                // An error occured
                var errorCode = Marshal.GetLastWin32Error();
                logger.LogWarning("Could not send keyboard input. ErrorCode: {0}", errorCode);
            }
        }

        public static void CombinedInput(params VirtualKeyShort[] virtualKeys)
        {
            if (virtualKeys == null)
            {
                return;
            }
            INPUT[] inputs = new INPUT[virtualKeys.Length * 2];
            for (int i = 0; i < virtualKeys.Length; i++)
            {
                var virtualKeyCode = (ushort)virtualKeys[i];
                var isExtend = commonExtendVirtualKeys.Select(x => (ushort)x).Any(x => x == virtualKeyCode);
                var input_down = CreateInput(virtualKeyCode, true, false, isExtend, false);
                var input_up = CreateInput(virtualKeyCode, false, false, isExtend, false);
                inputs[i] = input_down;
                inputs[inputs.Length - 1 - i] = input_up;
            }
            // Send the command
            if (User32.SendInput((uint)inputs.Length, inputs, INPUT.Size) != (uint)inputs.Length)
            {
                // An error occured
                var errorCode = Marshal.GetLastWin32Error();
                logger.LogWarning("Could not send keyboard input. ErrorCode: {0}", errorCode);
            }
        }
        private static INPUT CreateInput(ushort keyCode, bool isKeyDown, bool isScanCode, bool isExtended, bool isUnicode)
        {
            // Prepare the basic object
            var keyboardInput = new KEYBDINPUT
            {
                time = 0,
                dwExtraInfo = User32.GetMessageExtraInfo()
            };

            // Add the "key-up" flag if needed. By default it is "key-down"
            if (!isKeyDown)
            {
                keyboardInput.dwFlags |= KeyEventFlags.KEYEVENTF_KEYUP;
            }

            if (isScanCode)
            {
                keyboardInput.wScan = keyCode;
                keyboardInput.dwFlags |= KeyEventFlags.KEYEVENTF_SCANCODE;
                // Add the extended flag if the flag is set or the keycode is prefixed with the byte 0xE0
                // See https://msdn.microsoft.com/en-us/library/windows/desktop/ms646267(v=vs.85).aspx
                if (isExtended || (keyCode & 0xFF00) == 0xE0)
                {
                    keyboardInput.dwFlags |= KeyEventFlags.KEYEVENTF_EXTENDEDKEY;
                }
            }
            else if (isUnicode)
            {
                keyboardInput.dwFlags |= KeyEventFlags.KEYEVENTF_UNICODE;
                keyboardInput.wScan = keyCode;
            }
            else
            {
                //虚拟按键也要考虑扩展键的情况，否则控制键上下左右、删除键、插入键、HOM END、PU 、PD会和小键盘碰撞。
                if (isExtended || (keyCode & 0xFF00) == 0xE0)
                {
                    keyboardInput.dwFlags |= KeyEventFlags.KEYEVENTF_EXTENDEDKEY;
                }

                keyboardInput.wVk = keyCode;
            }

            // Build the input object
            var input = INPUT.KeyboardInput(keyboardInput);
            return input;
        }
    }
}
