using II.RPA.EventMonitor.JSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Gma.UserActivityMonitor;
using System.Threading.Tasks;
using System.Threading;

namespace II.RPA.EventMonitor
{
    class HotkeyTrigger : Trigger, IDisposable
    {
        #region 私有字段 属性
        private string AuxiliaryKey { get; set; }
        private string Key { get; set; }
        private MouseButtons monitorMouseKeyType { get; set; }

        private List<Keys> monitorKeys = null;
        private bool IsIgnoreTime = false;
        #endregion

        #region 构造
        internal HotkeyTrigger(HotKeyTriggerJson json)
        {
            this.TriggerID = json.id;
            this.TriggerName = json.name;
            this.AuxiliaryKey = json.auxiliaryKey;
            this.Key = json.key;
        }
        #endregion
        #region 触发器提供的方法

        internal override void StartTrigger()
        {
            this.Status = eTriggerStatus.Starting;
            monitorKeys = new List<Keys>();
            bool flag = InitMonitorKeys();
            if (!flag)
                return;
            InstallHook();
            this.Status = eTriggerStatus.Runing;
        }

        internal override void StopTrigger()
        {
            this.Status = eTriggerStatus.Stoping;
            try
            {
                this.Dispose();
            }
            catch (Exception ex)
            {
                string msg = "";
                SendMessage(eMessageStatus.Error_CloseTrigger_HotKey, msg, ex);
            }
            this.Status = eTriggerStatus.Stoped;
        }

        internal override void Continue()
        {
            try
            {
                InstallHook();
                HasTriggered = false;
                this.Status = eTriggerStatus.Runing;
            }
            catch (Exception ex)
            {
                string msg = "按键触发器继续执行异常";
                ErrorAndStop(eMessageStatus.Error_File_Exception, msg, ex);
            }
        }

        public void Dispose()
        {
            UninstallHook();
            monitorKeys = null;
        }

        #endregion
        private bool InitMonitorKeys()
        {
            try
            {
                this.monitorKeys.Clear();
                string[] key = this.AuxiliaryKey.ToLower().Split(';');
                if (key.Contains("alt"))
                {
                    this.monitorKeys.Add(Keys.Alt);
                }
                if (key.Contains("ctrl"))
                {
                    this.monitorKeys.Add(Keys.Control);
                }
                if (key.Contains("shift"))
                {
                    this.monitorKeys.Add(Keys.Shift);
                }
                if (key.Contains("win"))
                {
                    this.monitorKeys.Add(Keys.LWin);
                }
                if (!string.IsNullOrEmpty(this.Key))
                {
                    //int keyVal = Convert.ToInt32(this.Key, 16);
                    //int keyVal = Convert.ToInt32(this.Key);
                    Keys useKey;
                    Enum.TryParse<Keys>(this.Key, true, out useKey);
                    this.monitorKeys.Add(useKey);
                }
                return true;
            }
            catch (Exception ex)
            {
                string msg = "按键触发器参数异常";
                ErrorAndStop(eMessageStatus.Error_HotKey_ParamsError, msg, ex);
            }
            return false; 
        }
        private void InstallHook()
        {
            MouseKeyMonitor.OnProduceKeyCode += MouseKey_OnProduceKeyCode;
        }
        private void UninstallHook()
        {
            MouseKeyMonitor.OnProduceKeyCode -= MouseKey_OnProduceKeyCode;
        }

        private void MouseKey_OnProduceKeyCode(List<Keys> currentKeys, MouseButtons currentMouseButton)
        {
            if (IsIgnoreTime)
            {
                return;
            }
            MainForm.PrintMessage($"【HT】:{string.Join(",", currentKeys)} | {currentMouseButton}");
            if (currentKeys.Count == this.monitorKeys.Count && currentKeys.All(this.monitorKeys.Contains))
            {
                this.IsIgnoreTime = true;
                MainForm.PrintMessage($"【HT】:按键触发器，触发成功");
                UninstallHook();
                this.TryTrigger(this, null);
                Task.Run(() => {
                    Thread.Sleep(200);
                    this.IsIgnoreTime = false;
                });
            }
        }
    }


}
