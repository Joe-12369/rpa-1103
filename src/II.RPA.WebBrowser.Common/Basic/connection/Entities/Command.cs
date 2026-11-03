using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.WebBrowser.Common.Basic.connection.Entities
{
    public class CommandMessage
    {
        public string Command { get; set; }
        public string Args { get; set; }
        public int Timeout { get; set; } = 5000;
    }
}
