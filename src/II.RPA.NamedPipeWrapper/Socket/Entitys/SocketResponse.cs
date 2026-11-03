using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.NamedPipeWrapper.Sockets.Entitys
{
    
    public class SocketResponseMessage : SocketResponse<object>
    {

    }
    public class SocketResponse<T>
    {
        public SocketResponse()
        {
            this.MessageID = string.Empty;
            this.Code = -100;
            this.Message = "None";
            //this.Result = default(T);
        }
        public string MessageID { get; set; }
        public int Code { get; set; }
        public string Message{ get; set; }
        //public T Result { get; set; }
    }
}
