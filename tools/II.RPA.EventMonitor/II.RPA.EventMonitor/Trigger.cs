using II.RPA.EventMonitor.Common;
using II.RPA.EventMonitor.Entitys;
using II.RPA.EventMonitor.JSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.EventMonitor
{
    delegate void TaskHandler(string sessionID, PostBackJson postBack);

    delegate void TriggerHandler(object sender, EventArgs e);

    delegate void ErrorHandler(object sender, TriggerErrorEventArgs e);
    delegate void MessageHandler(object sender, JMessage message);
    abstract class Trigger
    {
        /// <summary>
        /// 满足触发条件时触发的事件
        /// </summary>
        internal event TriggerHandler EventTrigger;
        /// <summary>
        ///触发器 异常 时 触发的事件
        /// </summary>
        internal event ErrorHandler ErrorEvent;
        /// <summary>
        ///触发器 产生消息 时 触发的事件
        /// </summary>
        internal event MessageHandler MessageEvent;
        internal int TriggerID { get; set; }
        internal string TriggerName { get; set; }
        internal bool HasTriggered { get; set; }
        internal eTriggerStatus Status { get; set; }
        internal abstract void StartTrigger();
        internal abstract void StopTrigger();
        /// <summary>
        /// 当触发事件执行完成时触发的事件
        /// </summary>
        internal abstract void Continue();

        internal Func<Trigger, Trigger, bool> EqualsFunc = null;
        protected virtual void TryTrigger(object sender, EventArgs e)
        {
            HasTriggered = true;
            this.Status = eTriggerStatus.Suspend;
            if (this.EventTrigger != null)
            {
                //Task.Run(() =>
                //{
                EventTrigger(sender, e);
                //});
            }
        }
        //public bool Equals(Func<Trigger, Trigger, bool> func, Trigger obj)
        //{
        //    return func(this, obj);
        //}
        public override bool Equals(object obj)
        {
            if (obj is Trigger) {
                return EqualsFunc(this, (Trigger)obj);
            }
            return base.Equals(obj);
        }

        /// <summary>
        /// 发送一条消息给客户端
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        protected virtual void SendMessage(object sender, JMessage message)
        {
            message.info = this.TriggerName;
            if (this.MessageEvent != null)
            {
                Task.Run(() =>
                {
                    MessageEvent(sender, message);
                });
            }
        }
        protected void SendMessage(eMessageStatus status, string msg = "", Exception ex = null)
        {
            JMessage message = new JMessage();
            message.status = (int)status;
            message.message = Configurations.GetErrorMessge(message.status);
            if (string.IsNullOrEmpty(msg))
            {
                msg = message.message;
            }
            Loggers.Error($"【{this.TriggerName}】{msg}：{ex}");
            SendMessage(this, message);
        }
        /// <summary>
        /// 异常 停止
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        protected virtual void ErrorAndStop(object sender, JMessage message)
        {
            this.Status = eTriggerStatus.Error;
            if (this.ErrorEvent != null)
            {
                TriggerErrorEventArgs e = new TriggerErrorEventArgs();
                e.triggerName = this.TriggerName;
                message.info = this.TriggerName;
                e.message = message;
                ErrorEvent(sender, e);
            }
        }
        /// <summary>
        /// 异常，会 打印日志，和 发送消息给客户端
        /// </summary>
        protected void ErrorAndStop(eMessageStatus status, string msg = "", Exception ex = null)
        {
            JMessage message = new JMessage();
            message.status = (int)status;
            message.message = Configurations.GetErrorMessge(message.status);
            if (string.IsNullOrEmpty(msg))
            {
                msg = message.message;
            }
            Loggers.Error($"【{this.TriggerName}】{msg}：{ex}");
            ErrorAndStop(this, message);
        }


    }
}
