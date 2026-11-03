using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.GUI.Commons.FacDialog;
using II.RPA.Plugin.GUI.Commons.Params;
using II.RPA.Plugin.GUI.Commons.Params.DataCollect;
using II.RPA.WinAutomation.CommonUtility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Commons
{
    public class FacMessageHelper
    {
        #region Factory Action Const Fields

        private const string FACTORY_ACTION_COLLECTDATA_GETCOLLECTRESULT = "collectData.getCollectResult";
        private const string FACTORY_ACTION_COLLECTDATA_GETSIMILARCOLLECTRESULT = "collectData.getSimilarCollectResult";
        private const string FACTORY_ACTION_SHOWWINDOWDIALOG = "showWindowDialog";
        private const string FACTORY_ACTION_COLLECTDATA_RECEIVEEXCEPTION = "collectData.receiveException";
        private const string FACTORY_ACTION_COLLECTDATA_RECEIVEESCCOMMAND = "collectData.receiveEscCommand";
        private const string FACTORY_ACTION_COLLECTDATA_RECEIVENODATASTATUS = "collectData.receiveNoDataStatus";
        private const string FACTORY_ACTION_COLLECTDATA_RECEIVEPAGETURNBUTTONELEMENT = "collectData.receivePageTurnButtonElement";
        private const string FACTORY_ACTION_AGENT_SETOPACITY = "Agent.setOpacity";
        private const string FACTORY_ACTION_AGENT_INSERTELEMENT = "Agent.InsertElement";
        private const string FACTORY_ACTION_AGENT_CHECKFINALSTATE = "Agent.checkFinalState";
        private const string FACTORY_ACTION_AGENT_UPDATESMALLCARDSTEPTEXT = "Agent.updateSmallCardStepText";
        private const string FACTORY_ACTION_ELEMENT_RETRIEVE = "Element.aiEnhancedLocator";

        #endregion
        private static ILogger logger;
        static FacMessageHelper()
        {
            logger = Logger.OfContext();
        }

        #region ChatRpa

        #region 通知 前端隐藏/显示 操作窗口
        /// <summary>
        /// 
        /// </summary>
        /// <param name="statusCode"> true show;false hidden</param>
        /// <param name="msg"></param>
        /// <param name="facRestart"></param>
        /// <returns></returns>
        public static string SendShowOrHiddenMainWndActionToFactory(bool show)
        {
            var param = new JObject() {
                        { "opacity", show?1:0 }
            };
            var ret = FacMessageHelper.SendMessageToFactory(FacMessageHelper.FACTORY_ACTION_AGENT_SETOPACITY, param.ToJsonNoneFormating(), 2000).Result;
            logger.LogDebug($"通知 前端隐藏/显示 操作窗口,返回值：{ret}");
            return ret;
        }
        public static string SendScreenshotCaptureResultToFactory(object result)
        {
            var ret = FacMessageHelper.SendMessageToFactory(FacMessageHelper.FACTORY_ACTION_AGENT_INSERTELEMENT, result.ToJsonNoneFormating(), 2000).Result;
            logger.LogDebug($"通知前端 截图拾取元素结果,返回值：{ret}");
            return ret;
        }
        public static string SendAgentCheckFinalStateResultToFactory()
        {
            var param = new JObject() { };
            var ret = FacMessageHelper.SendMessageToFactory(FacMessageHelper.FACTORY_ACTION_AGENT_CHECKFINALSTATE, param.ToJsonNoneFormating(), 2000).Result;
            logger.LogDebug($"通知前端 Agent执行终态检查结果,返回值：{ret}");
            return ret;
        }
        public static string SendUpdateSmallCardStepTextActionToFactory(string updateText)
        {
            var param = new JObject() {
                        { "stepText", updateText}
            };
            var ret = FacMessageHelper.SendMessageToFactory(FacMessageHelper.FACTORY_ACTION_AGENT_UPDATESMALLCARDSTEPTEXT, param.ToJsonNoneFormating(), 2000).Result;
            logger.LogDebug($"通知更新前端执行小窗小卡片步骤文本,返回值：{ret}");
            return ret;
        }
        #endregion

        #endregion



        #region 通知前端弹出对话框
        public static string ShowWindowDialog(ParamShowWindowDialogModel dialogModel)
        {
            var ret = FacMessageHelper.SendMessageToFactory(FacMessageHelper.FACTORY_ACTION_SHOWWINDOWDIALOG, dialogModel.ToJsonNoneFormating()).Result;
            logger.LogDebug($"已通知前端拾取结果：{ret}");
            return ret;
        }
        #endregion

        #region 通知前端数据采集异常
        /// <summary>
        /// 
        /// </summary>
        /// <param name="statusCode"> 0-普通轻提示  1-浏览器插件相关  2 数据采集跨页面拾取异常</param>
        /// <param name="msg"></param>
        /// <param name="facRestart"></param>
        /// <returns></returns>
        public static string SendExceptionToFactory(string statusCode, string msg, bool facRestart = true)
        {
            var param = new JObject() {
                        { "statusCode", statusCode },
                        { "isRestart", facRestart },
                        { "message", msg }
            };
            var ret = FacMessageHelper.SendMessageToFactory(FacMessageHelper.FACTORY_ACTION_COLLECTDATA_RECEIVEEXCEPTION, param.ToJsonNoneFormating(), 500).Result;
            logger.LogDebug($"通知前端数据采集异常,结果：{ret}");
            return ret;
        }
        #endregion

        #region 通知前端用户已Esc操作
        public static string SendEscCommandToFactory()
        {
            var param = new JObject()
            {
            };

            var ret = FacMessageHelper.SendMessageToFactory(FacMessageHelper.FACTORY_ACTION_COLLECTDATA_RECEIVEESCCOMMAND, param.ToJsonNoneFormating(), 500).Result;
            logger.LogDebug($"通知前端用户已Esc操作,结果：{ret}");
            return ret;
        }
        #endregion

        #region 通知前端拾取结果(普通拾取)
        public static string SendCaptureDataToFactory(RetDataCollectConfigurationModel_v2 RetCaptureModel)
        {
            var ret = FacMessageHelper.SendMessageToFactory(FacMessageHelper.FACTORY_ACTION_COLLECTDATA_GETCOLLECTRESULT, RetCaptureModel.ToJsonNoneFormating()).Result;
            logger.LogDebug($"已通知前端拾取结果：{ret}");
            return ret;
        }
        #endregion

        #region 通知前端拾取结果(同类数据拾取)
        public static string SendSimilarCaptureDataToFactory(RetDataCollectConfigurationModel_v2 RetCaptureModel)
        {
            var ret = FacMessageHelper.SendMessageToFactory(FacMessageHelper.FACTORY_ACTION_COLLECTDATA_GETSIMILARCOLLECTRESULT, RetCaptureModel.ToJsonNoneFormating()).Result;
            logger.LogDebug($"已通知前端拾取结果：{ret}");
            return ret;
        }
        #endregion

        #region 通知前端 获取预览数据的不是原采集页面
        public static string SendNotCollectionPageToFactory(string msg)
        {
            var param = new JObject() {
                        { "statusCode", "0" },
                        { "message", msg }
            };
            var ret = FacMessageHelper.SendMessageToFactory(FacMessageHelper.FACTORY_ACTION_COLLECTDATA_RECEIVENODATASTATUS, param.ToJsonNoneFormating(), 500).Result;
            logger.LogDebug($"已通知前端拾取结果：{ret}");
            return ret;
        }
        #endregion
        #region 通知前端 获取翻页按钮selector
        public static string SendPageTurnButtonElement(string selector)
        {
            var ret = FacMessageHelper.SendMessageToFactory(FacMessageHelper.FACTORY_ACTION_COLLECTDATA_RECEIVEPAGETURNBUTTONELEMENT, selector, 2000).Result;
            logger.LogDebug($"已通知前端 翻页按钮selector，返回：{ret}");
            return ret;
        }
        #endregion

        #region 发送消息到前端
        public static async Task<string> SendMessageToFactory(string action, object dataObj, int timeout)
        {
            using (CancellationTokenSource timeoutCancellationTokenSource = new CancellationTokenSource())
            {

                var sendTask = Task.Run(() =>
                {
                    return MessageService.SendMessage(TerminalType.Factory, action, dataObj);
                });
                Task completedTask = await Task.WhenAny(sendTask, Task.Delay(timeout, timeoutCancellationTokenSource.Token)).ConfigureAwait(false);

                if (completedTask == sendTask)
                {
                    timeoutCancellationTokenSource.Cancel();
                    var result = await sendTask.ConfigureAwait(false);
                    if (result != null)
                    {
                        logger.LogDebug($"SendMessageToFactory done");
                        return result.Info;
                    }
                    else
                    {
                        logger.LogError($"SendMessageToFactory fail");
                        return "";
                    }
                }
                else
                {
                    logger.LogError($"SendMessageToFactory timeout");
                    return "";
                }

            }
        }
        public static async Task<string> SendMessageToFactory(string action, object dataObj)
        {
            return await Task.Run(() =>
            {
                var ret = MessageService.SendMessage(TerminalType.Factory, action, dataObj);
                if (ret != null)
                {
                    logger.LogDebug($"SendMessageToFactory done");
                    return ret.Info;
                }
                else
                {
                    logger.LogError($"SendMessageToFactory fail");
                }
                return "";
            }).ConfigureAwait(false);
        }
        #endregion


        #region 通知前端Ai增强定位后的寻回元素结构回填
        public static string SendRetrieveElementResultToFactory(ParamAiEnhancedModel enhancedData)
        {
            var ret = FacMessageHelper.SendMessageToFactory(FacMessageHelper.FACTORY_ACTION_ELEMENT_RETRIEVE, enhancedData.ToJsonNoneFormating()).Result;
            logger.LogDebug($"通知前端Ai增强定位后的寻回元素结构回填结果：{ret}");
            return ret;
        }
        #endregion
    }
}
