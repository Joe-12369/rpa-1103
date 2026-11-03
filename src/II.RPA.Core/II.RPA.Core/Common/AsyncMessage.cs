using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Core.Common
{
    public class AsyncMessage<T,P> where T : class where P : class
    {
        public AsyncMessage(int anId) 
        {
            Id = anId;
        }
        public int Id
        {
            get;
            private set;
        }
        private AsyncMessageModel<T, P> MessageModel;
        public void AddInput(T anInput)
        {
            MessageModel = new AsyncMessageModel<T, P>(anInput);
        }
        public T GetInput()
        {
            return MessageModel.Input;
        }
        public void SetOutPut(P anOutPut)
        {
            MessageModel.SetOutput(anOutPut);
        }
        public P GetOutPut(TimeSpan timeSpan)
        {
            return MessageModel.GetOutput(timeSpan);
        }
        public P GetOutPut()
        {
            return MessageModel.GetOutput();
        }
    }
}
