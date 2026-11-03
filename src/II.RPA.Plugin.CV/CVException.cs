using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Runtime.Serialization;

namespace II.RPA.Plugin.CV
{
    class CVException : RPAException
    {
        public CVException()
            : base()
        {
        }
     
        internal CVException(int errorCode, eComponentType componentType)
            : base(eGuiType.CV, errorCode, componentType, null)
        {

        }
        internal CVException(eErrorMessageType errorCode)
        : base(eGuiType.CV, errorCode, eComponentType.None, null)
        {

        }
        internal CVException(eErrorMessageType errorCode, eComponentType componentType)
          : base(eGuiType.CV, errorCode, componentType, null)
        {

        }
        internal CVException(eErrorMessageType errorCode,Exception e)
        : base(eGuiType.CV, errorCode, eComponentType.None, e)
        {

        }
        internal CVException(int errorCode, eComponentType componentType, Exception e)
            : base(eGuiType.CV, errorCode, componentType, e)
        {

        }
        internal CVException(eErrorMessageType errorCode, eComponentType componentType, Exception e)
            : base(eGuiType.CV, errorCode, componentType, e)
        {

        }
    }
}
