using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Runtime.Serialization;

namespace II.RPA.Plugin.IE
{
    class IEException : RPAException
    {
        public IEException()
            : base()
        {
        }
    
        internal IEException(int errorCode, eComponentType componentType)
            : base(eGuiType.IE, errorCode, componentType, null)
        {

        }
        internal IEException(eErrorMessageType errorCode)
        : base(eGuiType.IE, errorCode, eComponentType.None, null)
        {

        }
        internal IEException(eErrorMessageType errorCode, eComponentType componentType)
          : base(eGuiType.IE, errorCode, componentType, null)
        {

        }
        internal IEException(eErrorMessageType errorCode,Exception e)
        : base(eGuiType.IE, errorCode, eComponentType.None, e)
        {

        }
        internal IEException(int errorCode, eComponentType componentType, Exception e)
            : base(eGuiType.IE, errorCode, componentType, e)
        {

        }
        internal IEException(eErrorMessageType errorCode, eComponentType componentType, Exception e)
            : base(eGuiType.IE, errorCode, componentType, e)
        {

        }
    }
}
