using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.PatchUpdate
{
    public class ProcessInfos
    {
        public List<ProcessInfo> Processes = new List<ProcessInfo>();
    }
    public class ProcessInfo
    {
        public int PId { get; set; }
        public string Path { get; set; }
        public string Name { get; set; }
    }
}
