using System;
using II.RPA.WinAutomation.CommonUtility;

namespace II.RPA.Plugin.Windows
{
    class WindowsException : RPAException
    {
        public WindowsException()
         : base()
        {
        }
        internal WindowsException(int errorCode, eComponentType componentType)
            : base(eGuiType.Windows, errorCode, componentType, null)
        {

        }
        internal WindowsException(eErrorMessageType errorCode)
        : base(eGuiType.Windows, errorCode, eComponentType.None, null)
        {

        }
        internal WindowsException(eErrorMessageType errorCode, eComponentType componentType)
          : base(eGuiType.Windows, errorCode, componentType, null)
        {

        }
        internal WindowsException(eErrorMessageType errorCode, Exception e)
        : base(eGuiType.Windows, errorCode, eComponentType.None, e)
        {

        }
        internal WindowsException(int errorCode, eComponentType componentType, Exception e)
            : base(eGuiType.Windows, errorCode, componentType, e)
        {

        }
        internal WindowsException(eErrorMessageType errorCode, eComponentType componentType, Exception e)
            : base(eGuiType.Windows, errorCode, componentType, e)
        {

        }
    }
}
