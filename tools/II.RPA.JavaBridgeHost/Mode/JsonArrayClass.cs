using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.JavaBridgeHost.Mode
{
    public class JsonArrayClass
    {
        public object ret = new object();
        public JsonArrayClass() { }
        public JsonArrayClass(object obj)
        {
            this.ret = obj;
        }
    }
}
