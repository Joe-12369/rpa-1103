using System;
using System.IO;
using FlaUI.Core.Logging;
using II.RPA.IPC;
using II.RPA.Plugin.Core.Entities;
using II.RPA.WinAutomation.CommonUtility;
using Newtonsoft.Json;

namespace II.RPA.ResiliencePluginServices.IPC
{
    internal class PluginInitializeService : IActionService
    {
        IResponseResult IActionService.Request(IExecuteMessageable executor, IRequestParams request)
        {
            NLogger.Default.Info($"[Request]编号[{request.GetHashCode()}=>PluginInitializeService");
            int len = request.Parameters.Length;
            NLogger.Default.Info($"[Request]编号[{request.GetHashCode()}]接收到请求，请求内容长度为{len}。");
            PluginIPCInitializeRequest receivedMessage = null;
            try
            {
                receivedMessage = JsonConvert.DeserializeObject<PluginIPCInitializeRequest>(request.Parameters);
            }
            catch
            {
                var exception = new ArgumentException("[Request]IRequestParams.Parameters 无法转换为 PluginIPCInitializeRequest，Json字符串异常!");
                NLogger.Default.Error("[Request]请求参数非法！", exception);
                NLogger.Default.Error("[Request]非法参数：" + request.Parameters);
                throw exception;
            }

            if (receivedMessage == null || string.IsNullOrEmpty(receivedMessage.PackagesPath))
            {
                var exception = new ArgumentException("[Request]PluginIPCInitializeRequest为Null,或者 PackagesPath为Null，或者 Action为Null");
                Logger.Default.Error("[Request]请求参数非法！", exception);
                Logger.Default.Error("[Request]非法参数：" + request.Parameters);
                throw exception;
            }

            if (!Directory.Exists(receivedMessage.PackagesPath))
            {
                string exception = $"路径[{receivedMessage.PackagesPath}]不存在,或者格式不正确";
                Logger.Default.Error(exception);
                throw new DirectoryNotFoundException(exception);
            }

            if (executor != null)
            {
                try
                {
                    var doRet = executor.InitializePlugin(receivedMessage.PackagesPath, receivedMessage.InstallDirPath);
                    ResponseResult ret = new ResponseResult()
                    {
                        Result = JsonConvert.SerializeObject(doRet)
                    };
                    NLogger.Default.Info($"[Request]编号[{request.GetHashCode()}]完成请求，并且开始返回。");
                    return ret;
                }
                catch (Exception ex)
                {
                    NLogger.Default.Error($"[Request]编号[{request.GetHashCode()}]执行异常：", ex);
                    throw;
                }
            }
            throw new Exception($"[Request]编号[{request.GetHashCode()}]执行器为空...");
        }
    }
}