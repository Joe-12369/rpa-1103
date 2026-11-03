using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Plugin.GUI.Tasks
{
    internal class CaptureTaskStopedException : Exception
    {
        public CaptureTaskStopedException() : base() { }

        public CaptureTaskStopedException(string message) : base(message) { }

        public CaptureTaskStopedException(string message, Exception ex) : base(message, ex) { }

        public CaptureTaskStopedException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
