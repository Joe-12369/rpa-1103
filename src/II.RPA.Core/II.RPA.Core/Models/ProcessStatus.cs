using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Core.Models
{
    public enum ProcessStatus
    {
        New = 0,
        Started = 1,
        Initialize = 2,
        Ready = 3,
        Dispose = 4,
        Stop = 5,
        Stopping = 6
    }
}
