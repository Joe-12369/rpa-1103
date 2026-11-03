using System;
using System.Linq;
using System.Threading;
using AutoIt;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons.Params;
using Newtonsoft.Json.Linq;

namespace II.RPA.Plugin.GUI.Commons.Win32Tools
{
    public class Win32Action
    {
        private readonly IntPtr _hWnd;
        public Win32Action(IntPtr hWnd)
        {
            this._hWnd = hWnd;
        }
        public IntPtr Handle
        {
            get { return this._hWnd; }
        }
        public Win32Window Root
        {
            get
            {
                var rootHwnd = NativeMethods.GetAncestor(this._hWnd, GetAncestorFlags.GetRoot);
                return new Win32Window(rootHwnd);
            }
        }
        /// <summary>
        /// UINode 有重复代码 
        /// 本期优先解决 输入问题
        /// 后续从结构上做调整 统一代码
        /// </summary>
        /// <param name="interval"></param>
        /// <param name="keys"></param>
        private void TypeCombine(int interval, params VirtualKeyShort[] keys)
        {
            foreach (var key in keys)
            {
                Keyboard.Press(key);
                Thread.Sleep(interval);
            }

            foreach (var key in keys.Reverse())
            {
                Keyboard.Release(key);
            }
        }

        public JObject GetInputMethod()
        {
            string result = GuiRuntime.Services.InvokePlugin(new II.RPA.Plugin.Abstractions.PluginRoute("DesktopInput", "GetState"), "{}");
            var info = JObject.Parse(result)["info"];
            var state = JObject.Parse(info.ToString());
            return state;
        }
        public void SetInputMethod(string model = "en_us")
        {
            JObject j = new JObject();
            j.Add("state", model);
            var result = GuiRuntime.Services.InvokePlugin(new II.RPA.Plugin.Abstractions.PluginRoute("DesktopInput", "SetState"), j.ToString());
        }
        public void Input(string value, InputMode inputMode, bool isReplace, bool isSwitchInputMethod, int Interval = 50)
        {

            if (isReplace)
            {
                AutoItHelper.ClearWords(Interval);
            }
            else
            {
                AutoItHelper.MoveCursorToEnd(Interval);
            }
            Thread.Sleep(200);
            switch (inputMode)
            {
                case InputMode.SetValue:
                    SetText(value);
                    break;
                case InputMode.Simulate:
                    if (isSwitchInputMethod)
                    {
                        SetInputMethod();
                    }
                    AutoItHelper.Send(value, false, Interval);
                    break;
                case InputMode.ClipBoard:
                    ClipBoard(value);
                    break;
                default:
                    break;
            }
        }
        private void ClipBoard(string value)
        {
            GUI.Commons.UiCommon.SetClipBoard(value);
            Thread.Sleep(50);//设置剪切板 延时，防止操作过快 Ctrl + V 无效
            //Ctrl + V
            TypeCombine(20, VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_V);
            Thread.Sleep(50);//剪切后延迟 防止操作过快引发剪切板异常
            GUI.Commons.UiCommon.ClearClipBoard();
        }
        private bool SetText(string value)
        {
            return AutoItHelper.ControlSetText(this.Root.Handle, this._hWnd, value) == 1;
        }
        
    }
}
