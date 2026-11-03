using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace II.RPA.MobileDeviceManager.Models
{
    public enum E_MobileTool_Install_Status
    {
        /// <summary>
        /// 未安装
        /// </summary>
        NotInstall,
        /// <summary>
        /// 安装中
        /// </summary>
        Installing,
        /// <summary>
        /// 安装失败
        /// </summary>
        FailInstall,
        /// <summary>
        /// 安装成功
        /// </summary>
        SuccessInstall
    }
}
