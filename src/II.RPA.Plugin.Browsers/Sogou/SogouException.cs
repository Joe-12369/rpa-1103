using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Runtime.Serialization;

namespace II.RPA.Plugin.Browsers.Sogou
{
    class SogouException : RPAException
    {
        public SogouException()
            : base()
        {
        }
       
        internal SogouException(int errorCode, eComponentType componentType)
            : base(eGuiType.Sogou, errorCode, componentType, null)
        {

        }
        internal SogouException(eErrorMessageType errorCode)
        : base(eGuiType.Sogou, errorCode, eComponentType.None, null)
        {

        }
        internal SogouException(eErrorMessageType errorCode, eComponentType componentType)
          : base(eGuiType.Sogou, errorCode, componentType, null)
        {

        }
        internal SogouException(eErrorMessageType errorCode,Exception e)
        : base(eGuiType.Sogou, errorCode, eComponentType.None, e)
        {

        } 
        internal SogouException(int errorCode, eComponentType componentType, Exception e)
            : base(eGuiType.Sogou, errorCode, componentType, e)
        {

        }
        internal SogouException(eErrorMessageType errorCode, eComponentType componentType, Exception e)
            : base(eGuiType.Sogou, errorCode, componentType, e)
        {

        }
    }
}
