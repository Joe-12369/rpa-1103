using II.RPA.WinAutomation.CommonUtility;
using System;
using System.Runtime.Serialization;

namespace II.RPA.Plugin.Database
{
    class DataBaseException : RPAException
    {
        public DataBaseException()
            : base()
        {
        }
       
        internal DataBaseException(int errorCode, eComponentType componentType)
            : base(eGuiType.DataBase, errorCode, componentType, null)
        {

        }
        internal DataBaseException(eErrorMessageType errorCode)
        : base(eGuiType.DataBase, errorCode, eComponentType.None, null)
        {

        }
        internal DataBaseException(eErrorMessageType errorCode, eComponentType componentType)
          : base(eGuiType.DataBase, errorCode, componentType, null)
        {

        }
        internal DataBaseException(eErrorMessageType errorCode,Exception e)
        : base(eGuiType.DataBase, errorCode, eComponentType.None, e)
        {

        } 
        internal DataBaseException(int errorCode, eComponentType componentType, Exception e)
            : base(eGuiType.DataBase, errorCode, componentType, e)
        {

        }
        internal DataBaseException(eErrorMessageType errorCode, eComponentType componentType, Exception e)
            : base(eGuiType.DataBase, errorCode, componentType, e)
        {

        }
    }
}
