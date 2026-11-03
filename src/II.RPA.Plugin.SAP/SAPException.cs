using System;
using II.RPA.WinAutomation.CommonUtility;

namespace II.RPA.Plugin.SAP
{
    class SAPException : RPAException
    {
        public SAPException()
         : base()
        {
        }
        internal SAPException(int errorCode, eComponentType componentType)
            : base(eGuiType.SAP, errorCode, componentType, null)
        {

        }
        internal SAPException(eErrorMessageType errorCode)
        : base(eGuiType.SAP, errorCode, eComponentType.None, null)
        {

        }
        internal SAPException(eErrorMessageType errorCode, eComponentType componentType)
          : base(eGuiType.SAP, errorCode, componentType, null)
        {

        }
        internal SAPException(eErrorMessageType errorCode, Exception e)
        : base(eGuiType.SAP, errorCode, eComponentType.None, e)
        {

        }
        internal SAPException(int errorCode, eComponentType componentType, Exception e)
            : base(eGuiType.SAP, errorCode, componentType, e)
        {

        }
        internal SAPException(eErrorMessageType errorCode, eComponentType componentType, Exception e)
            : base(eGuiType.SAP, errorCode, componentType, e)
        {

        }
    }
}
