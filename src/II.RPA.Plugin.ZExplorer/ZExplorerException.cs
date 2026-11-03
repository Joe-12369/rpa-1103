using II.RPA.WinAutomation.CommonUtility;
using System;

namespace II.RPA.Plugin.ZExplorer
{
    class ZExplorerException : RPAException
    {
        public ZExplorerException()
            : base()
        {
        }

        internal ZExplorerException(int errorCode, eComponentType componentType)
            : base(eGuiType.ZExplorer, errorCode, componentType, null)
        {

        }
        internal ZExplorerException(eErrorMessageType errorCode)
        : base(eGuiType.ZExplorer, errorCode, eComponentType.None, null)
        {

        }
        internal ZExplorerException(eErrorMessageType errorCode, eComponentType componentType)
          : base(eGuiType.ZExplorer, errorCode, componentType, null)
        {

        }
        internal ZExplorerException(eErrorMessageType errorCode, Exception e)
        : base(eGuiType.ZExplorer, errorCode, eComponentType.None, e)
        {

        }
        internal ZExplorerException(int errorCode, eComponentType componentType, Exception e)
            : base(eGuiType.ZExplorer, errorCode, componentType, e)
        {

        }
        internal ZExplorerException(eErrorMessageType errorCode, eComponentType componentType, Exception e)
            : base(eGuiType.ZExplorer, errorCode, componentType, e)
        {

        }
    }
}
