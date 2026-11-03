using II.RPA.EventMonitor.Entitys;
using II.RPA.EventMonitor.JSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace II.RPA.EventMonitor
{
    class TriggerTask
    {
        public event TaskHandler EventTask;
        internal event ErrorHandler TaskErrorEvent;
        public PostBackJson PostBack { get; set; }

        public string SessionID { get; set; }

        //const int AND = 0;
        //const int OR = 1;
        /// <summary>
        /// 是否重复
        /// </summary>
        internal bool IsRepeat { get; set; }
        /// <summary>
        /// 一个触发任务包含了多个触发规则
        /// </summary>
        internal List<Trigger> Triggers { get; set; }
        /// <summary>
        /// 0：AND；1：OR
        /// </summary>
        private eConditionType TriggerCondition { get; set; }


        /// <summary>
        /// 保存触发器，返回该触发器的ID
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public bool SaveTriggers(TriggerJson triggers)
        {
            bool result = true;
            try
            {
                if (Triggers == null)
                {
                    Triggers = new List<Trigger>();
                }
                this.PostBack = triggers.postBack;
                this.SessionID = triggers.SessionID;
                this.IsRepeat = triggers.isRepeat == 1;
                Triggers.Clear();
                TriggerCondition = (eConditionType)triggers.condition;
                SaveSpecificTriggers(triggers);
                StartTask();
            }
            catch (Exception ex)
            {
                Loggers.Error(ex);
                result = false;
            }
            return result;
        }
        private bool SaveSpecificTriggers(TriggerJson triggers)
        {
            try
            {

                //添加文件触发器
                var fileTtiggers = from ft in triggers.fileTriggers
                                   group ft by new { ft.filePath, ft.containSubfolder, ft.fileType, ft.monitorEvent } into d
                                   select d.First();
                foreach (var fileTrigger in fileTtiggers)
                {
                    FileTrigger ft = new FileTrigger(fileTrigger);
                    ft.EventTrigger += Trigger_EventTrigger;
                    ft.ErrorEvent += Trigger_ErrorEvent;
                    ft.MessageEvent += Trigger_MessageEvent;
                    Triggers.Add(ft);
                }
            }
            catch (Exception ex)
            {
                Loggers.Error($"保存具体文件触发器实例时异常{ex}");
                return false;
            }
            try
            {

                //添加邮件触发器
                var mailTriggers = from mt in triggers.mailTriggers
                                   group mt by new
                                   {
                                       mt.interval,
                                       mt.protocol,
                                       mt.serverAddress,
                                       mt.port,
                                       mt.isSSL,
                                       mt.email,
                                       mt.password,
                                       mt.condition,
                                       mt.sendAddrContent,
                                       mt.recAddrContent,
                                       mt.themeContent,
                                       mt.bodyContent
                                   } into d
                                   select d.First();
                foreach (var mailTrigger in mailTriggers)
                {
                    MailTrigger mt = new MailTrigger(mailTrigger);
                    mt.EventTrigger += Trigger_EventTrigger;
                    mt.ErrorEvent += Trigger_ErrorEvent;
                    mt.MessageEvent += Trigger_MessageEvent;
                    Triggers.Add(mt);
                }
            }
            catch (Exception ex)
            {
                Loggers.Error($"保存具体邮件触发器实例时异常{ex}");
                return false;
            }
            try
            {
                //添加按键触发器
                var hotkeyTriggers = from ht in triggers.hotkeyTriggers
                                     group ht by new { ht.auxiliaryKey, ht.key } into d
                                     select d.First();
                foreach (var hotkeyTrigger in hotkeyTriggers)
                {
                    HotkeyTrigger ht = new HotkeyTrigger(hotkeyTrigger);
                    ht.EventTrigger += Trigger_EventTrigger;
                    ht.ErrorEvent += Trigger_ErrorEvent;
                    ht.MessageEvent += Trigger_MessageEvent;
                    Triggers.Add(ht);
                }
            }
            catch (Exception ex)
            {
                Loggers.Error($"保存具体热键触发器实例时异常{ex}");
                return false;
            }
            try
            {
                //添加鼠标触发器
                var mouseTriggers = from mt in triggers.mouseTriggers
                                    group mt by new { mt.auxiliaryKey, mt.mouseType } into d
                                    select d.First();
                foreach (var mouseTrigger in mouseTriggers)
                {
                    MouseTrigger mt = new MouseTrigger(mouseTrigger);
                    mt.EventTrigger += Trigger_EventTrigger;
                    mt.ErrorEvent += Trigger_ErrorEvent;
                    mt.MessageEvent += Trigger_MessageEvent;
                    Triggers.Add(mt);
                }

            }
            catch (Exception ex)
            {
                Loggers.Error($"保存具体鼠标触发器实例时异常{ex}");
                return false;
            }
            return true;
        }
        #region 触发器的事件方法

        private void Trigger_EventTrigger(object sender, EventArgs e)
        {
            try
            {
                if (TriggerCondition == eConditionType.AND)
                {
                    int count = Triggers.Count(t => t.HasTriggered == false);
                    if (count > 0)
                    {
                        return;
                    }
                    this.PostBack.targetTriggers = this.Triggers.Select(t => t.TriggerID).ToList();
                    if (this.IsRepeat)
                    {
                        Triggers.ForEach(t => t.Continue());
                    }
                }
                else
                {
                    var trigger = ((Trigger)sender);
                    if (trigger == null || trigger.Status == eTriggerStatus.Error)
                    {
                        return;
                    }
                    if (sender != null)
                    {
                        this.PostBack.AddTargetTriggerID(((Trigger)sender).TriggerID);
                    }
                    if (this.IsRepeat)
                    {
                        trigger.Continue();
                    }
                }
                if (EventTask != null)
                {
                    Task.Run(() =>
                    {
                        EventTask.Invoke(this.SessionID, this.PostBack);
                    });
                }
            }
            catch (Exception ex)
            {
                Loggers.Error($"触发器已触发，执行触发事件时异常{ex}");
            }
        }

        private void Trigger_ErrorEvent(object sender, TriggerErrorEventArgs e)
        {
            try
            {

                if (TaskErrorEvent != null)
                {
                    e.message.postBack = this.PostBack;
                    if (sender != null)
                    {
                        e.message.postBack.AddTargetTriggerID(((Trigger)sender).TriggerID);
                    }
                    TaskErrorEvent(sender, e);
                }

            }
            catch (Exception ex)
            {
                Loggers.Error($"触发器错误时间执行时，异常{ex}");
                throw;
            }
        }
        private void Trigger_MessageEvent(object sender, JMessage message)
        {
            try
            {
                if (message != null)
                {
                    message.postBack = this.PostBack;
                    if (sender != null)
                    {
                        message.postBack.AddTargetTriggerID(((Trigger)sender).TriggerID);
                    }
                }
                TriggerServer.SendMessage(this.SessionID, message);


            }
            catch (Exception ex)
            {
                Loggers.Error($"触发器 执行发送消息，异常{ex}");
                throw;
            }
        }
        #endregion
        private void StartTask()
        {
            if (Triggers != null)
            {
                foreach (var chd in Triggers)
                {
                    try
                    {
                        //Task.Run(() =>
                        //{
                        chd.StartTrigger();
                        if (chd.Status != eTriggerStatus.Runing)
                        {
                            return;
                        }
                        //});
                    }
                    catch (Exception ex)
                    {
                        Loggers.Error($"触发器启动失败：{chd.TriggerName}，{ex}");
                        //JMessage msg = new JMessage();
                        //msg.status = (int)eMessageStatus.Error_StartTrigger;
                        //msg.postBack.targetTriggers.Add(chd.TriggerID);
                        //TriggerServer.SendMessage(this.SessionID, msg);
                    }
                }
            }

        }
        internal void StopTask()
        {
            if (Triggers != null)
            {
                foreach (var chd in Triggers)
                {
                    try
                    {
                        if (chd.Status != eTriggerStatus.None && chd.Status != eTriggerStatus.Stoping && chd.Status != eTriggerStatus.Stoped)
                        {
                            chd.StopTrigger();
                            this.PostBack.AddTargetTriggerID(chd.TriggerID);
                        }
                    }
                    catch (Exception ex)
                    {
                        Loggers.Error($"触发器停止失败:{chd.TriggerName}，{ex}");
                    }
                }
            }
        }

    }
}
