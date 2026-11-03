using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.WebBrowser.Common
{
    internal enum CrxVersion
    {
        /// <summary>
        /// 旧版
        /// </summary>
        Legacy = 2,
        /// <summary>
        /// V3版本
        /// </summary>
        V3 = 3,
        /// <summary>
        /// 新版V3
        /// </summary>
        NewV3 = 4
    }
    internal class CrxVersionHelper
    {
        public static CrxVersion GetCrxVersion(string crxVersion)
        {
            if (string.IsNullOrWhiteSpace(crxVersion))
            {
                return CrxVersion.Legacy;
            }
            else if (crxVersion.StartsWith("3.") || crxVersion.Equals("4.0.0.134"))
            {
                return CrxVersion.V3;
            }
            else if(crxVersion.StartsWith("4."))
            {
                return CrxVersion.NewV3;
            }
            else
            {
                return CrxVersion.Legacy;
            }
        }
    }
}
