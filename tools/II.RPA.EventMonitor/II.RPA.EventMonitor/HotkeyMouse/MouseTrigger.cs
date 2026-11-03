using Gma.UserActivityMonitor;
using II.RPA.EventMonitor.JSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace II.RPA.EventMonitor
{
    class MouseTrigger : Trigger, IDisposable
    {

        #region 私有字段 属性

        public string AuxiliaryKey { get; set; }
        public int MouseType { get; set; }
        private MouseButtons monitorMouseKeyType = MouseButtons.Left;

        private List<Keys> monitorKeys = null;
        private bool IsIgnoreTime = false;
        #endregion

        #region 构造
        internal MouseTrigger(MouseTriggerJson json)
        {
            this.TriggerName = json.name;
            this.TriggerID = json.id;
            this.AuxiliaryKey = json.auxiliaryKey;
            this.MouseType = json.mouseType;
        }
        #endregion
        #region 触发器提供的方法

        internal override void StartTrigger()
        {
            try
            {
                this.Status = eTriggerStatus.Starting;
                monitorKeys = new List<Keys>();
                InitMonitorKeys();
                InstallHook();
                this.Status = eTriggerStatus.Runing;

            }
            catch (Exception ex)
            {
                string msg = "鼠标触发器-启动异常";
                ErrorAndStop(eMessageStatus.Error_Mouse_Exception, msg, ex);
            }
        }

        internal override void StopTrigger()
        {
            try
            {
                this.Status = eTriggerStatus.Stoping;
                this.Dispose();
                this.Status = eTriggerStatus.Stoped;
            }
            catch (Exception ex)
            {
                string msg = "邮件触发器-启动异常";
                SendMessage(eMessageStatus.Error_CloseTrigger_Mouse, msg, ex);
            }
        }

        internal override void Continue()
        {
            HasTriggered = false;
            InstallHook();
            //userPressKeys.Clear();
            this.Status = eTriggerStatus.Runing;
        }

        public void Dispose()
        {
            try
            {
                UninstallHook();
                monitorKeys = null;
            }
            catch (Exception ex)
            {
                string msg = "鼠标触发器-释放资源";
                ErrorAndStop(eMessageStatus.Error_Mail_Exception, msg, ex);
            }
        }

        #endregion
        private void InitMonitorKeys()
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
                switch (this.MouseType)
                {
                    case 1:
                        this.monitorMouseKeyType = MouseButtons.Right;
                        break;
                    case 2:
                        this.monitorMouseKeyType = MouseButtons.Middle;
                        break;
                    case 0:
                    default:
                        this.monitorMouseKeyType = MouseButtons.Left;
                        break;
                }
            }
            catch (Exception ex)
            {
                string msg = "";
                ErrorAndStop(eMessageStatus.Error_Mouse_ParamsError, msg, ex);
            }
            MainForm.PrintMessage($"【MT】:开始检测{string.Join(",", monitorKeys)}+{this.monitorMouseKeyType}");
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
            MainForm.PrintMessage($"【MT】:{string.Join(",",currentKeys)} | {currentMouseButton}");
            if (this.monitorMouseKeyType == currentMouseButton)
            {
                if (currentKeys.Count == this.monitorKeys.Count && currentKeys.All(this.monitorKeys.Contains))
                {
                    this.IsIgnoreTime = true;
                    MainForm.PrintMessage($"【MT】:鼠标触发器，触发成功,检测辅助为：{string.Join(",", monitorKeys)}");
                    UninstallHook();
                    this.TryTrigger(this, null);
                    Task.Run(()=>{
                        Thread.Sleep(200);
                        this.IsIgnoreTime = false;
                    });
                }
            }
        }
    }
}
