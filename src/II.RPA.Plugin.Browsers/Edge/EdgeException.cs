using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Runtime.Serialization;

namespace II.RPA.Plugin.Browsers.Edge
{
    class EdgeException : RPAException
    {
        public EdgeException()
            : base()
        {
        }
       
        internal EdgeException(int errorCode, eComponentType componentType)
            : base(eGuiType.Edge, errorCode, componentType, null)
        {

        }
        internal EdgeException(eErrorMessageType errorCode)
        : base(eGuiType.Edge, errorCode, eComponentType.None, null)
        {

        }
        internal EdgeException(eErrorMessageType errorCode, eComponentType componentType)
          : base(eGuiType.Edge, errorCode, componentType, null)
        {

        }
        internal EdgeException(eErrorMessageType errorCode,Exception e)
        : base(eGuiType.Edge, errorCode, eComponentType.None, e)
        {

        } 
        internal EdgeException(int errorCode, eComponentType componentType, Exception e)
            : base(eGuiType.Edge, errorCode, componentType, e)
        {

        }
        internal EdgeException(eErrorMessageType errorCode, eComponentType componentType, Exception e)
            : base(eGuiType.Edge, errorCode, componentType, e)
        {

        }
    }
}
