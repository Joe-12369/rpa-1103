using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using II.RPA.Logging;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace II.RPA.Plugin.Core
{
    public class MessageService
    {
        private static ILogger logger = Logger.OfContext();

        public static Func<TerminalType, string, object, string> SendMessageToTerminal { private get; set; } = null;
        public static Func<TerminalType, string, object, Action<object>, bool> PostMessageToTerminal { private get; set; } = null;
        public static Func<SendMessageToCoeServerAction, object, string> SendMessageToCoeServerFunc { get; set; } = null;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="terminalType"></param>
        /// <param name="action">CompileSelector、ShowSelectorEditor</param>
        /// <param name="objData"></param>
        /// <returns></returns>
        public static JMessage SendMessage(TerminalType terminalType, string action, object objData)
        {
            logger.LogDebug($"SendMessage :{terminalType},{action},{objData}");
            if (SendMessageToTerminal == null)
            {
                logger.LogDebug("SendMessageToTerminal 未注册");
                return new JMessage
                {
                    Code = 100,
                    Message = "数据发送服务未注册".I18n("MessageService_F800333F")
                };
            }
            JMessage resutlt = SendMessageToTerminal(terminalType, action, objData).ToObject<JMessage>();
            return resutlt;
        }


        public static Func<Func<string, string, string, string>, string, string, string, string> UIThreadRunAction = null;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="terminalType"></param>
        /// <param name="action">CompileSelector、ShowSelectorEditor</param>
        /// <param name="objData"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public static bool PostMessage(TerminalType terminalType, string action, object objData, Action<object> callback)
        {
            logger.LogDebug($"PostMessage :{terminalType},{action},{objData}");
            if (PostMessageToTerminal == null)
            {
                logger.LogDebug("PostMessageToTerminal 未注册");
                return false;
            }
            bool resutlt = PostMessageToTerminal(terminalType, action, objData, callback);
            return resutlt;
            //var state = new Tuple<TerminalType, object, Action<object>>(
            //    terminalType,
            //    objData,
            //    callback);
            //Task.Factory.StartNew((object param) =>
            //{
            //    logger.LogDebug($"PostMessage :{terminalType},{objData}");
            //    var (termType, data, callbackAction) = (Tuple<TerminalType, object, Action<object>>)param;
            //    JMessage result = PostMessageToTerminal(terminalType, objData).ToObject<JMessage>();
            //    callbackAction(result);
            //}, state);
            //return true;
        }

        /// <summary>
        /// 发送消息到CoreServer
        /// </summary>
        /// <param name="action"></param>
        /// <param name="objData"></param>
        /// <returns></returns>
        public static string SendMessageToCoeServer(SendMessageToCoeServerAction action, object objData)
        {
            try
            {
                logger.LogDebug($"SendMessageToCoeServer :{action.ToString()},{objData}");
                if (SendMessageToCoeServerFunc == null)
                {
                    logger.LogDebug("SendMessageToCoeServerFunc 未注册");
                    return "-1";
                }
                var resutlt = SendMessageToCoeServerFunc(action, objData);
                return resutlt;

            }
            catch (Exception ex)
            {
                logger.LogDebug($"SendMessageToCoeServer :{action.ToString()}");
                logger.LogError($"SendMessageToCoeServer Exception:{ex}");
                return string.Empty;
            }
        }
    }

    public enum SendMessageToCoeServerAction
    {
        /// <summary>
        /// 重置运行时间
        /// </summary>
        ResetPluginLoaderRunTimeout = 0
    }

    public class ResetPluginLoaderRunTimeoutPayload
    {
        [JsonProperty("runUniqueID")]
        public string RunUniqueID { get; set; } = string.Empty;

        [JsonProperty("newTimeout")]
        public long NewTimeout { get; set; } = 20000;

        [JsonProperty("timeoutMsg")]
        public string TimeoutMsg { get; set; }
    }
}
