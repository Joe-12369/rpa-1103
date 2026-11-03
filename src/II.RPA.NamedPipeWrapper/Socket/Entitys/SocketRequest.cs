using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.NamedPipeWrapper.Sockets.Entitys
{
    public class SocketRequestMessage : SocketRequest<JObject>
    {

    }
    public class SocketRequestMessage<T>  : SocketRequest<T> where T : new()
    {
        public SocketRequestMessage()
        {
            this.Params = new T();
        }
    }
    public class SocketRequest<T> 
    {
        public SocketRequest()
        {
            this.MessageID = string.Empty;
            this.Method = string.Empty;
            this.Params = default(T);
        }
        public string MessageID { get; set; }
        public string Method { get; set; }
        public T Params { get; set; }
    }


}
