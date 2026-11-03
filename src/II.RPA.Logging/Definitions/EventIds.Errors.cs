using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Logging.Definitions
{
    public static partial class EventIds
    {
        public static EventId ErrorDefault = new EventId(400);
        public static EventId ErrorConfig = new EventId(410);
        public static EventId ErrorExit = new EventId(440);
    }
}
