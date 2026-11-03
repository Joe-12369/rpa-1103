using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Runtime.Serialization;

namespace II.RPA.JavaBridgeHost
{
    class JavaException : RPAException
    {
        public JavaException()
            : base()
        {
        }

        internal JavaException(int errorCode, eComponentType componentType)
            : base(eGuiType.Java, errorCode, componentType, null)
        {

        }
        internal JavaException(eErrorMessageType errorCode)
        : base(eGuiType.Java, errorCode, eComponentType.None, null)
        {

        }
        internal JavaException(eErrorMessageType errorCode, eComponentType componentType)
          : base(eGuiType.Java, errorCode, componentType, null)
        {

        }
        internal JavaException(eErrorMessageType errorCode, Exception e)
        : base(eGuiType.Java, errorCode, eComponentType.None, e)
        {

        }
        internal JavaException(int errorCode, eComponentType componentType, Exception e)
            : base(eGuiType.Java, errorCode, componentType, e)
        {

        }
        internal JavaException(eErrorMessageType errorCode, eComponentType componentType, Exception e)
            : base(eGuiType.Java, errorCode, componentType, e)
        {

        }
    }
}
