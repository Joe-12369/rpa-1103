using II.RPA.EventMonitor.Entitys;
using II.RPA.EventMonitor.JSON;
using System;
using System.Collections.Generic;
using System.Linq;

namespace II.RPA.EventMonitor
{
    /// <summary>
    /// 触发总线
    /// </summary>
    class TriggerBus
    {
        static Dictionary<string, TriggerTask> TriggerTaskDic = new Dictionary<string, TriggerTask>();
        static readonly object objlock = new object();
        internal static event Action<Dictionary<string, TriggerTask>> OnTaskListChanged;

        internal static string StartTask(TriggerJson jsonInput)
        {
            JMessage msg = new JMessage();
            msg.postBack = jsonInput.postBack;
            bool isNewTask = false;
            try
            {
                lock (objlock)
                {
                    if (!TriggerTaskDic.ContainsKey(jsonInput.postBack.appId))
                    {
                        isNewTask = true;
                        TriggerTask tt = new TriggerTask();
                        tt.EventTask += Tt_EventTask;
                        tt.TaskErrorEvent += Tt_TaskError;
                        TriggerTaskDic.Add(jsonInput.postBack.appId, tt);
                        if (tt.SaveTriggers(jsonInput))
                        {
                            msg.status = (int)eMessageStatus.Success_StartTrigger;
                            OnTaskListChanged?.Invoke(TriggerTaskDic);
                            Loggers.Info($"添加触发器任务成功,appID:{jsonInput.postBack.appId}");
                            TriggerServer.SendMessage(jsonInput.SessionID, msg);
                        }
                        else
                        {
                            //msg.postBack.targetTriggers.Add()
                            Loggers.Info($"添加触发器任务失败,appID:{jsonInput.postBack.appId}");
                            //msg.status = (int)eMessageStatus.Error_StartTrigger;
                            //TriggerServer.SendMessage(jsonInput.SessionID, msg);
                        }
                    }
                }
                if (!isNewTask)
                {
                    Loggers.Info($"触发器任务appID已存在，尝试关闭原任务,appID:{jsonInput.postBack.appId}");
                    CloseTrigger(jsonInput.postBack.appId, null);
                    Loggers.Info($"尝试关闭原任务完成，尝试重新开启任务");
                    StartTask(jsonInput);
                }
            }
            catch (Exception ex)
            {
                Loggers.Error($"添加触发器任务失败,appID:{jsonInput.postBack.appId},{ex}");
                msg.status = (int)eMessageStatus.Error_StartTrigger;
                TriggerServer.SendMessage(jsonInput.SessionID, msg);
            }
            return string.Empty;
        }



        #region 事件
        private static void Tt_EventTask(string sessionID, PostBackJson postBack)
        {
            JMessage msg = new JMessage();
            msg.status = (int)eMessageStatus.Success_TriggerActived;
            if (postBack != null)
            {
                msg.postBack = postBack;
            }
            TriggerServer.SendMessage(sessionID, msg);
            if (TriggerTaskDic.Values.Any(t => t.PostBack.appId == postBack.appId && !t.IsRepeat))
            {
                CloseTrigger(postBack.appId);//如果是非重复的任务，直接关闭
            }
        }
        private static void Tt_TaskError(object send, TriggerErrorEventArgs e)
        {
            if (e.message != null && e.message.postBack != null)
                CloseTrigger(e.message.postBack.appId, e.message);
        }
        #endregion
        internal static void CloseTriggerBySessionID(string sessionID)
        {
            var appIds = TriggerTaskDic.Values.Where(t => t.SessionID == sessionID).Select(t => t.PostBack.appId);
            if (appIds != null && appIds.Count() > 0)
                foreach (var appId in appIds)
                {
                    try
                    {
                        CloseTrigger(appId);
                    }
                    catch (Exception ex)
                    {
                        Loggers.Error($"根据会话关闭任务失败{ex}");
                    }
                }
        }
        internal static void CloseTrigger(string appid, JMessage errorMsg = null)
        {
            JMessage msg = new JMessage();
            string sessionId = "";
            try
            {
                lock (objlock)
                {
                    if (TriggerTaskDic.ContainsKey(appid))
                    {
                        var task = TriggerTaskDic[appid];
                        sessionId = task.SessionID;
                        if (errorMsg != null)
                        {
                            TriggerServer.SendMessage(sessionId, errorMsg);//发送错误信息给客户端
                        }
                        msg.status = (int)eMessageStatus.Success_CloseTrigger;
                        task.StopTask();//关闭触发器任务
                        if (task.PostBack != null)
                        {
                            msg.postBack = task.PostBack;
                        }
                        TriggerTaskDic.Remove(appid);
                        OnTaskListChanged?.Invoke(TriggerTaskDic);
                        TriggerServer.SendMessage(sessionId, msg);//发送关系成功的消息
                        return;
                    }
                }
                if (!string.IsNullOrEmpty(sessionId))
                {
                    msg.status = (int)eMessageStatus.Error_CloseTrigger;
                    TriggerServer.SendMessage(sessionId, msg);//sessionId 可能存空的情况
                }
                else
                {
                    Loggers.Debug("未获取到SessionID，因为此任务已经被已经关闭");
                }
            }
            catch (Exception ex)
            {
                msg = new JMessage();
                msg.status = (int)eMessageStatus.Error_CloseTrigger;
                TriggerServer.SendMessage(sessionId, msg);//sessionId 可能存空的情况
            }
        }


    }

}
