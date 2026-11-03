using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace FlaUI.Core.Exceptions
{
    [Serializable]
    public class NoRectangleCenterException : FlaUIException
    {
        public NoRectangleCenterException()
        {
        }

        public NoRectangleCenterException(string message)
            : base(message)
        {
        }

        public NoRectangleCenterException(Exception innerException)
            : base(String.Empty, innerException)
        {
        }

        public NoRectangleCenterException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        protected NoRectangleCenterException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
