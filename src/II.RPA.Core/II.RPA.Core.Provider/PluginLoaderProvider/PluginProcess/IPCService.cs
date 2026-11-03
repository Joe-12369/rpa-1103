using System;
using II.RPA.Core.IPCExtension.IPC;
using II.RPA.Core.IPCExtension.Message;
using II.RPA.Core.Log;
using II.RPA.IPC;
using Newtonsoft.Json;

namespace II.RPA.Core.Provider.PluginLoaderProvider.PluginProcess
{
    public abstract class BaseDistributeService : IActionService
    {
        protected ILogger logger;

        public BaseDistributeService(ILogger logger)
        {
            this.logger = logger;
        }

        public IResponseResult Request(IRequestParams request)
        {
            if (request == null)
            {
                var exception = new NullReferenceException($"[ReceiveHandle]参数为空，无法执行空操作！");
                this.logger.LogError("[ReceiveHandle]请求参数非法！", exception);
                throw exception;
            }

            if (string.IsNullOrEmpty(request.Parameters))
            {
                var exception = new NullReferenceException($"[ReceiveHandle]业务执行操作为空，无法执行！");
                this.logger.LogError("[ReceiveHandle]请求参数非法！", exception);
                throw exception;
            }

            this.logger.LogTrace($"Params Type:Is RequestActionParams {request is RequestActionParams}");

            if (request is RequestActionParams actionRequest)
            {
                if (actionRequest.Action == "DataCollect")
                {
                    this.logger.LogDebug($"DataCollect Received:{actionRequest.Parameters}");
                    dynamic retObj = JsonConvert.DeserializeObject(actionRequest.Parameters);
                    var handleRet = this.HandleMessage(actionRequest.Action, (int)retObj.TerminalType, retObj.Data.Value);

                    ResponseResult ret = new ResponseResult()
                    {
                        Result = handleRet
                    };
                    return ret;
                }
                else if (actionRequest.Action == "PluginServicesStatus")
                {
                    this.logger.LogDebug($"PluginServicesStatus Received:{actionRequest.Parameters}");
                    dynamic retObj = JsonConvert.DeserializeObject(actionRequest.Parameters);
                    var handleRet = this.HandleMessage(actionRequest.Action, (int)retObj.TerminalType, retObj.Data.ToString());

                    ResponseResult ret = new ResponseResult()
                    {
                        Result = handleRet
                    };
                    return ret;
                }
                else
                {
                    var exception = new NullReferenceException($"[ReceiveHandle]无法处理的业务动作:({actionRequest.Action})！");
                    this.logger.LogError("[Request]请求参数非法！", exception);
                    throw exception;
                }
            }
            else
            {
                var exception = new NullReferenceException($"[ReceiveHandle]业务执行操作为空，无法执行！");
                this.logger.LogError("[ReceiveHandle]请求参数非法！", exception);
                throw exception;
            }
        }

        protected abstract string HandleMessage(string action, int terminalType, string data);
    }

    public class IPCService : BaseDistributeService
    {
        private IPublisher publisher;

        public IPCService(ILogger aLogger, IPublisher publisher) : base(aLogger)
        {
            this.publisher = publisher;
        }

        protected override string HandleMessage(string action, int terminalType, string data)
        {
            if (action == "PluginServicesStatus")
            {
                publisher.Publish("PluginServicesStatus", data);
                return "Updated!";
            }
            else
            {
                this.logger.LogDebug($"TestService Received :{terminalType},{data}");
                string sendingData = $"TestService Send:{DateTime.Now.ToString("yyyy-dd-MM hh:mm:ss")} Welcome!";
                this.logger.LogDebug($"TestService Sending :({sendingData})");
                return sendingData;
            }
        }
    }
}