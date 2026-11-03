using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.EventMonitor.Entitys
{
    class TriggerErrorEventArgs : EventArgs
    {
        public string sessionId { get; set; } = string.Empty;
        public string triggerName { get; set; } = string.Empty;
        public JMessage message { get; set; } 
    }
}
