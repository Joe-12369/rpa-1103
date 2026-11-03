using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.MobileProvider.Params
{
    public class ParamClickKeyButtonModel
    {
        public string sessionId { get; set; }
        //public string Selector { get; set; }
        /// <summary>
        /// 参见KeyType
        /// </summary>
        public int key { get; set; }
        public double timeout { get; set; }
    }
    enum EumKeyType
    {
        Home,
        Back,
        Switch,
        Enter,
        Revolve,
        Del
    }
}
