using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.MobileProvider
{
    public class MobileException : RPAException
    {
        public MobileException()
            : base()
        {
        }

        internal MobileException(int errorCode, eComponentType componentType)
            : base(eGuiType.Mobile, errorCode, componentType, null)
        {

        }
        internal MobileException(eErrorMessageType errorCode)
        : base(eGuiType.Mobile, errorCode, eComponentType.None, null)
        {

        }
        internal MobileException(eErrorMessageType errorCode, eComponentType componentType)
          : base(eGuiType.Mobile, errorCode, componentType, null)
        {

        }
        internal MobileException(eErrorMessageType errorCode, Exception e)
        : base(eGuiType.Mobile, errorCode, eComponentType.None, e)
        {

        }
        internal MobileException(int errorCode, eComponentType componentType, Exception e)
            : base(eGuiType.Mobile, errorCode, componentType, e)
        {

        }
        internal MobileException(eErrorMessageType errorCode, eComponentType componentType, Exception e)
            : base(eGuiType.Mobile, errorCode, componentType, e)
        {

        }
    }
}
