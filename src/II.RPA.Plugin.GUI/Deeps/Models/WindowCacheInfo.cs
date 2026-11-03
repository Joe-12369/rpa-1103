using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Deeps.Models
{
    public class WindowCacheInfo
    {
        public int ProcessId { get; set; }
        public string ProcessName { get; set; }
        public DateTime CacheTime { get; set; }
        public bool IsExcluded { get; set; }

        public override string ToString()
        {
            return $"{ProcessId}-{ProcessName}-{IsExcluded}";
        }
    }
}
