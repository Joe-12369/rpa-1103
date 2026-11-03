using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Core
{
    public class ProviderException: IIException
    {
        public ProviderException() : base() 
        {
            
        }
        public ProviderException(Exception anInnerException) : base("Exception",anInnerException)
        {

        }
        public ProviderException(string aMessage, Exception anInnerException) : base(aMessage,anInnerException)
        {

        }
    }
}
