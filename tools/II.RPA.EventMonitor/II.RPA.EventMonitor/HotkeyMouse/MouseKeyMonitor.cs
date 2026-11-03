using Gma.UserActivityMonitor;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace II.RPA.EventMonitor
{
    class MouseKeyMonitor
    {
        private ConcurrentQueue<int> hookKeyCodes = null;
        private List<Keys> currentKeys = null;
        //private MouseButtons currentMouseButton = 0;
        internal bool IsMonitorHotKey { get; set; }
        internal bool IsMonitorMouse { get; set; }
        private static object hookObj = new object();
        public static event Action<List<Keys>, MouseButtons> OnProduceKeyCode = null;
        public MouseKeyMonitor()
        {
            hookKeyCodes = new ConcurrentQueue<int>();
            currentKeys = new List<Keys>();
            IsMonitorHotKey = false;
            IsMonitorMouse = false;
        }
        internal void Start(bool hasMonitorHotKey, bool hasMonitorMouse)
        {
            try
            {

                lock (hookObj)
                {
                    if (hasMonitorHotKey && !IsMonitorHotKey)//有监视键盘需求,且未监视
                    {
                        InstallHook(false);
                        StartConsumeHotKey();
                    }
                    if (hasMonitorMouse && !IsMonitorMouse)//有监视鼠标需求,且未监视
                    {
                        InstallHook(true);
                        if (!IsMonitorHotKey)//未监视键盘
                        {
                            InstallHook(false);
                            StartConsumeHotKey();
                        }
                    }

                }
            }
            catch(Exception ex)
            {
                Loggers.Debug($"监视器启动失败{ex}");
            }
        }
        internal void Stop(bool hasMonitorHotKey, bool hasMonitorMouse)
        {
            try
            {
                lock (hookObj)
                {
                    if (!hasMonitorMouse && this.IsMonitorMouse)
                    {
                        UninstallHook(true);
                    }
                    if (!hasMonitorHotKey && !hasMonitorMouse && this.IsMonitorHotKey)
                    {
                        UninstallHook(false);
                    }
                }
            }
            catch (Exception)
            {

            }
        }
        private void StartConsumeHotKey()
        {
            Thread t = new Thread(ConsumeKeyCodes);
            t.IsBackground = true;
            t.Start();
        }

        private void ConsumeKeyCodes()
        {
            try
            {
                int count = 0;
                int keyCode;
                while (IsMonitorHotKey)
                {
                    count++;
                    while (hookKeyCodes.Any())
                    {
                        try
                        {
                            MouseButtons currentMouseButton = 0;
                            count = 0;
                            if (hookKeyCodes.TryDequeue(out keyCode))
                            {
                                //MouseButtons 1048576~16777216  Keys max 262144
                                if (keyCode < 0)
                                {
                                    if (keyCode <= -1048576)//鼠标
                                    {
                                        currentMouseButton = 0;//鼠标
                                    }
                                    else
                                    {
                                        var key = GetUseKeyCode((Keys)(-1 * keyCode));
                                        currentKeys.Remove(key);
                                    }
                                }
                                else if (keyCode >= 1048576)//鼠标
                                {
                                    currentMouseButton = (MouseButtons)keyCode;
                                }
                                else if (keyCode > 0)//按键
                                {
                                    var key = GetUseKeyCode((Keys)keyCode);
                                    if (!currentKeys.Contains(key))
                                    {
                                        currentKeys.Add(key);
                                    }
                                }
                                //MainForm.PrintMessage($"【Monitor】:{string.Join(",", currentKeys)} | {currentMouseButton}");
                                if (keyCode > 0 && OnProduceKeyCode != null)
                                {
                                    List<Keys> keys = new List<Keys>(currentKeys);
                                    OnProduceKeyCode(keys, currentMouseButton);
                                }
                            }

                        }
                        catch (Exception ex)
                        {
                            MainForm.PrintMessage($"【Monitor】：按键队列处理一次异常,{ex}");
                        }
                    }
                    if (count == 20)//特殊情况存在按键未清理，如包含win+L 且时间超过2秒
                    {
                        count = 0;
                        if (currentKeys.Any())// new[] { Keys.LWin, Keys.L }.All(currentKeys.Contains))
                        {
                            currentKeys.Clear();
                        }
                    }
                    Thread.Sleep(100);//没有数据时100毫秒后 继续
                }
                //清除按键队列
                while (hookKeyCodes.TryDequeue(out keyCode))
                { }
                currentKeys.Clear();

            }
            catch
            {

            }
        }
        private void InstallHook(bool isMonitorMouse)
        {
            if (isMonitorMouse)
            {
                IsMonitorMouse = true;
                HookManager.MouseDown += HookManager_MouseDown;
                HookManager.MouseUp += HookManager_MouseUp;
            }
            else
            {
                IsMonitorHotKey = true;
                HookManager.KeyDown += HookManager_KeyDown;
                HookManager.KeyUp += HookManager_KeyUp;
            }
        }
        private void UninstallHook(bool isMonitorMouse)
        {
            if (isMonitorMouse)
            {
                IsMonitorMouse = false;
                HookManager.MouseDown -= HookManager_MouseDown;
                HookManager.MouseUp -= HookManager_MouseUp;
            }
            else
            {
                IsMonitorHotKey = false;
                HookManager.KeyDown -= HookManager_KeyDown;
                HookManager.KeyUp -= HookManager_KeyUp;
            }

        }
        private void HookManager_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                hookKeyCodes.Enqueue((int)e.KeyCode);
            }
            catch (Exception ex)
            {
                string msg = "按键触发器-执行异常-KeyDown";
            }
        }
        private void HookManager_KeyUp(object sender, KeyEventArgs e)
        {
            try
            {
                hookKeyCodes.Enqueue(-1 * (int)e.KeyCode);
            }
            catch (Exception ex)
            {
                string msg = "按键触发器-执行异常-KeyUp";
            }
        }
        private void HookManager_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                hookKeyCodes.Enqueue((int)e.Button);
            }
            catch (Exception ex)
            {
                string msg = "按键触发器-执行异常-MouseClick";
            }
        }
        private void HookManager_MouseUp(object sender, MouseEventArgs e)
        {
            try
            {
                //MainForm.PrintMessage(e.Button.ToString());
                hookKeyCodes.Enqueue(-1 * (int)e.Button);
            }
            catch (Exception ex)
            {
                string msg = "按键触发器-执行异常-MouseClick";
                //Error(eMessageStatus.Error_Mouse_Exception, msg, ex);
            }
        }
        private Keys GetUseKeyCode(Keys keyCode)
        {
            switch (keyCode)
            {
                case Keys.LMenu:
                case Keys.RMenu:
                case Keys.Menu:
                case Keys.Alt:
                    return Keys.Alt;
                case Keys.LControlKey:
                case Keys.Control:
                case Keys.ControlKey:
                case Keys.RControlKey:
                    return Keys.Control;
                case Keys.LShiftKey:
                case Keys.Shift:
                case Keys.ShiftKey:
                case Keys.RShiftKey:
                    return Keys.Shift;
                case Keys.LWin:
                case Keys.RWin:
                    return Keys.LWin;
                default:
                    return keyCode;
            }
        }
    }
}
