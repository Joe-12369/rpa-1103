using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.Core.Common
{
    /// <summary>
    /// static command args
    /// </summary>
    public static class CommonConstants
    {
        public const string VALUE_FACTORY = "Z-Factory";
        public const string VALUE_BOT = "Z-Bot";
        public const string VALUE_ZDESKTOP = "Z-DeskTop";
        public const string VALUE_ADMIN = "Z-Admin";

        public const string VALUE_DEBUG = "Debug";
        public const string VALUE_RELEASE = "Release";

        /// <summary>
        /// 客户端类型
        /// </summary>
        public static BaseConstant CLIENTID
        {
            get
            {
                return new BaseConstant
                {
                    KEY = "-client",
                    VALUES = new string[]
                     {
                         VALUE_FACTORY,
                         VALUE_BOT,
                         VALUE_ZDESKTOP,
                         VALUE_ADMIN
                     }
                };
            }

        }
        /// <summary>
        /// debug/release
        /// 该参数目前主要用于日志输出级别的判断
        /// </summary>
        public static BaseConstant TYPE
        {
            get
            {
                return new BaseConstant
                {
                    KEY = "-type",
                    VALUES = new string[]
                    {
                        VALUE_DEBUG, VALUE_RELEASE
                    }
                };
            }

        }
    }
    public class BaseConstant
    {
        public string KEY { get; set; }
        public string[] VALUES { get; set; }
    }
}
