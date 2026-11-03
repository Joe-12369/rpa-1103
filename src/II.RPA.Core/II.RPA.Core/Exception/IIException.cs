using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Core
{
    public class IIException:Exception
    {
        public IIException() : base()
        {

        }
        public IIException(Exception anInnerException) : base("Exception", anInnerException)
        {

        }
        public IIException(string aMessage, Exception anInnerException) : base(aMessage, anInnerException)
        {

        }
        public ExceptionLevel Level
        {
            get;
            set;
        }
    }
    public enum ExceptionLevel
    {
        //log is enough
        WARN,
        //log and throw
        ERROR,
        //log and shutdown
        FATAL,
    }
}
