using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.NamedPipeWrapper
{
    [Serializable]
    public class PipeMessage
    {
        private static Random rnd = new Random();
        private static int MessageCounter = rnd.Next(100, 9000);
        public object Error { get; set; }
        public PipeMessage()
        {
            ++MessageCounter;
            messageid = MessageCounter.ToString();
        }
        public PipeMessage(PipeMessage message)
        {
            messageid = message.messageid;
        }
        public string messageid { get; set; }
        public object error { get; set; }
    }
}
