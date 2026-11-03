using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.EventMonitor
{
    /// <summary>
    /// 消息状态码
    /// </summary>
    internal enum eMessageStatus
    {
        /// <summary>
        /// 成功。适用于任何不需要明确场景的通信回复
        /// </summary>
        Success = 0,
        /// <summary>
        ///        1001：开始（添加）触发器任务成功
        /// </summary>
        Success_StartTrigger = 1001,
        /// <summary>
        ///1002：关闭（移除）触发器任务成功
        /// </summary>
        Success_CloseTrigger = 1002,

        /// <summary>
        ///1003：触发器触发成功
        /// </summary>
        Success_TriggerActived = 1003,
        /// <summary>
        ///1004：校验成功
        /// </summary>
        Success_Verify = 1004,

        /// <summary>
        ///4001：开始（添加）触发器任务失败
        /// </summary>
        Error_StartTrigger = 4001,
        /// <summary>
        ///40010101：开始（添加）触发器任务失败-入参解析异常
        /// </summary>
        Error_StartTrigger_ParamsError = 40010101,
        /// <summary>
        ///4002：关闭（移除）触发器任务失败
        /// </summary>
        Error_CloseTrigger = 4002,
        /// <summary>
        ///40020101：关闭（移除）文件触发器任务失败
        /// </summary>
        Error_CloseTrigger_File = 40020101,
        /// <summary>
        ///40020201：关闭（移除）邮件触发器任务失败
        /// </summary>
        Error_CloseTrigger_Mail = 40020201,
        /// <summary>
        ///40020301：关闭（移除）按键触发器任务失败
        /// </summary>
        Error_CloseTrigger_HotKey = 40020301,
        /// <summary>
        ///40020401：关闭（移除）鼠标触发器任务失败
        /// </summary>
        Error_CloseTrigger_Mouse = 40020401,
        /// <summary>
        ///4003：触发器任务执行过程中发生的未要求明确场景的失败情况
        /// </summary>
        Error_Exception = 4003,
        /// <summary>
        ///		40030101：文件触发器执行异常-其他异常
        /// </summary>
        Error_File_Exception = 40030101,
        /// <summary>
        ///		40030102：文件触发器执行异常-入参无效
        /// </summary>
        Error_File_ParamsError = 40030102,
        /// <summary>
        ///      40030103：文件触发器执行异常-文件路径不存在
        /// </summary>
        Error_File_PathNotExists = 40030103,
        /// <summary>
        ///      40030104：文件触发器执行异常-事件处理过程中发生异常，详情查看日志
        /// </summary>
        Error_File_EventExecute = 40030104,
        /// <summary>
        ///		40030201：邮件触发器执行异常-其他异常
        /// </summary>
        Error_Mail_Exception = 40030201,
        /// <summary>
        ///		40030202：邮件触发器执行异常-入参无效
        ///	</summary>
        Error_Mail_ParamsError = 40030202,
        /// <summary>
        ///		40030203：邮件触发器执行异常-邮箱参数配置错误
        /// </summary>
        Error_Mail_ConfigError = 40030203,
        /// <summary>
        ///		40030204：邮件触发器执行异常-网络异常
        /// </summary>
        Error_Mail_Socket = 40030204,
        /// <summary>
        ///		40030301：按键触发器执行异常-其他异常
        /// </summary>
        Error_HotKey_Exception = 40030301,
        /// <summary>
        ///		40030302：按键触发器执行异常-入参无效
        /// </summary>
        Error_HotKey_ParamsError = 40030302,
        /// <summary>
        ///		40030401：鼠标触发器执行异常-其他异常
        /// </summary>
        Error_Mouse_Exception = 40030401,
        /// <summary>
        ///      40030402：鼠标触发器执行异常-入参无效
        /// </summary>
        Error_Mouse_ParamsError = 40030402,

        /// <summary>
        ///      4004：触发器配置校验失败
        /// </summary>
        Error_VerifyConfig = 4004,
        /// <summary>
        ///      40040201：触发器配置校验失败 - 邮件触发器
        /// </summary>
        Error_VerifyConfig_Mail = 40040201,
    }

    internal enum eTriggerStatus
    {
        None=0,
        Starting=1,
        Runing=2,
        Error = 3,
        /// <summary>
        /// 暂停 监控
        /// </summary>
        Suspend=4,
        Stoping =5,
        Stoped=6,
        
    }
}
