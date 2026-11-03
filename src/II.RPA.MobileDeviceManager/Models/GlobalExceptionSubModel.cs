using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.MobileDeviceManager.Models
{
    public class GlobalExceptionSubModel
    {
        public GlobalExceptionSubModel(ExceptionType exceptionType, string exceptionMsg)
        {
            ExceptionType = exceptionType;
            ExceptionMessage = exceptionMsg;
        }
        public ExceptionType ExceptionType { get; private set; }
        public string ExceptionMessage { get; private set; }
    }

    public enum ExceptionType
    {
        /// <summary>
        /// appium连接类异常
        /// </summary>
        AppiumEx,
        /// <summary>
        /// screen连接类异常
        /// </summary>
        ScreenEx,
        /// <summary>
        /// 其他
        /// </summary>
        OtherEx,
    }
}
