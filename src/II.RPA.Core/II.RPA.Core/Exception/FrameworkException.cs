using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Core
{
    public class FrameworkException:IIException
    {
        public FrameworkException() : base()
        {

        }
        public FrameworkException(Exception anInnerException) : base("Exception", anInnerException)
        {

        }
        public FrameworkException(string aMessage, Exception anInnerException) : base(aMessage, anInnerException)
        {

        }
    }
}
