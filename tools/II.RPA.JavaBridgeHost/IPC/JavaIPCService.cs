using System;
using II.RPA.IPC;
using II.RPA.NamedPipeWrapper;
using Newtonsoft.Json;

namespace II.RPA.JavaBridgeHost.IPC
{
    public interface IExecuteMessageable
    {
        JavaMessage ExecuteReceiveMessage(JavaMessage message);
    }
    public class JavaIPCService : IMessageExchange, IDisposable
    {
        public JavaIPCService(IExecuteMessageable executor)
        {
            this.Executor = executor;
        }

        public IExecuteMessageable Executor { get; private set; }

        public void Dispose()
        {
        }

        public void Pending()
        {
        }

        public IResponseResult Request(IRequestParams request)
        {
            JavaMessage javaMessage = JsonConvert.DeserializeObject<JavaMessage>(request.Parameters);
            if (this.Executor != null)
            {
                var doRet = this.Executor.ExecuteReceiveMessage(javaMessage);
                ResponseResult ret = new ResponseResult()
                {
                    Result = JsonConvert.SerializeObject(doRet)
                };
                return ret;
            }
            throw new JavaException();
        }
    }
}
