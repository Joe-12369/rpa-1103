using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using II.RPA.Logging;
using II.RPA.Plugin.Core;
using II.RPA.Plugin.Core.Commons;
using II.RPA.Plugin.GUI.Commons.Params;
using II.RPA.Plugin.GUI.Commons.Params.DataCollect;
using II.RPA.WebBrowser.Common;
using II.RPA.WebBrowser.Common.Basic.Params;
using II.RPA.WebBrowser.Common.Basic.connection;
using II.RPA.WebBrowser.Common.Basic.connection.ClientTransfer;
using II.RPA.WinAutomation.CommonUtility;
using II.RPA.WinAutomation.CommonUtility.Utils;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace II.RPA.WebBrowser.Common.Basic
{
    public class WebDataProvider
    {
        #region  基础属性和构造
        private static ILogger logger = Logger.OfContext();
        protected Func<IBrowserConnectionNew> InitConnectionFunc;
        private bool IsInitialScripe = false;
        /// <summary>
        /// 浏览器连接，与浏览器通信
        /// </summary>
        public IBrowserConnectionNew Connection { get; set; }
        private ConnectionConfig ConnectionConfig { get; set; }
        private eGuiType GuiType { get; set; }
        public int Timeout { get; set; } = 5000;
        public WebDataProvider(ConnectionConfig connectionConfig)
        {
            this.ConnectionConfig = connectionConfig;
            this.GuiType = connectionConfig.GuiType;
        }

        #endregion

        #region 初始化 连接、脚本
        private void InitConnection(int connectTimeout = 10000, bool ignoreConnection = false)
        {
            if (Connection != null && !Connection.IsConnected)
            {
                var invalidConnection = Connection;
                invalidConnection.Stop();
                if (ReferenceEquals(Connection, invalidConnection))
                {
                    Connection = null;
                }
            }

            if (Connection == null)
            {
                TimeTrackHelper timeTrackHelper = new TimeTrackHelper();
                ConnectionConfig.ConnectTimeout = connectTimeout;
                ConnectionConfig.IgnoreConnection = ignoreConnection;
                var connectionLog = new
                {
                    ConnectionConfig.GuiType,
                    ConnectionConfig.NativeMessageHostProcessName,
                    ConnectionConfig.NativeMessageHostProcessID,
                    ConnectionConfig.BrowserProcessName,
                    ConnectionConfig.ServiceKey,
                    ConnectionConfig.SignalRUrl,
                    ServiceConnectionType = ConnectionConfig.ServiceConnectionType?.FullName,
                    NamePipeConnectionType = ConnectionConfig.NamePipeConnectionType?.FullName,
                    SignalRConnectionType = ConnectionConfig.SignalRConnectionType?.FullName,
                    HasCheckBrowserExtensions = ConnectionConfig.CheckBrowserExtensions != null,
                    HasDetectionMessageProcess = ConnectionConfig.DetectionMessageProcess != null,
                    ConnectionConfig.ConnectTimeout,
                    ConnectionConfig.IgnoreConnection
                };
                timeTrackHelper.Start($"WebData InitConnection 参数:{connectionLog.ToJson()}", TimeTrackLogLevel.info);
                Connection = InitConnectionFunc != null
                    ? InitConnectionFunc()
                    : ConnectionFactory.GetConnection(ConnectionConfig);
                timeTrackHelper.Pick("WebData InitConnection", TimeTrackLogLevel.info);
                timeTrackHelper.Stop("WebData InitConnection", TimeTrackLogLevel.info);
            }
        }

        protected virtual IBrowserConnectionNew AcquireScriptRecoveryConnection(
            int remainingMilliseconds,
            bool ignoreConnection)
        {
            InitConnection(remainingMilliseconds, ignoreConnection);
            return Connection;
        }

        protected virtual void WriteScriptInitializationDiagnostic(string message)
        {
            logger.LogInformation(message);
        }

        private void EmitScriptInitializationDiagnostic(
            string eventName,
            CrxScriptInitializationResult result,
            int attempt,
            int recoveryCount,
            long elapsedMilliseconds)
        {
            try
            {
                int? errorCode = result?.Response?.Result?.Error?.Code;
                WriteScriptInitializationDiagnostic(
                    $"[CRX.ScriptInitialization.{eventName}] GuiType={GuiType}"
                    + $" Stage={result?.Stage ?? CrxScriptInitializationStage.None}"
                    + $" Path={result?.Path ?? CrxScriptInitializationPath.None}"
                    + $" Attempt={attempt} RecoveryCount={recoveryCount}"
                    + $" ElapsedMs={elapsedMilliseconds}"
                    + $" Result={result?.Completion.ToString() ?? "Running"}"
                    + (errorCode.HasValue ? $" ErrorCode={errorCode.Value}" : string.Empty)
                    + (result?.Exception != null
                        ? $" ExceptionType={result.Exception.GetType().Name}"
                        : string.Empty));
            }
            catch
            {
                // Diagnostics must never alter initialization behavior.
            }
        }

        internal CrxScriptInitializationResult InitializeScriptsWithRecovery(bool ignoreConnection)
        {
            const int recoveryBudgetMilliseconds = 10000;
            var stopwatch = Stopwatch.StartNew();
            var initializer = new CrxScriptInitializer();
            var failedConnection = Connection;
            EmitScriptInitializationDiagnostic("Started", null, 1, 0, 0);
            CrxScriptInitializationResult result = initializer.Initialize(
                failedConnection,
                GuiType,
                1);
            if (result.Completion != CrxScriptInitializationCompletion.ConnectionFailed)
            {
                EmitScriptInitializationDiagnostic(
                    result.Completion == CrxScriptInitializationCompletion.Success
                        ? "Completed"
                        : "StageFailed",
                    result,
                    1,
                    0,
                    stopwatch.ElapsedMilliseconds);
                return result;
            }

            EmitScriptInitializationDiagnostic(
                "StageFailed", result, 1, 0, stopwatch.ElapsedMilliseconds);

            failedConnection?.Stop();
            if (ReferenceEquals(Connection, failedConnection))
            {
                Connection = null;
            }

            int remainingMilliseconds = recoveryBudgetMilliseconds
                - (int)Math.Min(stopwatch.ElapsedMilliseconds, recoveryBudgetMilliseconds);
            if (remainingMilliseconds <= 0)
            {
                EmitScriptInitializationDiagnostic(
                    "RecoveryExhausted", result, 1, 0, stopwatch.ElapsedMilliseconds);
                return result;
            }

            IBrowserConnectionNew replacement = Connection;
            if (replacement == null || ReferenceEquals(replacement, failedConnection))
            {
                replacement = AcquireScriptRecoveryConnection(
                    remainingMilliseconds,
                    ignoreConnection);
            }
            if (replacement == null || ReferenceEquals(replacement, failedConnection))
            {
                EmitScriptInitializationDiagnostic(
                    "RecoveryExhausted", result, 1, 0, stopwatch.ElapsedMilliseconds);
                return result;
            }

            Connection = replacement;
            EmitScriptInitializationDiagnostic(
                "Recovering", result, 1, 1, stopwatch.ElapsedMilliseconds);
            EmitScriptInitializationDiagnostic(
                "Started", null, 2, 1, stopwatch.ElapsedMilliseconds);
            CrxScriptInitializationResult replacementResult =
                initializer.Initialize(replacement, GuiType, 2);
            EmitScriptInitializationDiagnostic(
                replacementResult.Completion == CrxScriptInitializationCompletion.Success
                    ? "Completed"
                    : replacementResult.Completion == CrxScriptInitializationCompletion.ConnectionFailed
                        ? "RecoveryExhausted"
                        : "StageFailed",
                replacementResult,
                2,
                1,
                stopwatch.ElapsedMilliseconds);
            return replacementResult;
        }

        public void InitialScripe()
        {
            try
            {
                CrxScriptInitializationResult result = InitializeScriptsWithRecovery(false);
                if (result.Completion != CrxScriptInitializationCompletion.Success)
                {
                    logger.LogError(
                        $"InitialScripe Failed:Stage={result.Stage},Exception={result.Exception?.Message}");
                }
            }
            catch (Exception exception)
            {
                logger.LogError(
                    $"InitialScripe Failed:Stage=Recovery,ExceptionType={exception.GetType().Name}");
            }
        }

        /// <summary>
        /// 保存每个eGuiType的 Js初始化状态
        /// </summary>
        public static ConcurrentDictionary<eGuiType, bool> GuiTypeScripeStateDics = new ConcurrentDictionary<eGuiType, bool>();


        /// <summary>
        /// 根据元素类型通过连接向目标注入Js Script injection
        /// </summary>
        /// <param name="_Connection">目标连接</param>
        /// <param name="_GuiType">浏览器类型</param>
        /// <param name="_IsPreInjection">是否为前置注入（插件加载后立刻执行）</param>
        /// <returns></returns>
        public static bool InjectScript(InjectScriptParam injectScriptParam)
        {
            try
            {
                if (injectScriptParam == null)
                {
                    return false;
                }

                CrxScriptInitializationResult result = new CrxScriptInitializer().Initialize(
                    injectScriptParam.Connection,
                    injectScriptParam.GuiType,
                    1);
                bool succeeded = result.Completion == CrxScriptInitializationCompletion.Success;
                injectScriptParam.IsInitial = succeeded;
                if (!succeeded)
                {
                    injectScriptParam.ErrMessage = Localize(
                        $"脚本初始化发生异常,阶段:{result.Stage},异常:{result.Exception?.Message}",
                        "WebDataProvider_1A0F8F06");
                    if (injectScriptParam.IsPreInjection)
                    {
                        GuiTypeScripeStateDics[injectScriptParam.GuiType] = false;
                    }
                }
                return succeeded;
            }
            catch (Exception ex)
            {
                if (injectScriptParam != null)
                {
                    injectScriptParam.IsInitial = false;
                    injectScriptParam.ErrMessage = Localize(
                        $"脚本初始化发生异常,脚本:{ex}",
                        "WebDataProvider_1A0F8F06");
                    if (injectScriptParam.IsPreInjection)
                    {
                        GuiTypeScripeStateDics[injectScriptParam.GuiType] = false;
                    }
                }
                return false;
            }
        }

        #endregion


        #region 获取版本
        public string GetConnCrxVersion()
        {
            try
            {
                if (this.Connection?.CrxVersion != null)
                {
                    return this.Connection?.CrxVersion;
                }
                InitConnection();
                if (this.Connection == null)
                {
                    return string.Empty;
                }
                var isConnected = this.Connection.TestConnection(this.GuiType, 300);
                if (!isConnected)
                {
                    return string.Empty;
                }
                //获取浏览器插件版本号，DLL从6.7.0版本开始、浏览器扩展程序2.7版本起有此数据
                var restule = this.Connection.Send(this.GuiType, "getVersion", new JObject(), 1000);
                if (restule?.Result.Error != null)
                {
                    logger.LogDebug($"未能获取到浏览器插件版本，可能是老插件");
                    this.Connection.CrxVersion = null;
                }
                else
                {
                    this.Connection.CrxVersion = restule?.Result.Content?.ToString();
                    logger.LogDebug($"浏览器插件版本：{this.Connection.CrxVersion}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"获取浏览器插件版本异常,{ex}");
            }
            return this.Connection?.CrxVersion ?? string.Empty;
        }
        #endregion

        #region   消息通信 Wrapper


        /*send异常后的异常状态 后补逻辑未实现  ，根据发送消息返回 连接异常的特定状态，stop*/

        private static readonly object objSendLock = new object();
        private static readonly HashSet<string> ThrowTimeoutCommands =
            new HashSet<string>(StringComparer.Ordinal)
            {
                "elements.buildSelector",
                "elements.buildCssSelector"
            };
        private static readonly HashSet<string> ConnectionClosedTolerantCommands =
            new HashSet<string>(StringComparer.Ordinal)
            {
                        "tabs.close",
                        "tabs.closeList",
                        "tabs.closeAll",
                        "tabs.closeOther",
                        "tabs.closeRight"
            };
        protected PipeResponse SendRequest(string command, JObject @params, int timeout = 30000, bool ignoreConnection = false)
        {
            lock (objSendLock)
            {
                InitConnection(timeout, ignoreConnection);
                if (Connection == null)
                {
                    logger.LogError("[SendRequest] Connection is Null");
                    return null;
                }

                var requestConnection = Connection;
                try
                {
                    var result = requestConnection.Send(this.GuiType, command, @params, timeout);
                    if (result != null && result.Code == 200)
                    {
                        return result;
                    }
                    if (result == null)
                    {
                        return null;
                    }
                    Debug.Assert(result.Code == 200);
                    logger.LogDebug($"脚本或连接发生异常，需要具体定位.{command}:{@params.ToJson()}");
                    logger.LogDebug($"脚本或连接发生异常，需要具体定位.{result}");
                    return result;
                }
                catch (BrowserConnectionException ex)
                {
                    if (ex.Reason == ClientTransferCompletionReason.Timeout)
                    {
                        if (!IsConnectionHealthy(requestConnection))
                        {
                            requestConnection?.Stop();
                            if (ReferenceEquals(Connection, requestConnection))
                            {
                                Connection = null;
                            }
                        }

                        if (ThrowTimeoutCommands.Contains(command))
                        {
                            throw new TimeoutException(
                                $"Browser command timed out: {command}",
                                ex);
                        }

                        return null;
                    }

                    if (ShouldInvalidateConnection(ex.Reason))
                    {
                        requestConnection?.Stop();
                        if (ReferenceEquals(Connection, requestConnection))
                        {
                            Connection = null;
                        }
                    }
                    if (ex.Reason == ClientTransferCompletionReason.ConnectionClosed && ConnectionClosedTolerantCommands.Contains(command))
                    {
                        Microsoft.Extensions.Logging.LoggingExtensions.LogWarning(logger, ex, $"[SendRequest] {command} 执行期间浏览器连接已断开（关闭窗口/浏览器导致），按操作成功处理");
                        return new PipeResponse() { Code = 200, Result = new PipeResult { Error = null } };
                    }

                    throw ex.ToRpaException(this.GuiType);
                }
                catch
                {
                    requestConnection?.Stop();
                    if (ReferenceEquals(Connection, requestConnection))
                    {
                        Connection = null;
                    }
                    throw;
                }
            }
        }
        public virtual JToken Send(string command, JObject @params, int timeout = 30000, bool ignoreConnection = false)
        {
            //Debug.Assert(!@params.ContainsKey("codeId"), "codeId具有特殊用途，请你换个参数名吧");
            //@params.Add("codeId", ExtensionProvider.Instance.CodeId);
            if (this.Connection != null && !this.Connection.CrxIsLatestOrNewer)
            {
                throw new RPAException(this.GuiType, eErrorMessageType.Error_BrowserPluginVersionTooLow);
            }
            return Send(command, @params, true, timeout, ignoreConnection);
        }

        private static bool ShouldInvalidateConnection(
            ClientTransferCompletionReason reason)
        {
            return reason == ClientTransferCompletionReason.ConnectionClosed
                || reason == ClientTransferCompletionReason.SendFailed
                || reason == ClientTransferCompletionReason.ProtocolError;
        }

        private static bool IsConnectionHealthy(
            IBrowserConnectionNew connection)
        {
            return connection != null && connection.IsConnected;
        }
        private JToken Send(string command, JObject @params, bool isNeedInitialScripe, int timeout = 30000, bool ignoreConnection = false)
        {
            var response = this.SendRequest(command, @params, timeout, ignoreConnection);
            if (response != null && response.Result.Error != null)
            {
                logger.LogDebug($"浏览器返回Error,connection type {this.Connection?.GetType().Name}");
                logger.LogDebug($"请求参数{command}：{@params.ToJson()}");
                logger.LogDebug($"响应Error信息：{response.Result.Error.ToJson()}");

                eErrorMessageType errType;
                if (response.Result.Error.Code == -100099 || response.Result.Error.Code == -1000100)
                {
                    //首次send初始化
                    if (isNeedInitialScripe)
                    {
                        CrxScriptInitializationResult initialization =
                            InitializeScriptsWithRecovery(ignoreConnection);
                        if (initialization.Completion == CrxScriptInitializationCompletion.Success)
                        {
                            return Send(command, @params, false, timeout, ignoreConnection);
                        }

                        if (initialization.Completion == CrxScriptInitializationCompletion.ConnectionFailed
                            && initialization.Exception != null)
                        {
                            if (initialization.Exception is BrowserConnectionException connectionException)
                            {
                                throw connectionException.ToRpaException(this.GuiType);
                            }

                            throw initialization.Exception;
                        }

                        int? initializationErrorCode =
                            initialization.Response?.Result?.Error?.Code;
                        throw new RPAException(
                            this.GuiType,
                            eErrorMessageType.Error_CrxScriptInitializationError,
                            new Exception(
                                $"CRX script initialization failed at {initialization.Stage}"
                                + (initializationErrorCode.HasValue
                                    ? $" with error {initializationErrorCode.Value}"
                                    : string.Empty),
                                initialization.Exception));
                    }
                    logger.LogDebug($"浏览器返回Error,connection type response.Result.Error.Code");
                    // 忽略无资源异常
                    return null;
                }
                else if (response.Result.Error.Code == 2)
                {
                    errType = eErrorMessageType.Error_ExtensionpPermissionError;
                }
                else if (response.Result.Error.Code == 3)
                {
                    // 忽略无资源异常
                    return null;
                }
                else
                {
                    //针对特殊其他浏览器使用IE模式操作异常 
                    // 后续有其他相似场景 再做封装
                    string errorMessage = "This is not supported on tabs in Internet Explorer Mode.";
                    string regexPattern = @"(?<=: )(.+)";
                    var matchValue = Regex.Match(response.Result.Error.Message, regexPattern);
                    if (response.Result.Error.Code == -1 && matchValue.Value.Contains(errorMessage))
                        errType = eErrorMessageType.Error_NotSupportIEMode;
                    else if (response.Result.Error.Code != -1)
                        errType = eErrorMessageType.Error_ElementNotOperation;
                    else
                        errType = eErrorMessageType.Error_BrowserExtensionCommandError;
                }
                throw new RPAException(errType, new Exception($"Extension error [{response.Result.Error.Code}]: {response.Result.Error.Message}"));
            }
            return response?.Result?.Content;
        }

        #endregion

        #region 浏览器操作 相关
        public void ActiveWindow(int windowId)
        {
            this.Send("windows.active",
                 new JObject {
                    { "windowId" ,windowId  }
                 });
        }
        public JToken GetNormalWindowAll()
        {
            JArray windowTypes = new JArray("normal");
            return this.Send("windows.getAll",
                new JObject {
                    { "windowTypes" ,windowTypes  }
                });
        }
        public void CloseAll(int timeout, bool ignoreConnection, bool isCloseAlert)
        {
            this.Send("tabs.closeAll", new JObject { { "isCloseAlert", isCloseAlert } }, timeout, ignoreConnection);
        }
        public void Minimized()
        {
            this.Send("windows.minimized", new JObject { });
        }
        public void Maximized(int windowId)
        {
            this.Send("windows.maximize", new JObject { { "windowId", windowId } });
        }
        public JToken Create(ParamCreateTabModel paramModel)
        {
            return this.Send("tabs.create", new JObject { { "url", paramModel.url } }, paramModel.Timeout);
        }
        public JToken GetCookies(ParamCookieModel paramModel)
        {
            return this.Send("cookies.getAll",
                new JObject {
                    { "url", paramModel.url },
                    { "name", paramModel.name },
                    { "domain", paramModel.domain }
                }, paramModel.Timeout);
        }
        public void SetCookie(ParamCookieModel paramModel)
        {
            var jObj = (JObject)JsonHelper.JsonDesByNewtonJson(paramModel.value);
            jObj.Add("url", paramModel.url);
            this.Send("cookies.set", jObj, paramModel.Timeout);
        }
        public void RemoveCookie(ParamCookieModel paramModel)
        {
            this.Send("cookies.remove",
                new JObject {
                    { "url", paramModel.url },
                    { "name", paramModel.name }
                }, paramModel.Timeout);
        }

        #endregion

        #region 获取tab信息 相关 
        public JToken CreateTabInWindow(ParamCreateTabModel param)
        {
            var jObj = new JObject {
                    { "url", param.url}
                };
            if (param.windowId > 0)
                jObj.Add("windowId", param.windowId);
            return this.Send("tabs.create", jObj, param.Timeout);
        }
        public virtual JToken GetIntoViewPortRect(ParamModel paramModel)
        {
            return this.Send("tabs.getIntoViewPortRect",
                new JObject {
                    { "tabId", paramModel.TabId }
                }, paramModel.Timeout);
        }
        public JToken GetTabDetail(ParamModel paramModel)
        {
            return this.Send("tabs.getDetail",
                new JObject {
                    { "tabId", paramModel.TabId }
                }, paramModel.Timeout);
        }
        public JToken GetAllTabIds(ParamGetTabIdsModel paramModel)
        {
            return this.GetTabIds(paramModel);
        }
        public JToken NormalTabIds(ParamGetTabIdsModel paramModel)
        {
            paramModel.queryObject.Add("windowType", "normal");
            return this.GetTabIds(paramModel);
        }
        public JToken GetActiveTabIds(ParamGetTabIdsModel paramModel)
        {
            paramModel.queryObject.Add("active", true);
            return this.GetTabIds(paramModel);
        }
        public JToken GetTabIds(ParamGetTabIdsModel paramModel)
        {
            if (paramModel.WndHandles != null && this.GuiType == eGuiType.IE)
            {
                paramModel.queryObject.Add("wndHandles", JArray.FromObject(paramModel.WndHandles));
            }
            if (paramModel.Original)
            {
                paramModel.queryObject.Add("original", true);
            }
            //关联窗口需求，可以通过指定WindowId体现，目前性能差异不大，暂时不处理。
            //if (paramModel.WindowId > 0)
            //{
            //    paramModel.queryObject.Add("windowId ", paramModel.WindowId);
            //}
            return this.Send("tabs.query", paramModel.queryObject, paramModel.Timeout);
        }
        public int GetTotalHeight(ParamModel paramModel)
        {

            var paramExecuteScriptOnFrameModel = new ParamExecuteScriptOnFrameModel
            {
                TabId = paramModel.TabId,
                code = "document.body.scrollHeight",
                RunParam = new Plugin.Core.Entities.RunParamModel
                {
                    TotalTime = paramModel.Timeout
                }
            };
            return this.ExecuteScriptOnFrame(paramExecuteScriptOnFrameModel).Value<int>();
        }
        public int GetTotalWidth(ParamModel paramModel)
        {
            var paramExecuteScriptOnFrameModel = new ParamExecuteScriptOnFrameModel
            {
                TabId = paramModel.TabId,
                code = "document.body.scrollWidth",
                RunParam = new Plugin.Core.Entities.RunParamModel
                {
                    TotalTime = paramModel.Timeout
                }
            };
            return this.ExecuteScriptOnFrame(paramExecuteScriptOnFrameModel).Value<int>();
        }
        public JToken ExecuteScriptOnFrame(ParamExecuteScriptOnFrameModel paramModel)
        {

            return this.Send("tabs.executeScriptOnFrame",
                new JObject {
                    { "tabId", paramModel.TabId },
                    { "code",paramModel.code }
                }, paramModel.Timeout);
        }
        #endregion

        #region tabpage操作 相关
        public string Executejs(ParamExecuteJavaScriptModel paramModel)
        {
            int timeOut = 30 * 60 * 1000;
            //运行js
            var result = this.Send("elements.executeScript",
                new JObject {
                    { "tabId", paramModel.TabId },
                    { "code", paramModel.scriptCode },
                    { "elementId", paramModel.ElementId },
                    { "world", paramModel.ExecutionWorld }
                }, timeOut);
            string value = result.ToString();

            if (value.StartsWith("\"") && value.EndsWith("\""))
            {
                return value.Substring(1, value.Length - 2).Replace("\\\"", "\"");
            }
            return value;
        }
        public void ActiveTab(ParamModel paramModel)
        {
            var retJtoken = this.Send("tabs.active", new JObject { { "tabId", paramModel.TabId } }, paramModel.Timeout);
            Thread.Sleep(200);
            //return Convert.ToBoolean(retJtoken);

        }
        public void SwitchTab(ParamModel paramModel)
        {
            var retJtoken = this.Send("tabs.switch", new JObject { { "tabId", paramModel.TabId } }, paramModel.Timeout);
            Thread.Sleep(200);
        }
        public void CloseRight(ParamModel paramModel)
        {
            this.Send("tabs.closeRight", new JObject {
                { "tabId",paramModel.TabId },
                { "isCloseAlert",paramModel.IsCloseAlert }
            }, paramModel.Timeout);
        }
        public void CloseOther(ParamModel paramModel)
        {
            this.Send("tabs.closeOther", new JObject {
                { "tabId",paramModel.TabId },
                { "isCloseAlert",paramModel.IsCloseAlert }
            }, paramModel.Timeout);
        }
        public void CloseList(ParamMultipleCloseModel paramModel)
        {
            this.Send("tabs.closeList", new JObject {
                { "tabIds", JArray.FromObject(paramModel.TabIds)  },
                { "isCloseAlert",paramModel.IsCloseAlert }
            }, paramModel.Timeout);
        }
        public void Close(ParamModel paramModel)
        {
            this.Send("tabs.close", new JObject {
                { "tabId",paramModel.TabId },
                { "isCloseAlert",paramModel.IsCloseAlert }
            }, paramModel.Timeout);
        }
        public JToken Scroll(ParamScrollModel paramModel)
        {
            return this.Send("tabs.scroll",
                new JObject {
                    { "tabId", paramModel.TabId },
                    { "direction", paramModel.direction.ToString().ToLower() },
                    { "value", paramModel.value }
                }, paramModel.Timeout);
        }
        public JToken Scroll_V2(ParamScrollModel paramModel)
        {
            return this.Send("tabs.scroll_V2",
                new JObject {
                    { "tabId", paramModel.TabId },
                    { "direction", paramModel.direction.ToString().ToLower() },
                    { "elementId", paramModel.elementId },
                    { "targetElementId", paramModel.targetElementId },
                    { "scrollType", paramModel.scrollType },
                    { "offSetX", paramModel.offSetX },
                    { "offSetY", paramModel.offSetY }
                }, paramModel.Timeout);
        }
        public JToken GetBodyScroll(ParamScrollModel paramModel)
        {
            return this.Send("tabs.getScroll",
                new JObject {
                    { "tabId", paramModel.TabId },
                    { "direction", paramModel.direction.ToString().ToLower() },
                    { "elementId", paramModel.elementId }
               }, paramModel.Timeout);
        }
        public void ScrollTo(ParamScrollToModel paramModel)
        {
            var @params = new JObject {
                { "tabId",paramModel.TabId },
                { "top",paramModel.top },
                { "left",paramModel.left },
                { "behavior", paramModel.behavior }
            };
            this.Send("tabs.scrollTo", @params, paramModel.Timeout);
        }
        public JToken GetDocument(ParamModel paramModel)
        {
            return this.Send("tabs.getDocument",
                new JObject {
                    { "tabId", paramModel. TabId }
                }, paramModel.Timeout);
        }
        public void GoBack(ParamModel paramModel)
        {
            this.Send("tabs.goBack",
                new JObject {
                     { "tabId", paramModel. TabId }
                }, paramModel.Timeout);
        }
        public void GoForward(ParamModel paramModel)
        {
            this.Send("tabs.goForward",
                new JObject {
                      { "tabId", paramModel. TabId }
                }, paramModel.Timeout);
        }
        public void Navigate(ParamNavigateModel paramModel)
        {
            this.Send("tabs.navigate",
                new JObject {
                    { "tabId",paramModel. TabId },
                    { "url", paramModel.url }
                }, paramModel.Timeout);
        }
        public void Reload(ParamModel paramModel)
        {
            this.Send("tabs.reload",
                new JObject {
                    { "tabId", paramModel.TabId },
                    { "bypassCache", false }
                }, paramModel.Timeout);
        }
        public JToken StopLoad(ParamModel paramModel)
        {
            return this.Send("tabs.stop", new JObject {
                { "tabId",  paramModel.TabId },
            }, paramModel.Timeout);
        }

        #region 数据采集
        public JToken QueryCollectionData(ParamQueryCollectionDataModel paramModel)
        {
            return this.Send("tabs.queryCollectionData",
                 new JObject {
                    { "tabId", paramModel.TabId },
                    { "parentSelector", JArray.Parse(paramModel.parentSelector) },
                    { "selectorDataList", JArray.Parse(paramModel.selectorDataList) }
                 }, paramModel.Timeout);
        }
        public JToken QueryCollectionData(int tabId, CollectionConfig collectionConfig, ColorGuidanceConfig colorGuidanceConfig)
        {
            int timeout = 60 * 1000;
            if (collectionConfig.ScrollVerticalDelay > 0)
            {
                timeout += collectionConfig.ScrollVerticalDelay;
            }

            return this.Send("tabs.queryCollectionDataV2",
                 new JObject {
                    { "tabId", tabId },
                    { "collectionConfig",collectionConfig==null?null:JObject.FromObject( collectionConfig ) },
                    { "colorGuidanceConfig",colorGuidanceConfig==null?null:JObject.FromObject( colorGuidanceConfig )}
                 },
                 timeout);
        }

        public JToken ScrollAndCollectionData(int tabId, CollectionConfig collectionConfig, ColorGuidanceConfig colorGuidanceConfig, string markedElementId)
        {
            int timeout = 20 * 1000;
            if (collectionConfig.ScrollVerticalDelay > 0)
            {
                timeout += collectionConfig.ScrollVerticalDelay;
            }

            return this.Send("tabs.scrollAndCollectionData",
                 new JObject {
                    { "tabId", tabId },
                    { "markedElementId", markedElementId },
                    { "collectionConfig",collectionConfig==null?null:JObject.FromObject( collectionConfig ) },
                    { "colorGuidanceConfig",colorGuidanceConfig==null?null:JObject.FromObject( colorGuidanceConfig )}
                 },
                 timeout);
        }
        #endregion

        #endregion

        #region 构建selector 相关
        public JToken BuildSelector(ParamBuildSelectorModel paramModel)
        {
            return this.Send("elements.buildSelector",
                new JObject { { "tabId",paramModel.TabId },
                    { "elementId", paramModel.ElementId },
                    { "innerTextFirst",paramModel.innerTextFirst },
                    { "rawData", paramModel.rawData },
                    { "excludeInnerText",paramModel.excludeInnerText },
                    { "excludeHiddenEle",paramModel.excludeHiddenEle },
                    {"validNodeAttributes",paramModel.ValidNodeAttributes!=null?JArray.FromObject( paramModel.ValidNodeAttributes):null}
                }, paramModel.Timeout);
        }
        public JToken BuildCssSelector(ParamModel paramModel)
        {
            return this.Send("elements.buildCssSelector", new JObject {
                { "tabId", paramModel.TabId },
                { "elementId",paramModel.ElementId }
            }, paramModel.Timeout);
        }

        public JToken TabCapture(ParamModel paramModel)
        {
            return this.Send("tabs.captureVisibleTab",
                new JObject {
                { "tabId", paramModel.TabId },
                { "quality", 100 },
                { "format", "png" }
                }, paramModel.Timeout);

        }
        public JToken TabCaptureFull(int tabId, JObject rect = null, int timeout = 5000)
        {
            var @params = new JObject {
                { "tabId", tabId },
                { "format", "png" },
                { "quality", 100 }
            };
            if (rect != null)
            {
                @params.Add("rect", rect);
            }
            return this.Send("tabs.captureTab", @params, timeout);
        }
        #endregion

        #region 根据selector匹配元素相关 
        public string QuerySelector(ParamQuerySelectorModel param)
        {
            return this.Send("tabs.querySelector",
                new JObject {
                    { "tabId",param.TabId },
                    { "selector", JArray.Parse(param.selector) },
                    { "mode", param.mode },
                    { "cssSelector", param.cssSelector },
                    { "index",param.index },
                    { "version",param.version },
                    { "discernHidden",param.discernHidden }
                }, param.Timeout).ToString();
        }
        public string QuerySelectorByXpathAll(ParamQuerySelectorModel param)
        {
            return this.Send("tabs.querySelectorByXpathAll",
                new JObject {
                    { "tabId",param.TabId },
                    { "xpathSelector", param.cssSelector },
                    { "resultAttrName",param.resultAttrName },
                    { "index",-1 },
                    { "discernHidden",param.discernHidden }
                }, param.Timeout).ToString();
        }
        public string QuerySelectorByXpath(ParamQuerySelectorModel param)
        {
            return this.Send("tabs.querySelectorByXpath",
                new JObject {
                    { "tabId",param.TabId },
                    { "xpathSelector", param.cssSelector },
                    { "index",param.index },
                    { "discernHidden",param.discernHidden }
                }, param.Timeout).ToString();
        }
        public string QuerySelectorOld(int tabId, string browserType, string selector, int index = 0, string parentElementId = null)
        {
            var buildselector = BrowserSelectorItem.GetAllSelectorJson(selector, browserType);
            var retJToken = this.Send("tabs.querySelector",
                new JObject {
                    { "tabId", tabId },
                    { "selector", JArray.Parse(buildselector.ToString()) },
                    { "parent", parentElementId },
                    { "index", index }
                });
            if (retJToken == null || string.IsNullOrEmpty(retJToken.ToString()))
            {
                buildselector = BrowserSelectorItem.GetAllSelectorJson(selector, browserType, "regular");
                retJToken = this.Send("tabs.querySelector",
                    new JObject {
                        { "tabId", tabId },
                        { "selector", buildselector },
                        { "parent", parentElementId },
                        { "index", index }
                    });
                return retJToken.ToString();
            }
            return retJToken.ToString();
        }
        public string QueryCssSelector(int tabId, string browserType, string selector, int index, string parent)
        {
            var jsonObj = (JToken)JsonHelper.JsonDesByNewtonJson(selector);
            string cssSelector = string.Empty;
            string outertext = string.Empty;
            JArray nodes = new JArray();
            foreach (var items in jsonObj)
            {
                if (items.Value<string>("cssSelector") != null || items.Value<string>("cssSelectorEx") != null
                    || !string.IsNullOrEmpty(items.Value<string>("tagName")?.ToString()))
                {
                    var node = new JObject();
                    foreach (JProperty item in items)
                    {
                        if (item.Name == "cssSelector")
                            cssSelector = item.Value.ToString();
                        else if (item.Name == "cssSelectorEx")
                            cssSelector = item.Value.ToString();
                        else if (item.Name == "outertext")
                            outertext = item.Value.ToString();
                        else if (item.Name == "outertextEx")
                            outertext = item.Value.ToString();
                        else
                            node.Add(item.Name, item.Value);
                    }
                    nodes.Add(node);
                }
            }
            JObject css = new JObject();
            css.Add("tagName", "document");
            css.Add("cssSelector", cssSelector);
            if (!string.IsNullOrEmpty(outertext))
                css.Add("outertext", outertext);
            nodes.Add(css);

            var buildselector = BrowserSelectorItem.GetAllSelectorJson(nodes.ToString(), browserType: browserType);
            var retJToken = this.Send("tabs.queryCSSSelector",
                new JObject {
                    { "tabId", tabId },
                    { "selector", buildselector },
                    { "index", index },
                    { "parent", parent }
                });
            return retJToken.ToString();
        }
        public string QuerySelectorAll(ParamQuerySelectorModel paramModel)
        {
            return this.Send("tabs.querySelectorAll", new JObject {
                    { "tabId", paramModel.TabId },
                    { "selector", JArray.Parse(paramModel.selector) },
                    { "relateElementId", paramModel.RelateElementId },
                    { "relateMode", (int)paramModel.ElementRelateMode },
                    { "mode", paramModel.mode },
                    { "cssSelector", paramModel.cssSelector },
                    { "index",paramModel.index },
                    { "resultAttrName",paramModel. resultAttrName },
                    { "filter", paramModel.filter },
                    { "relativeType", (int)paramModel.relativeType } ,
                    { "positionNum", paramModel.positionNum },
                    { "version", paramModel.version },
                    { "discernHidden", paramModel.discernHidden }
            }, paramModel.Timeout).ToString();
        }
        public string QuerySelectorAllOld(int tabId, string selector, int index = -1, string parentElementId = null)
        {
            var buildselector = BrowserSelectorItem.GetAllSelectorJson(selector, "Chrome");
            var result = this.Send("tabs.querySelectorAll",
                new JObject {
                    { "tabId", tabId },
                    { "selector", buildselector },
                    { "parent", parentElementId },
                    { "index", index }
                });
            var content = JArray.Parse(result.ToString());
            if (content.Count == 0)
            {
                buildselector = BrowserSelectorItem.GetAllSelectorJson(selector, "Chrome", "regular");
                result = this.Send("tabs.querySelectorAll",
                    new JObject {
                        { "tabId", tabId },
                        { "selector", buildselector },
                        { "parent", parentElementId },
                        { "index", index }
                    });
                return result.ToString();
            }
            return result.ToString();
        }

        public string QueryElementAll(int tabId, string tagNames, string attrKeywords)
        {
            return this.Send("tabs.queryElementsAll", new JObject {
                    { "tabId", tabId },
                    { "tagNames",tagNames },
                    { "attrKeywords",attrKeywords }
            }, 60 * 1000).ToString();
        }
        public string GetViewElementsData(int tabId)
        {
            return this.Send("tabs.getViewElementsData", new JObject {
                    { "tabId", tabId }
            }, 60 * 1000)?.ToString();
        }
        public string GetWebCode(int tabId)
        {
            return this.Send("tabs.getHtml", new JObject {
                    { "tabId", tabId }
            }, 60 * 1000)?.ToString();
        }
        public string SetWebUnqueId(int tabId)
        {
            return this.Send("tabs.setAllElementsUniqueId", new JObject {
                    { "tabId", tabId }
            }, 60 * 1000)?.ToString();
        }
        #endregion

        #region 元素 数据获取和操作 相关

        public virtual JToken ElementFormPoint(int tabId, Point point)
        {
            return this.Send("tabs.elementFromPoint",
                 new JObject {
                    { "tabId", tabId },
                    { "x", point.X * NativeMethods.ScaleX },
                    { "y", point.Y * NativeMethods.ScaleY },
                    { "ApiVersion","v2" }
              }, 1500);
        }
        public JToken IsDisplayed(ParamModel paramModel)
        {
            return this.Send("elements.isDisplayed",
                new JObject {
                    { "tabId", paramModel.TabId },
                    { "elementId", paramModel.ElementId }
                }, paramModel.Timeout);
        }
        public JToken BuildElementBoundingAll(ParamModel paramModel)
        {
            return this.Send("elements.getBoundingAll",
                new JObject {
                    { "tabId", paramModel.TabId },
                    { "elementIds", JArray.FromObject(paramModel.elementIds) }
                 }, paramModel.Timeout);
        }

        public void ScrollIntoViewIfNeeded_V2(ParamScrollModel paramModel)
        {
            this.Send("elements.scrollIntoViewIfNeeded_V2", new JObject {
                { "tabId", paramModel.TabId },
                { "elementId", paramModel.ElementId },
             }, paramModel.Timeout);
        }
        public void ScrollIntoViewIfNeeded(ParamScrollModel paramModel)
        {
            this.Send("elements.scrollIntoViewIfNeeded", new JObject {
                { "tabId", paramModel.TabId },
                { "elementId", paramModel.ElementId },
                { "location",paramModel.location }
            }, paramModel.Timeout);
        }
        public JToken BuildElementBounding(ParamModel paramModel)
        {
            return this.Send("elements.getBounding",
                new JObject {
                    { "tabId", paramModel.TabId },
                    { "elementId",paramModel.ElementId }
                }, paramModel.Timeout);
        }
        public JToken GetTextAll(ParamModel paramModel)
        {
            return this.Send("elements.getTextAll",
                    new JObject {
                    { "tabId", paramModel.TabId },
                    { "elementIds", JArray.FromObject( paramModel.elementIds) }
                  }, paramModel.Timeout);
        }

        public JToken GetText(ParamModel paramModel)
        {
            return this.Send("elements.getText",
                    new JObject {
                    { "tabId", paramModel.TabId },
                    { "elementId", paramModel.ElementId }
                }, paramModel.Timeout);
        }
        public JToken GetNextElementBuildSelector(ParamcssSelectorModel paramModel)
        {
            return this.Send("elements.getNextElementBuildSelector",
                new JObject {
                    { "tabId", paramModel.TabId },
                    { "cssSelector", paramModel.cssSelector },
                    { "innerTextFirst", paramModel.innerTextFirst },
                    { "excludeHiddenEle",paramModel.excludeHiddenEle}
                }, paramModel.Timeout);
        }
        public JToken GetTurningPageElement(ParamcssSelectorModel paramModel)
        {
            return this.Send("elements.getTurningPageElement",
                new JObject {
                    { "tabId", paramModel.TabId },
                    { "elementId",paramModel. ElementId },
                    { "cssSelectors", JArray.FromObject( paramModel.cssSelectors )}
                }, paramModel.Timeout);
        }
        public JToken GetSimilarElement(ParamModel paramModel)
        {
            return this.Send("tabs.getSimilarElement",
                new JObject {
                    { "tabId",  paramModel.TabId },
                    { "elementId",  paramModel.ElementId }
                }, 300 * 1000);
        }
        public JToken GetIsTableName(ParamModel paramModel)
        {
            return this.Send("elements.getIsTableName",
                new JObject {
                    { "tabId",  paramModel.TabId },
                    { "elementId",  paramModel.ElementId }
                }, paramModel.Timeout);
        }
        public JToken GetTableData(ParamModel paramModel)
        {
            return this.Send("elements.getTableData",
                new JObject {
                    { "tabId",  paramModel.TabId },
                    { "elementId",  paramModel.ElementId }
                }, paramModel.Timeout);
        }
        public JToken GetAttributes(ParamModel paramModel)
        {
            return this.Send("elements.getAttributes",
                new JObject {
                    { "tabId", paramModel.TabId },
                    { "elementId",  paramModel.ElementId }
                }, paramModel.Timeout);
        }
        public JToken GetAttributeAll(ParamAttributeModel paramModel)
        {
            return this.Send("elements.getAttributeAll",
                new JObject {
                    { "tabId", paramModel.TabId },
                    { "elementIds", JArray.FromObject(paramModel.elementIds) },
                    { "name", paramModel.name },
                    { "isIntact", paramModel.isIntact},
                    { "apiVersion", paramModel.apiVersion}
                }, paramModel.Timeout);
        }
        public JToken GetAttributeAll_V1(ParamAttributeModel paramModel)
        {
            return this.Send("elements.getAttributeAll",
                new JObject {
                    { "tabId", paramModel.TabId },
                    { "elementIds", JArray.FromObject(paramModel.elementIds) },
                    { "name", paramModel.name },
                    { "isIntact", paramModel.isIntact},
                    { "apiVersion", paramModel.apiVersion}
                }, paramModel.Timeout);
        }
        public JToken GetAttribute_V1(ParamAttributeModel paramModel)
        {
            return this.Send("elements.getAttribute_V1",
                new JObject {
                    { "tabId", paramModel.TabId },
                    { "elementId", paramModel.ElementId },
                    { "name",paramModel.name },
                    { "isIntact", paramModel.isIntact},
                    { "apiVersion", paramModel.apiVersion}
                }, paramModel.Timeout);
        }

        public JToken GetAttribute(ParamAttributeModel paramModel)
        {
            return this.Send("elements.getAttribute",
                new JObject {
                    { "tabId", paramModel.TabId },
                    { "elementId", paramModel.ElementId },
                    { "name",paramModel.name },
                    { "apiVersion", paramModel.apiVersion}
                }, paramModel.Timeout);
        }
        public JToken GetHtmlAll(ParamModel paramModel)
        {
            return this.Send("elements.getHtmlAll",
                new JObject {
                    { "tabId", paramModel.TabId },
                    { "elementIds", JArray.FromObject(paramModel.elementIds) }
                }, paramModel.Timeout);
        }

        public JToken GetHtml(ParamModel paramModel)
        {
            return this.Send("elements.getHtml",
                new JObject {
                    { "tabId", paramModel.TabId },
                    { "elementId", paramModel.ElementId }
                }, paramModel.Timeout);
        }
        public JToken GetCleanHtml(ParamModel paramModel)
        {
            return this.Send("elements.getCleanHtml",
                new JObject {
                    { "tabId", paramModel.TabId },
                    { "elementId", paramModel.ElementId }
                }, paramModel.Timeout);
        }
        public JToken GetParentIdList(ParamAssociatedElementModel paramModel)
        {
            return this.Send("elements.getParent",
                new JObject { { "tabId",paramModel.TabId },
                    { "elementId", paramModel.ElementId },
                    { "filter", paramModel.filter },
                    { "level", paramModel.level }
                }, paramModel.Timeout);
        }
        public JToken GetSiblingsIdList(ParamAssociatedElementModel paramModel)
        {
            return this.Send("elements.getSiblings",
                new JObject { { "tabId", paramModel.TabId },
                    { "elementId", paramModel.ElementId },
                    { "filter", paramModel.filter },
                    { "position", paramModel.position }
                }, paramModel.Timeout);
        }
        public JToken GetChildrenIdList(ParamAssociatedElementModel paramModel)
        {
            return this.Send("elements.getChildren",
                    new JObject { { "tabId", paramModel.TabId },
                    { "elementId", paramModel.ElementId },
                    { "filter", paramModel.filter },
                    { "index",paramModel.index  }
                    }, paramModel.Timeout);
        }
        public void Input(ParamInputModel paramModel)
        {
            this.Send("elements.input",
                new JObject {
                    { "tabId", paramModel.TabId },
                    { "elementId", paramModel.ElementId },
                    { "value", paramModel.value },
                    { "replace", paramModel.isReplace }
                }, paramModel.Timeout);
        }
        public JToken IsEditable(ParamModel paramModel)
        {
            return this.Send("elements.isEditable",
                new JObject {
                    { "tabId", paramModel.TabId },
                    { "elementId", paramModel.ElementId }
                }, paramModel.Timeout);
        }

        public JToken GetProperty(ParamGetPropertyModel paramModel)
        {
            return this.Send("elements.getProperty",
                new JObject {
                    { "tabId", paramModel.TabId },
                    { "elementId", paramModel.ElementId },
                    { "name", paramModel.name }
                }, paramModel.Timeout);
        }
        public JToken GetCheckStatus(ParamModel paramModel)
        {
            return this.Send("elements.getCheckStatus",
                new JObject {
                    { "tabId", paramModel.TabId },
                    { "elementId", paramModel.ElementId }
                }, paramModel.Timeout);
            //if (response.Result.Error != null)
            //{
            //    if (response.Result.Error.Code != -1)
            //        throw new RPAException(eErrorMessageType.Error_ElementNotOperation);
            //    else
            //        throw new RPAException(eErrorMessageType.Error_OperationFail);
            //}
            //return response.Result.Content.ToString();
        }
        public JToken GetSelectOptions(ParamSelectOptionModel paramModel)
        {
            return this.Send("elements.getSelectOptions",
                new JObject {
                    { "tabId", paramModel.TabId },
                    { "elementId", paramModel.ElementId },
                    { "optionType", paramModel.type },
                    { "selected", paramModel.selected==1?true:false }
                }, paramModel.Timeout);

        }
        public JToken GetSelectOption(ParamSelectOptionModel paramModel)
        {
            return this.Send("elements.getSelectOption",
                new JObject {
                    { "tabId", paramModel.TabId },
                    { "elementId", paramModel.ElementId },
                    { "optionType", paramModel.type }
                }, paramModel.Timeout);
        }
        public void Click(ParamModel paramModel)
        {
            this.Send("elements.click",
                new JObject {
                    { "tabId", paramModel.TabId },
                    { "elementId", paramModel.ElementId }
                }, paramModel.Timeout);
        }
        public void RightClick(ParamModel paramModel)
        {
            this.Send("elements.rclick",
                new JObject {
                    { "tabId", paramModel.TabId },
                    { "elementId", paramModel.ElementId }
                }, paramModel.Timeout);
        }

        public void RightDoubleClick(int tabId, string elementId)
        {
            throw new RPAException(eErrorMessageType.Error_NotImplemented);
        }
        public void DoubleClick(ParamModel paramModel)
        {
            this.Send("elements.dblclick",
                new JObject {
                    { "tabId", paramModel.TabId },
                    { "elementId", paramModel.ElementId }
                }, paramModel.Timeout);
        }
        public JToken SetAttribute(ParamAttributeModel paramModel)
        {
            return this.Send("elements.setAttribute",
                new JObject {
                    { "tabId", paramModel.TabId },
                    { "elementId", paramModel.ElementId },
                    { "name", paramModel.name },
                    { "value", paramModel.value }
                }, paramModel.Timeout);
        }
        public JToken SetCheckStatus(ParamCheckStatusModel paramModel)
        {
            return this.Send("elements.setCheckStatus",
                new JObject {
                    { "tabId", paramModel. TabId },
                    { "elementId", paramModel. ElementId },
                    { "isChecked", paramModel.isChecked }
                }, paramModel.Timeout);
        }
        public void Focus(ParamModel paramModel)
        {
            this.Send("elements.focus",
                new JObject {
                    { "tabId", paramModel.TabId },
                    { "elementId",paramModel.ElementId }
                }, paramModel.Timeout);
        }
        public JToken SetSelectOptions(ParamSelectOptionModel paramModel)
        {
            var json = JsonHelper.JsonByNewtonJson(paramModel.idxList);
            return this.Send("elements.setSelectOptions",
                new JObject {
                    { "tabId", paramModel.TabId },
                    { "elementId", paramModel.ElementId },
                    { "options", (JArray)JsonHelper.JsonDesByNewtonJson(json) }
                }, paramModel.Timeout);
        }
        public JToken GetUsableAttributes(ParamModel paramModel)
        {
            return this.Send("elements.getUsableAttributes",
                new JObject {
                    { "tabId", paramModel.TabId },
                    { "elementId", paramModel.ElementId }
                }, paramModel.Timeout);
        }

        public JToken GetElementShot(ParamModel paramModel)
        {
            return this.Send("elements.getElementShot",
                new JObject {
                    { "tabId", paramModel.TabId },
                    { "elementId", paramModel.ElementId }
                }, paramModel.Timeout);
        }

        public JToken GetLineAndBarChartData(Params.ParamGetChartDataModel paramModel)
        {
            return this.Send("elements.getLineChartData",
                new JObject {
                    { "tabId", paramModel.TabId },
                    { "elementId", paramModel.ElementId },
                    { "moveDirection",paramModel.moveDirection},
                    { "moveSpeed",paramModel.moveSpeed},
                    { "moveModel",paramModel.moveModel} ,
                    { "offset",paramModel.offset},
                    {"parentElementId",paramModel.parentElementId }
                }, paramModel.Timeout);
        }
        public JToken GetPieChartData(Params.ParamGetChartDataModel paramModel)
        {
            return this.Send("elements.getPieChartData",
                new JObject {
                    { "tabId", paramModel.TabId },
                    { "elementId", paramModel.ElementId },
                    { "radiusRatio",paramModel.radiusRatio},
                    { "horOffset",paramModel.horOffset} ,
                    { "verOffset",paramModel.verOffset},
                    { "moveSpeed",paramModel.moveSpeed}
                }, paramModel.Timeout);
        }
        #endregion

        #region 网页监听

        public virtual JToken SendCommand(int tabId, string method, JObject commandParam, int timeout = 5000, bool keepAttached = false)
        {
            return this.Send("debugger.sendCommand",
                new JObject {
                   { "tabId", tabId },
                   { "method", method },
                   { "commandParam", commandParam },
                   { "keepAttached", keepAttached }
                }, timeout);
        }
        public virtual void EndInspectNetwork(ParamModel paramModel)
        {
            this.Send("debugger.detach", new JObject { { "tabId", paramModel.TabId } }, paramModel.Timeout);
        }
        public virtual void EnableNetwork(ParamModel paramModel)
        {
            this.SendCommand(paramModel.TabId, "Network.enable", new JObject { { "tabId", paramModel.TabId } }, paramModel.Timeout);
        }

        public virtual void AddNetworkCache(ParamModel paramModel)
        {
            this.Send("debugger.addCache", new JObject { { "tabId", paramModel.TabId }, { "method", "Network.responseReceived" }, { "filter", new JObject { } } }, paramModel.Timeout);
            this.Send("debugger.addCache", new JObject { { "tabId", paramModel.TabId }, { "method", "Network.webSocketFrameReceived" }, { "filter", new JObject { } } }, paramModel.Timeout);
            this.Send("debugger.addCache", new JObject { { "tabId", paramModel.TabId }, { "method", "Network.webSocketFrameSent" }, { "filter", new JObject { } } }, paramModel.Timeout);
        }
        public virtual void StartInspectNetwork(ParamModel paramModel)
        {
            this.AddNetworkCache(paramModel);
            this.EnableNetwork(paramModel);
        }

        public virtual List<JObject> GetInspectNetwork(ParamInspectNetworkModel paramModel)
        {
            var records = new List<JObject>();
            if (paramModel.type == "All" || paramModel.type == "WebSocket")
            {
                var wsSent = this.Send("debugger.getCache", new JObject { { "tabId", paramModel.TabId }, { "method", "Network.webSocketFrameSent" } }, paramModel.Timeout).ToObject<List<JObject>>();
                var wsReceived = this.Send("debugger.getCache", new JObject { { "tabId", paramModel.TabId }, { "method", "Network.webSocketFrameReceived" } }, paramModel.Timeout).ToObject<List<JObject>>();
                records.AddRange(wsSent);
                records.AddRange(wsReceived);
            }
            if (paramModel.type != "WebSocket")
            {
                var httpReceived = this.Send("debugger.getCache", new JObject { { "tabId", paramModel.TabId }, { "method", "Network.responseReceived" } }, paramModel.Timeout).ToObject<List<JObject>>();
                if (paramModel.type != "All")
                {
                    httpReceived = httpReceived.Where(r => r.Value<string>("type") == paramModel.type).ToList();
                }
                if (!string.IsNullOrWhiteSpace(paramModel.url))
                {
                    if (paramModel.isRegex)
                    {
                        var regex = new Regex(paramModel.url, RegexOptions.IgnoreCase);
                        httpReceived = httpReceived.Where(r => regex.IsMatch(r.Value<string>("url"))).ToList();
                    }
                    else
                    {
                        httpReceived = httpReceived.Where(r => r.Value<string>("url").Contains(paramModel.url)).ToList();
                    }
                }
                records.AddRange(httpReceived.Select(r =>
                {
                    var bodyContent = this.Send("debugger.getCacheBody", new JObject { { "tabId", paramModel.TabId }, { "requestId", r.Value<string>("requestId") } }, paramModel.Timeout);
                    if (bodyContent == null || !bodyContent.HasValues)
                    {
                        return r;
                    }
                    r.Add("body", bodyContent.Value<string>("body"));
                    r.Add("base64Encoded", bodyContent.Value<bool>("base64Encoded"));
                    return r;
                }).ToList());
            }
            return records;
        }
        #endregion

        #region 下载
        public void AddCreatedEvent(ParamModel paramModel)
        {
            this.Send("downloads.addCreatedEvent",
                  new JObject(), paramModel.Timeout);
        }
        public void RemoveCreatedEvent(ParamModel paramModel)
        {
            this.Send("downloads.removeCreatedEvent",
                  new JObject(), paramModel.Timeout);
        }
        public JToken Downloading(ParamModel paramModel)
        {
            return this.Send("downloads.search",
                new JObject {
                    { "state", "in_progress" }
                }, paramModel.Timeout);
        }
        public JToken DownloadingV1(ParamModel paramModel)
        {
            return this.Send("downloads.searchV1",
                new JObject {
                    { "state", "in_progress" }
                }, paramModel.Timeout);
        }
        public JToken DownLoadSearchV1(ParamDownloadModel paramModel)
        {
            return this.Send("downloads.searchV1",
                new JObject {
                    { "id", paramModel.id }
                }, paramModel.Timeout);
        }
        public JToken DownLoadSearch(ParamDownloadModel paramModel)
        {
            return this.Send("downloads.search",
                new JObject {
                    { "id", paramModel.id }
                }, paramModel.Timeout);
        }
        public JToken DownLoadUrl(ParamDownloadModel paramModel)
        {
            return this.Send("downloads.downloadUrl",
                new JObject {
                    { "url", paramModel.url }
                }, paramModel.Timeout);

        }
        #endregion

        #region NMH
        public JToken GetWindowMap(int timeout = 5000)
        {
            return this.Send("getWindowMap", new JObject(), timeout);
        }
        #endregion
        public bool Ping(int timeout = 1000)
        {
            logger.LogDebug($"Ping timeout: {timeout}");
            var result = this.Send("ping", new JObject(), timeout);
            return (result != null && result.ToString() == "pong");
        }
        public void TestFuncBase()
        {
            this.Send("tabs.ClearIHtmlObject", new JObject());
        }
    }

    /// <summary>
    /// 脚本注入参数
    /// </summary>
    public class InjectScriptParam
    {
        /// <summary>
        /// 连接
        /// </summary>
        public IBrowserConnectionNew Connection;

        /// <summary>
        /// 元素类型 对应浏览器插件类型
        /// </summary>
        public eGuiType GuiType;

        /// <summary>
        /// 是否已注入过（原初始化注入逻辑）
        /// </summary>
        public bool IsInitial;

        /// <summary>
        /// 异常信息
        /// </summary>
        public string ErrMessage;

        /// <summary>
        /// 是否时候预注入
        /// </summary>
        public bool IsPreInjection;
    }
}
