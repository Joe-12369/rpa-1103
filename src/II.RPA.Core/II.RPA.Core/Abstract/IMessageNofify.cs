using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Core.Abstract
{
    public interface IMessageNotify: IGRPCService
    {
        void SetReady();
        void SetSuspend();
        Task<string> NotifyFactoryAndWaitReturn(string anAction,string aMessage);
        Task<EngineInvokeMessage> NotifyEngineAndWaitReturn(string anAction, string aMessage);
        Task<string> NotifyBotAndWaitReturn(string anAction, string aMessage);
        Task<string> ServerMessagePublish(string anAction, string aMessage);
        string ServerMessagePublishSync(string anAction, string aMessage);
        Action<string> ClientMessageSubscribeHandle
        { 
            get; 
            set; 
        }
    }
    public struct EngineInvokeMessage
    {
        public int StatusCode
        {
            get;
            set;
        }
        public string JsonMessage
        {
            get;
            set;
        }
    }
}
