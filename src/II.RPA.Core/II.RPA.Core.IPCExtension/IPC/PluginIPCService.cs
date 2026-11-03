using System;
using System.ServiceModel;
using II.RPA.IPC;

namespace II.RPA.Core.IPCExtension.IPC
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, UseSynchronizationContext =false,IncludeExceptionDetailInFaults = true, ConcurrencyMode = ConcurrencyMode.Multiple)]
    internal class PluginIPCService : IMessageExchange, IDisposable
    {
        private IActionService actionService;
        private object lockObj = new object();

        public PluginIPCService(IActionService actionService)
        {
            this.actionService = actionService;
        }

        public void Dispose()
        {
        }

        public void Pending()
        {
        }

        public IResponseResult Request(IRequestParams request)
        {
            if (request == null)
            {
                var exception = new NullReferenceException($"[Request]参数为空，无法执行空操作！");
                //this.logger.LogError("[Request]请求参数非法！", exception);
                throw exception;
            }

            if (string.IsNullOrEmpty(request.Parameters))
            {
                var exception = new NullReferenceException($"[Request]业务执行操作为空，无法执行！");
                //this.logger.LogError("[Request]请求参数非法！", exception);
                throw exception;
            }

            lock (lockObj)
            {
                var result = this.actionService.Request(request);
                return result;
            }
        }
    }
}